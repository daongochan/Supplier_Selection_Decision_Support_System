#!/usr/bin/env python3
"""
tools/perf/setup_perf_db.py

Idempotent setup for the performance benchmark database: SupplierSelectionDSS_Perf.
1. Creates SupplierSelectionDSS_Perf from database/01_schema.sql (with ConfirmRecreate=YES).
2. Applies database/02_seed.sql base seed.
3. Generates realistic benchmark workload:
   - 200 Suppliers across 10 Product Groups
   - 12 Criteria (Benefit/Cost, Point/Unit)
   - 40 Evaluation Batches (Completed, Evaluating, Draft, Cancelled) spanning 6 months
   - Complete AHP + TOPSIS results + Approvals + Reports for completed batches
   - 30,000+ AuditLogs across the last 6 months

Safety:
  - Accepts ONLY target databases named SupplierSelectionDSS_Perf or ending with _Perf / _Test.
  - Refuses to touch production or development database SupplierSelectionDSS.
"""

import argparse
import os
import subprocess
import sys
import tempfile

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SCHEMA_SQL = os.path.join(REPO_ROOT, "database", "01_schema.sql")
SEED_SQL = os.path.join(REPO_ROOT, "database", "02_seed.sql")
PERF_SEED_SQL = os.path.join(REPO_ROOT, "tools", "perf", "seed_perf_data.sql")


def run_sqlcmd(server: str, sql_file: str, db_name: str = "master", extra_vars: list = None) -> None:
    cmd = ["sqlcmd", "-S", server, "-E", "-C", "-b", "-d", db_name]
    if extra_vars:
        for v in extra_vars:
            cmd.extend(["-v", v])
    cmd.extend(["-i", sql_file])

    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if proc.returncode != 0:
        print(f"Error running sqlcmd for {sql_file}:\nSTDOUT:\n{proc.stdout}\nSTDERR:\n{proc.stderr}", file=sys.stderr)
        sys.exit(proc.returncode or 1)
    else:
        out = proc.stdout.strip()
        if out:
            print(out)


def create_perf_schema(server: str, db_name: str) -> None:
    print(f"[1/3] Preparing schema for {db_name} on {server}...")
    with open(SCHEMA_SQL, "r", encoding="utf-8") as f:
        content = f.read()

    # Replace SupplierSelectionDSS with db_name
    perf_content = content.replace("SupplierSelectionDSS", db_name)

    with tempfile.NamedTemporaryFile("w", encoding="utf-8", suffix=".sql", delete=False) as tmp:
        tmp.write(perf_content)
        tmp_path = tmp.name

    try:
        print(f"  Executing schema creation on SQL Server ({server}) ...")
        run_sqlcmd(server, tmp_path, db_name="master", extra_vars=["ConfirmRecreate=YES"])
    finally:
        if os.path.exists(tmp_path):
            os.remove(tmp_path)


def apply_base_seed(server: str, db_name: str) -> None:
    print(f"[2/3] Applying base seed to {db_name}...")
    with open(SEED_SQL, "r", encoding="utf-8") as f:
        content = f.read()

    perf_content = content.replace("SupplierSelectionDSS", db_name)
    with tempfile.NamedTemporaryFile("w", encoding="utf-8", suffix=".sql", delete=False) as tmp:
        tmp.write(perf_content)
        tmp_path = tmp.name

    try:
        run_sqlcmd(server, tmp_path, db_name=db_name)
    finally:
        if os.path.exists(tmp_path):
            os.remove(tmp_path)


def apply_perf_data(server: str, db_name: str) -> None:
    print("[3/3] Generating realistic benchmark data (200 suppliers, 40 batches, 30,000+ audit logs)...")
    run_sqlcmd(server, PERF_SEED_SQL, db_name=db_name)


def verify_perf_db(server: str, db_name: str) -> None:
    query = """
    SET NOCOUNT ON;
    SELECT 'Users' AS TableName, COUNT(*) AS [RowCount] FROM Users
    UNION ALL SELECT 'ProductGroups', COUNT(*) FROM ProductGroups
    UNION ALL SELECT 'Suppliers', COUNT(*) FROM Suppliers
    UNION ALL SELECT 'Criteria', COUNT(*) FROM Criteria
    UNION ALL SELECT 'EvaluationBatches', COUNT(*) FROM EvaluationBatches
    UNION ALL SELECT 'EvaluationScores', COUNT(*) FROM EvaluationScores
    UNION ALL SELECT 'AHPResults', COUNT(*) FROM AHPResults
    UNION ALL SELECT 'TopsisResults', COUNT(*) FROM TopsisResults
    UNION ALL SELECT 'Approvals', COUNT(*) FROM Approvals
    UNION ALL SELECT 'Reports', COUNT(*) FROM Reports
    UNION ALL SELECT 'AuditLogs', COUNT(*) FROM AuditLogs;
    """
    cmd = ["sqlcmd", "-S", server, "-E", "-C", "-b", "-d", db_name, "-Q", query]
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if proc.returncode != 0:
        print(f"Error verifying {db_name}:\n{proc.stderr}", file=sys.stderr)
        sys.exit(proc.returncode or 1)
    print(proc.stdout)


def main() -> int:
    parser = argparse.ArgumentParser(description="Setup Performance Benchmark Database for Supplier Selection DSS")
    parser.add_argument("--server", type=str, default=None, help="SQL Server instance (default: DSS_TEST_SQL_SERVER env or '.')")
    parser.add_argument("--db-name", type=str, default="SupplierSelectionDSS_Perf", help="Database name (default: SupplierSelectionDSS_Perf)")
    args = parser.parse_args()

    server = args.server or os.environ.get("DSS_TEST_SQL_SERVER") or "."
    db_name = args.db_name

    # Security check: Never target production / main dev database
    if db_name == "SupplierSelectionDSS" or not (db_name.endswith(("_Perf", "_Test")) or "Perf" in db_name):
        print(f"SECURITY ERROR: Target database name '{db_name}' is not allowed.", file=sys.stderr)
        print("Performance DB setup only allows targets ending with _Perf or _Test.", file=sys.stderr)
        return 1

    print("=" * 70)
    print(f"INITIALIZING PERFORMANCE BENCHMARK DATABASE: {db_name} on {server}")
    print("=" * 70)
    create_perf_schema(server, db_name)
    apply_base_seed(server, db_name)
    apply_perf_data(server, db_name)
    print("Benchmark Database Verification:")
    verify_perf_db(server, db_name)
    print(f"{db_name} is ready for performance testing.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
