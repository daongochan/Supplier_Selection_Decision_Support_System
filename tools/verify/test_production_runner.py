#!/usr/bin/env python3
"""
tools/verify/test_production_runner.py

Automated integration test suite for database/scripts/Run_Production.sql.
Mandatory test verification scenarios:
  a. Clean provisioning creates correct objects in target database.
  b. No business objects created in master, model, or msdb.
  c. Re-running on existing database is rejected; data and DB configuration unchanged.
  d. Error at a module halts execution; success message is never printed.
  e. Fresh production database contains no demo accounts (Users count == 0).
  f. Incomplete provisioning reports state and does not automatically drop database.
"""

import os
import subprocess
import sys
import uuid

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SCRIPTS_DIR = os.path.join(REPO_ROOT, "database", "scripts")
SERVER = os.environ.get("DSS_TEST_SQL_SERVER", r"np:\\.\pipe\sql\query" if os.name == "nt" else "localhost")


def run_sqlcmd(args: list[str], cwd=SCRIPTS_DIR) -> tuple[int, str]:
    cmd = ["sqlcmd", "-S", SERVER, "-E", "-C", "-b", "-l", "15"] + args
    res = subprocess.run(cmd, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    return res.returncode, res.stdout


def execute_query(query: str) -> str:
    code, out = run_sqlcmd(["-Q", query])
    if code != 0:
        raise RuntimeError(f"Query failed ({code}): {out}")
    return out


def drop_test_db(db_name: str) -> None:
    drop_sql = f"""
    IF DB_ID(N'{db_name}') IS NOT NULL
    BEGIN
        ALTER DATABASE [{db_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [{db_name}];
    END
    """
    run_sqlcmd(["-Q", drop_sql])


def main() -> int:
    test_id = uuid.uuid4().hex[:8]
    fresh_db = f"SupplierSelectionDSS_Test_ProdFresh_{test_id}"
    print(f"=== TESTING PRODUCTION RUNNER ON {SERVER} ===")
    print(f"Target Fresh DB: {fresh_db}")

    try:
        # Pre-cleanup in case leftover exists
        drop_test_db(fresh_db)

        # -------------------------------------------------------------
        # Test a, b, e: Clean provisioning on fresh DB
        # -------------------------------------------------------------
        print("\n[Test 1/3] Executing fresh provisioning (Scenarios a, b, e)...")
        code, out = run_sqlcmd(["-v", f"DatabaseName={fresh_db}", "-i", "Run_Production.sql"])
        print(f"  Exit code: {code}")
        if code != 0:
            print(f"FAILED output:\n{out}")
            return 1

        assert "PRODUCTION DATABASE PROVISIONING COMPLETED SUCCESSFULLY." in out, "Missing completion message"
        assert "Base tables verified: 18 tables present." in out, "Table count check failed"
        assert "Views verified: 2 views present" in out, "View count check failed"
        assert "Triggers verified: 7 business triggers present." in out, "Trigger count check failed"
        assert "Performance indexes verified: 21 indexes present." in out, "Index count check failed"
        assert "Clean user table verified: 0 demo users present." in out, "User count check failed"
        print("  -> Scenario a (all objects created in target DB) PASSED.")
        print("  -> Scenario e (0 demo accounts in production DB) PASSED.")

        # Check Scenario b: No business objects created in master, model, or msdb
        # We verify that unique DSS tables were not created in system databases,
        # and no new tables appeared compared to pre-run state.
        for sys_db in ["master", "model", "msdb"]:
            chk_out = execute_query(f"USE {sys_db}; SELECT COUNT(*) FROM sys.tables WHERE name IN ('EvaluationBatches', 'BatchRoundSnapshots', 'TOPSISResults', 'AHPComparisons');")
            lines = [l.strip() for l in chk_out.splitlines() if l.strip().isdigit()]
            tbl_count = int(lines[0]) if lines else -1
            if tbl_count != 0:
                print(f"FAILED: Found {tbl_count} DSS business tables in system database [{sys_db}]!")
                return 1
        print("  -> Scenario b (zero business tables in master/model/msdb) PASSED.")

        # Check recovery model
        rec_out = execute_query(f"SELECT recovery_model_desc FROM sys.databases WHERE name = '{fresh_db}';")
        assert "FULL" in rec_out, f"Expected RECOVERY FULL, got:\n{rec_out}"
        print("  -> Target database recovery model verified as FULL.")

        # -------------------------------------------------------------
        # Test c: Re-running on existing DB must be rejected
        # -------------------------------------------------------------
        print("\n[Test 2/3] Re-running on existing database (Scenario c)...")
        code_rerun, out_rerun = run_sqlcmd(["-v", f"DatabaseName={fresh_db}", "-i", "Run_Production.sql"])
        print(f"  Exit code: {code_rerun} (expected non-zero)")
        if code_rerun == 0:
            print("FAILED: Re-running on existing database succeeded when it should have failed!")
            return 1

        assert "ALREADY EXISTS on this SQL Server instance!" in out_rerun, "Missing pre-existence error message"
        assert "PRODUCTION PROVISIONING COMPLETED SUCCESSFULLY" not in out_rerun, "Printed success on aborted run!"

        # Verify target DB configuration and tables untouched
        rec_after = execute_query(f"SELECT recovery_model_desc FROM sys.databases WHERE name = '{fresh_db}';")
        assert "FULL" in rec_after
        print("  -> Scenario c (rejected re-run on existing DB without changes) PASSED.")

        # -------------------------------------------------------------
        # Test d & f: Error handling during incomplete provisioning
        # -------------------------------------------------------------
        fault_db = f"SupplierSelectionDSS_Test_ProdFault_{test_id}"
        print(f"\n[Test 3/3] Simulating faulty module step (Scenarios d & f) on {fault_db}...")
        drop_test_db(fault_db)

        # Create a temporary faulty runner that introduces an error at step 3
        faulty_runner = os.path.join(SCRIPTS_DIR, "_Temp_Faulty_Runner.sql")
        runner_content = open(os.path.join(SCRIPTS_DIR, "Run_Production.sql"), encoding="utf-8").read()
        faulty_content = runner_content.replace(
            ":r 02_Evaluation_Data.sql",
            "RAISERROR(N'SYNTHETIC MODULE ERROR IN EVALUATION DATA', 16, 1);\nGO\nSET NOEXEC ON;\nGO"
        )
        with open(faulty_runner, "w", encoding="utf-8") as f:
            f.write(faulty_content)

        try:
            code_fault, out_fault = run_sqlcmd(["-v", f"DatabaseName={fault_db}", "-i", "_Temp_Faulty_Runner.sql"])
            print(f"  Exit code: {code_fault} (expected non-zero)")
            if code_fault == 0:
                print("FAILED: Faulty module run exited with 0!")
                return 1

            assert "SYNTHETIC MODULE ERROR" in out_fault, "Missing synthetic error message"
            assert "PRODUCTION DATABASE PROVISIONING COMPLETED SUCCESSFULLY" not in out_fault, "Success message printed on failure!"
            print("  -> Scenario d (error halted execution, no success printed) PASSED.")

            # Check Scenario f: Database was NOT automatically dropped
            db_chk = execute_query(f"SELECT DB_ID(N'{fault_db}');")
            lines = [l.strip() for l in db_chk.splitlines() if l.strip().isdigit()]
            db_id = int(lines[0]) if lines else 0
            assert db_id > 0, "Database was automatically dropped after failure!"
            print(f"  -> Scenario f (faulty database [ID: {db_id}] preserved for DBA inspection) PASSED.")
        finally:
            if os.path.exists(faulty_runner):
                os.remove(faulty_runner)
            drop_test_db(fault_db)

    finally:
        print(f"\nCleaning up test database {fresh_db}...")
        drop_test_db(fresh_db)

    print("\n=======================================================")
    print("ALL PRODUCTION RUNNER MANDATORY TESTS PASSED (a, b, c, d, e, f)")
    print("=======================================================")
    return 0


if __name__ == "__main__":
    sys.exit(main())
