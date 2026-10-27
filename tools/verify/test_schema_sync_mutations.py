#!/usr/bin/env python3
"""
tools/verify/test_schema_sync_mutations.py

Mutation test suite for verify_schema_sync.py.
Verifies that the semantic schema comparator detects:
  1. Data type changes (e.g. DECIMAL(10,4) -> DECIMAL(10,3))
  2. Nullability changes (NULL -> NOT NULL)
  3. Constraint modifications (CHECK expression, FOREIGN KEY)
  4. Trigger condition changes
  5. Index column order changes
  6. Column addition or removal
And ensures:
  7. Pure comment or formatting/whitespace changes do NOT produce false positives.
"""

import copy
import sys
import os

# Add tools/verify to sys.path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from verify_schema_sync import parse_schema, compare_schemas, SCHEMA_PATH, read_file


def run_mutation_tests():
    print("======================================================================")
    print("RUNNING SCHEMA COMPARATOR MUTATION TESTS")
    print("======================================================================")

    canonical_sql = read_file(SCHEMA_PATH)
    canonical_snapshot = parse_schema(canonical_sql)

    # Baseline sanity: canonical compared against itself must have 0 diffs
    baseline_diffs = compare_schemas(canonical_snapshot, canonical_snapshot, "self")
    assert len(baseline_diffs) == 0, f"Self-comparison produced errors: {baseline_diffs}"
    print("[Test 0] Self-comparison baseline: 0 diffs (PASSED)")

    # 1. Mutate DECIMAL(10,4) to DECIMAL(10,3) in AHPPairwiseComparisons
    mutated_sql_1 = canonical_sql.replace(
        "ComparisonValue  DECIMAL(10,4)",
        "ComparisonValue  DECIMAL(10,3)"
    )
    snap_1 = parse_schema(mutated_sql_1)
    diffs_1 = compare_schemas(canonical_snapshot, snap_1, "mutation_1")
    assert any("ComparisonValue" in d and "data type" in d for d in diffs_1), f"Failed to detect DECIMAL precision change: {diffs_1}"
    print("[Test 1] Mutation: DECIMAL(10,4) -> DECIMAL(10,3) detected correctly (PASSED)")

    # 2. Mutate NULL to NOT NULL on Users.Phone
    mutated_sql_2 = canonical_sql.replace(
        "Phone         NVARCHAR(20)   NULL,",
        "Phone         NVARCHAR(20)   NOT NULL,"
    )
    snap_2 = parse_schema(mutated_sql_2)
    diffs_2 = compare_schemas(canonical_snapshot, snap_2, "mutation_2")
    assert any("Phone" in d and "nullability" in d for d in diffs_2), f"Failed to detect NULL/NOT NULL change: {diffs_2}"
    print("[Test 2] Mutation: NULL -> NOT NULL on Users.Phone detected correctly (PASSED)")

    # 3. Mutate CHECK constraint expression in Criteria
    mutated_sql_3 = canonical_sql.replace(
        "CHECK (CriteriaType IN (N'Benefit', N'Cost'))",
        "CHECK (CriteriaType IN (N'Benefit', N'Cost', N'Neutral'))"
    )
    snap_3 = parse_schema(mutated_sql_3)
    diffs_3 = compare_schemas(canonical_snapshot, snap_3, "mutation_3")
    assert any("CK_Criteria_Type" in d for d in diffs_3), f"Failed to detect CHECK constraint change: {diffs_3}"
    print("[Test 3] Mutation: CHECK constraint modification detected correctly (PASSED)")

    # 4. Mutate FOREIGN KEY reference in BatchRoundSnapshots
    mutated_sql_4 = canonical_sql.replace(
        "REFERENCES Users(UserID)",
        "REFERENCES Users(UserID) ON DELETE CASCADE"
    )
    snap_4 = parse_schema(mutated_sql_4)
    diffs_4 = compare_schemas(canonical_snapshot, snap_4, "mutation_4")
    assert any("FK_BatchRoundSnapshots_User" in d for d in diffs_4), f"Failed to detect FOREIGN KEY change: {diffs_4}"
    print("[Test 4] Mutation: FOREIGN KEY ON DELETE clause detected correctly (PASSED)")

    # 5. Mutate condition inside a trigger body
    mutated_sql_5 = canonical_sql.replace(
        "eb.Status IN (N'Completed', N'Cancelled')",
        "eb.Status IN (N'Draft', N'Cancelled')"
    )
    snap_5 = parse_schema(mutated_sql_5)
    diffs_5 = compare_schemas(canonical_snapshot, snap_5, "mutation_5")
    assert any("TR_EvaluationScores_ValidateCriteria" in d for d in diffs_5), f"Failed to detect trigger body change: {diffs_5}"
    print("[Test 5] Mutation: Trigger condition logic change detected correctly (PASSED)")

    # 6. Mutate index column order
    mutated_sql_6 = canonical_sql.replace(
        "CREATE INDEX IX_EvaluationScores_Batch_Supplier ON EvaluationScores(BatchID, SupplierID);",
        "CREATE INDEX IX_EvaluationScores_Batch_Supplier ON EvaluationScores(SupplierID, BatchID);"
    )
    snap_6 = parse_schema(mutated_sql_6)
    diffs_6 = compare_schemas(canonical_snapshot, snap_6, "mutation_6")
    assert any("IX_EvaluationScores_Batch_Supplier" in d and "column definition/order" in d for d in diffs_6), f"Failed to detect index column order change: {diffs_6}"
    print("[Test 6] Mutation: Index column order swap detected correctly (PASSED)")

    # 7. Add / remove column
    mutated_sql_7 = canonical_sql.replace(
        "Description   NVARCHAR(500)  NULL,",
        ""
    )
    snap_7 = parse_schema(mutated_sql_7)
    diffs_7 = compare_schemas(canonical_snapshot, snap_7, "mutation_7")
    assert any("Description" in d and "missing" in d for d in diffs_7), f"Failed to detect removed column: {diffs_7}"
    print("[Test 7] Mutation: Column removal detected correctly (PASSED)")

    # 8. Negative Test: Formatting and comment changes MUST NOT cause false positive
    formatted_sql = (
        "-- Header comment added\n/* Block comment added */\n" +
        canonical_sql.replace("    ", "  ").replace("CREATE TABLE", "create table") +
        "\n\n-- Footer comment\n"
    )
    snap_8 = parse_schema(formatted_sql)
    diffs_8 = compare_schemas(canonical_snapshot, snap_8, "formatting_test")
    assert len(diffs_8) == 0, f"Formatting / comments caused false positives: {diffs_8}"
    print("[Test 8] Semantic formatting & comment resilience: 0 false positives (PASSED)")

    print("\n----------------------------------------------------------------------")
    print("ALL 8 MUTATION AND SEMANTIC INVARIANCE TESTS PASSED SUCCESSFULLY!")
    print("----------------------------------------------------------------------")
    return 0


if __name__ == "__main__":
    sys.exit(run_mutation_tests())
