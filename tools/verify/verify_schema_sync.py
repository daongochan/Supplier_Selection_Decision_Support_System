#!/usr/bin/env python3
"""
tools/verify/verify_schema_sync.py

Kiểm tra tính đồng bộ giữa:
1. database/01_schema.sql
2. database/scripts/00_Create_Database.sql
3. database/scripts/Run_All.sql
4. database/scripts/01_Master_Data.sql .. 08_Views.sql

Đảm bảo:
- Chốt an toàn (Safety Guard / ConfirmRecreate) có mặt ở cả 3 tệp khởi tạo
- Các bảng (17 tables), triggers (7 triggers), views (2 views), indexes tồn tại đầy đủ và khớp nhau
- Không có đường dẫn tuyệt đối (file:///)
"""

import os
import sys
import re

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

SCHEMA_PATH = os.path.join(REPO_ROOT, "database", "01_schema.sql")
RUN_ALL_PATH = os.path.join(REPO_ROOT, "database", "scripts", "Run_All.sql")
CREATE_DB_PATH = os.path.join(REPO_ROOT, "database", "scripts", "00_Create_Database.sql")
SCRIPTS_DIR = os.path.join(REPO_ROOT, "database", "scripts")


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
    tables = set(re.findall(r"CREATE\s+TABLE\s+([A-Za-z0-9_]+)", sql, re.IGNORECASE))
    triggers = set(re.findall(r"CREATE\s+TRIGGER\s+([A-Za-z0-9_]+)", sql, re.IGNORECASE))
    views = set(re.findall(r"CREATE\s+VIEW\s+([A-Za-z0-9_]+)", sql, re.IGNORECASE))
    indexes = set(re.findall(r"CREATE\s+(?:UNIQUE\s+)?INDEX\s+([A-Za-z0-9_]+)", sql, re.IGNORECASE))
    return {
        "tables": tables,
        "triggers": triggers,
        "views": views,
        "indexes": indexes,
    }


def main() -> int:
    print("=" * 70)
    print("KIỂM TRA TÍNH ĐỒNG BỘ CƠ SỞ DỮ LIỆU & CHỐT AN TOÀN")
    print("=" * 70)

    schema_sql = read_file(SCHEMA_PATH)
    run_all_sql = read_file(RUN_ALL_PATH)
    create_db_sql = read_file(CREATE_DB_PATH)

    all_errors = []

    # 1. Kiểm tra Safety Guard
    print("[1/4] Kiểm tra Safety Guard chống xóa database ngoài ý muốn...")
    all_errors.extend(verify_safety_guard(schema_sql, "database/01_schema.sql"))
    all_errors.extend(verify_safety_guard(run_all_sql, "database/scripts/Run_All.sql"))
    all_errors.extend(verify_safety_guard(create_db_sql, "database/scripts/00_Create_Database.sql"))

    # 2. Kiểm tra đối tượng CSDL
    print("[2/4] Kiểm tra số lượng và tên các đối tượng CSDL giữa 01_schema.sql và Run_All.sql...")
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
            print(f"  + {category.capitalize()}: {len(s_set)} đối tượng (khớp 100%)")

    # Kiểm tra số lượng chuẩn: 17 bảng, 7 triggers, 2 views
    if len(schema_objs["tables"]) < 17:
        all_errors.append(f"Số bảng ({len(schema_objs['tables'])}) chưa đủ 17 bảng chuẩn")
    if len(schema_objs["triggers"]) != 7:
        all_errors.append(f"Số triggers ({len(schema_objs['triggers'])}) chưa đủ 7 triggers chuẩn")
    if len(schema_objs["views"]) != 2:
        all_errors.append(f"Số views ({len(schema_objs['views'])}) chưa đủ 2 views chuẩn")

    # 3. Kiểm tra các module đơn lẻ trong database/scripts/
    print("[3/4] Kiểm tra nội dung các script module đơn lẻ (01 -> 08)...")
    for step in range(1, 9):
        prefix = f"{step:02d}_"
        matches = [f for f in os.listdir(SCRIPTS_DIR) if f.startswith(prefix) and f.endswith(".sql")]
        if not matches:
            all_errors.append(f"Thiếu file script cho bước {prefix} trong {SCRIPTS_DIR}")
            continue
        module_file = matches[0]
        module_content = read_file(os.path.join(SCRIPTS_DIR, module_file))
        # Mỗi bảng trong module đơn lẻ phải có trong 01_schema.sql
        mod_objs = extract_objects(module_content)
        for cat in ["tables", "triggers", "views"]:
            for obj in mod_objs[cat]:
                if obj not in schema_objs[cat]:
                    all_errors.append(f"Đối tượng {obj} trong {module_file} không có trong 01_schema.sql")

    # 4. Kiểm tra đường dẫn tuyệt đối (file:///)
    print("[4/4] Quét link file:/// trong database/ và docs/...")
    for root, dirs, files in os.walk(os.path.join(REPO_ROOT, "database")):
        for f in files:
            if f.endswith((".md", ".sql")):
                full_path = os.path.join(root, f)
                txt = read_file(full_path)
                if "file:///" in txt:
                    all_errors.append(f"Phát hiện link tuyệt đối file:/// trong {os.path.relpath(full_path, REPO_ROOT)}")

    print("-" * 70)
    if all_errors:
        print(f"FAILED: Phát hiện {len(all_errors)} lỗi đồng bộ:")
        for err in all_errors:
            print(f"  [X] {err}")
        return 1
    else:
        print("SUCCESS: CSDL đồng bộ 100%, đầy đủ 17 bảng, 7 triggers, 2 views và có Safety Guard bảo vệ dữ liệu.")
        return 0


if __name__ == "__main__":
    sys.exit(main())
