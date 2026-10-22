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
"""

import os
import subprocess
import sys
import tempfile

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SCHEMA_SQL = os.path.join(REPO_ROOT, "database", "01_schema.sql")
SEED_SQL = os.path.join(REPO_ROOT, "database", "02_seed.sql")
PERF_SEED_SQL = os.path.join(REPO_ROOT, "tools", "perf", "seed_perf_data.sql")


def run_sqlcmd(sql_file: str, db_name: str = "master", extra_vars: list = None) -> None:
    cmd = ["sqlcmd", "-S", ".", "-E", "-C", "-d", db_name]
    if extra_vars:
        for v in extra_vars:
            cmd.extend(["-v", v])
    cmd.extend(["-i", sql_file])

    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if proc.returncode != 0:
        print(f"Error running sqlcmd for {sql_file}:\nSTDOUT:\n{proc.stdout}\nSTDERR:\n{proc.stderr}", file=sys.stderr)
        sys.exit(1)
    else:
        # print non-empty output
        out = proc.stdout.strip()
        if out:
            print(out)


def create_perf_schema() -> None:
    print("[1/3] Preparing schema for SupplierSelectionDSS_Perf...")
    with open(SCHEMA_SQL, "r", encoding="utf-8") as f:
        content = f.read()

    # Replace SupplierSelectionDSS with SupplierSelectionDSS_Perf
    perf_content = content.replace("SupplierSelectionDSS", "SupplierSelectionDSS_Perf")

    with tempfile.NamedTemporaryFile("w", encoding="utf-8", suffix=".sql", delete=False) as tmp:
        tmp.write(perf_content)
        tmp_path = tmp.name

    try:
        print("  Executing schema creation on SQL Server (.) ...")
        run_sqlcmd(tmp_path, db_name="master", extra_vars=["ConfirmRecreate=YES"])
    finally:
        if os.path.exists(tmp_path):
            os.remove(tmp_path)


def apply_base_seed() -> None:
    print("[2/3] Applying base seed to SupplierSelectionDSS_Perf...")
    with open(SEED_SQL, "r", encoding="utf-8") as f:
        content = f.read()

    perf_content = content.replace("SupplierSelectionDSS", "SupplierSelectionDSS_Perf")
    with tempfile.NamedTemporaryFile("w", encoding="utf-8", suffix=".sql", delete=False) as tmp:
        tmp.write(perf_content)
        tmp_path = tmp.name

    try:
        run_sqlcmd(tmp_path, db_name="SupplierSelectionDSS_Perf")
    finally:
        if os.path.exists(tmp_path):
            os.remove(tmp_path)


def apply_perf_data() -> None:
    print("[3/3] Generating realistic benchmark data (200 suppliers, 40 batches, 30,000+ audit logs)...")
    run_sqlcmd(PERF_SEED_SQL, db_name="SupplierSelectionDSS_Perf")


def verify_perf_db() -> None:
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
    cmd = ["sqlcmd", "-S", ".", "-E", "-C", "-d", "SupplierSelectionDSS_Perf", "-Q", query]
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    print(proc.stdout)


if __name__ == "__main__":
    print("=" * 70)
    print("INITIALIZING PERFORMANCE BENCHMARK DATABASE: SupplierSelectionDSS_Perf")
    print("=" * 70)
    create_perf_schema()
    apply_base_seed()
    apply_perf_data()
    print("Benchmark Database Verification:")
    verify_perf_db()
    print("SupplierSelectionDSS_Perf is ready for performance testing.")
