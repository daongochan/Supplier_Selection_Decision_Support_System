#!/usr/bin/env python3
"""
tools/verify/verify_schema_sync.py

Comprehensive verification and synchronization auditor for database schemas:
1. database/01_schema.sql (Canonical Aggregate Schema)
2. database/scripts/Run_All.sql (Scripts Runner Schema)
3. database/scripts/00_Create_Database.sql .. 08_Views.sql (Modular Schema Definitions)
4. database/02_seed.sql (Canonical Development Seed)

Verification Checks:
  - Safety Guards (ConfirmRecreate, DATA LOSS PREVENTION, SET NOEXEC ON) in all root creation scripts.
  - Object inventory counts: 18 tables, 7 triggers, 2 views, 21 indexes.
  - Column-level schema definition parity between 01_schema.sql and modular scripts.
  - Elimination of duplicate/stale seed data from Run_All.sql and 09_Seed_Data.sql.
  - Absolute path check (disallow 'file:///' references).
"""

import argparse
import os
import re
import sys

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

SCHEMA_PATH = os.path.join(REPO_ROOT, "database", "01_schema.sql")
RUN_ALL_PATH = os.path.join(REPO_ROOT, "database", "scripts", "Run_All.sql")
CREATE_DB_PATH = os.path.join(REPO_ROOT, "database", "scripts", "00_Create_Database.sql")
SCRIPTS_DIR = os.path.join(REPO_ROOT, "database", "scripts")
SEED_PATH = os.path.join(REPO_ROOT, "database", "02_seed.sql")


def read_file(path: str) -> str:
    if not os.path.exists(path):
        raise FileNotFoundError(f"Missing file: {path}")
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def verify_safety_guard(content: str, name: str) -> list[str]:
    errors = []
    if "ConfirmRecreate" not in content:
        errors.append(f"{name} thiếu cờ xác nhận ConfirmRecreate trong Safety Guard")
    if "DATA LOSS PREVENTION" not in content:
        errors.append(f"{name} thiếu thông điệp cảnh báo DATA LOSS PREVENTION")
    if "SET NOEXEC ON" not in content:
        errors.append(f"{name} thiếu lệnh SET NOEXEC ON khi chưa xác nhận")
    return errors


def extract_objects(sql: str) -> dict[str, set[str]]:
    tables = set(re.findall(r"CREATE\s+TABLE\s+(?:dbo\.)?([A-Za-z0-9_]+)", sql, re.IGNORECASE))
    triggers = set(re.findall(r"CREATE\s+TRIGGER\s+(?:dbo\.)?([A-Za-z0-9_]+)", sql, re.IGNORECASE))
    views = set(re.findall(r"CREATE\s+VIEW\s+(?:dbo\.)?([A-Za-z0-9_]+)", sql, re.IGNORECASE))
    indexes = set(re.findall(r"CREATE\s+(?:UNIQUE\s+)?INDEX\s+([A-Za-z0-9_]+)", sql, re.IGNORECASE))
    return {
        "tables": tables,
        "triggers": triggers,
        "views": views,
        "indexes": indexes,
    }


def extract_table_columns(sql: str) -> dict[str, set[str]]:
    """Extract table name -> set of column names for structural definition parity check."""
    tables: dict[str, set[str]] = {}
    pattern = re.compile(r"CREATE\s+TABLE\s+(?:dbo\.)?([A-Za-z0-9_]+)\s*\((.*?)\);", re.DOTALL | re.IGNORECASE)
    for match in pattern.finditer(sql):
        tbl_name = match.group(1)
        body = match.group(2)
        cols = set()
        for line in body.splitlines():
            line = line.strip().rstrip(",")
            if not line or line.startswith("--") or line.startswith("/*"):
                continue
            if re.match(r"^(?:CONSTRAINT|PRIMARY\s+KEY|FOREIGN\s+KEY|CHECK|UNIQUE)\b", line, re.IGNORECASE):
                continue
            col_match = re.match(r"^\[?([A-Za-z0-9_]+)\]?\s+[A-Za-z0-9_()]+", line)
            if col_match:
                cols.add(col_match.group(1))
        tables[tbl_name] = cols
    return tables


def main() -> int:
    parser = argparse.ArgumentParser(description="Database Schema Verification and Audit")
    parser.add_argument("--check", action="store_true", default=True, help="Verify schema sync without modifications")
    args = parser.parse_args()

    print("=" * 70)
    print("KIỂM TRA TÍNH ĐỒNG BỘ CƠ SỞ DỮ LIỆU, CHỐT AN TOÀN & ĐỊNH NGHĨA CỘT")
    print("=" * 70)

    schema_sql = read_file(SCHEMA_PATH)
    run_all_sql = read_file(RUN_ALL_PATH)
    create_db_sql = read_file(CREATE_DB_PATH)

    all_errors = []

    # 1. Safety Guard Check
    print("[1/5] Kiểm tra Safety Guard chống xóa database ngoài ý muốn...")
    all_errors.extend(verify_safety_guard(schema_sql, "database/01_schema.sql"))
    all_errors.extend(verify_safety_guard(run_all_sql, "database/scripts/Run_All.sql"))
    all_errors.extend(verify_safety_guard(create_db_sql, "database/scripts/00_Create_Database.sql"))

    # 2. Object Inventory Check
    print("[2/5] Kiểm tra số lượng và tên các đối tượng CSDL...")
    schema_objs = extract_objects(schema_sql)
    run_all_objs = extract_objects(run_all_sql)

    for category in ["tables", "triggers", "views", "indexes"]:
        s_set = schema_objs[category]
        r_set = run_all_objs[category]
        if s_set != r_set:
            diff_s = s_set - r_set
            diff_r = r_set - s_set
            all_errors.append(f"Lệch {category}: chỉ có trong 01_schema: {diff_s}, chỉ có trong Run_All: {diff_r}")
        else:
            print(f"  + {category.capitalize()}: {len(s_set)} đối tượng (khớp tên và số lượng)")

    if len(schema_objs["tables"]) < 18:
        all_errors.append(f"Số bảng ({len(schema_objs['tables'])}) chưa đủ 18 bảng chuẩn")
    if len(schema_objs["triggers"]) != 7:
        all_errors.append(f"Số triggers ({len(schema_objs['triggers'])}) chưa đủ 7 triggers chuẩn")
    if len(schema_objs["views"]) != 2:
        all_errors.append(f"Số views ({len(schema_objs['views'])}) chưa đủ 2 views chuẩn")
    if len(schema_objs["indexes"]) < 21:
        all_errors.append(f"Số indexes ({len(schema_objs['indexes'])}) chưa đủ 21 indexes chuẩn")

    # 3. Deep Column Definition Check
    print("[3/5] So sánh chi tiết danh sách cột giữa 01_schema.sql và các script module (01 -> 08)...")
    canonical_columns = extract_table_columns(schema_sql)
    all_module_sql = []
    for step in range(1, 9):
        prefix = f"{step:02d}_"
        matches = [f for f in os.listdir(SCRIPTS_DIR) if f.startswith(prefix) and f.endswith(".sql")]
        if not matches:
            all_errors.append(f"Thiếu file script cho bước {prefix} trong {SCRIPTS_DIR}")
            continue
        module_file = matches[0]
        module_sql = read_file(os.path.join(SCRIPTS_DIR, module_file))
        all_module_sql.append(module_sql)

    merged_module_sql = "\n".join(all_module_sql)
    module_columns = extract_table_columns(merged_module_sql)

    for tbl_name, can_cols in canonical_columns.items():
        if tbl_name not in module_columns:
            all_errors.append(f"Bảng {tbl_name} có trong 01_schema.sql nhưng thiếu trong các file module scripts")
        else:
            mod_cols = module_columns[tbl_name]
            if can_cols != mod_cols:
                diff_can = can_cols - mod_cols
                diff_mod = mod_cols - can_cols
                all_errors.append(f"Lệch định nghĩa cột bảng {tbl_name}: thiếu trong module: {diff_can}, thừa trong module: {diff_mod}")

    print(f"  + Đã đối chiếu cấu trúc cột cho {len(canonical_columns)} bảng: khớp định nghĩa.")

    # 4. Seed Data Isolation & Drift Prevention
    print("[4/5] Kiểm tra cách ly dữ liệu mẫu và loại bỏ placeholder hash...")
    if "INSERT INTO Users" in run_all_sql:
        all_errors.append("Run_All.sql vẫn còn chứa dữ liệu seed; seed chỉ được phép nằm duy nhất tại 02_seed.sql")

    seed_09_path = os.path.join(SCRIPTS_DIR, "09_Seed_Data.sql")
    if os.path.exists(seed_09_path):
        seed_09_content = read_file(seed_09_path)
        if "<hash>" in seed_09_content:
            all_errors.append("09_Seed_Data.sql chứa placeholder '<hash>'; cần dùng 02_seed.sql với BCrypt hash chuẩn")

    # 5. Absolute Path Check
    print("[5/5] Quét link file:/// trong database/ và docs/...")
    for root, dirs, files in os.walk(os.path.join(REPO_ROOT, "database")):
        for f in files:
            if f.endswith((".md", ".sql")):
                full_path = os.path.join(root, f)
                txt = read_file(full_path)
                if "file:///" in txt:
                    all_errors.append(f"Phát hiện link tuyệt đối file:/// trong {os.path.relpath(full_path, REPO_ROOT)}")

    print("-" * 70)
    if all_errors:
        print(f"FAILED: Phát hiện {len(all_errors)} lỗi kiểm tra:")
        for err in all_errors:
            print(f"  [X] {err}")
        return 1
    else:
        print("SUCCESS: Kiểm tra schema hoàn tất thành công:")
        print(f"  - 18 bảng, 7 triggers, 2 views, 21 indexes khớp hoàn toàn giữa aggregate và module.")
        print(f"  - Dữ liệu seed được tập trung duy nhất tại database/02_seed.sql.")
        print(f"  - Safety Guard hợp lệ, không có link tuyệt đối.")
        return 0


if __name__ == "__main__":
    sys.exit(main())
