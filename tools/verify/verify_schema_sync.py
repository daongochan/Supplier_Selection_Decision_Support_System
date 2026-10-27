#!/usr/bin/env python3
"""
tools/verify/verify_schema_sync.py

Comprehensive semantic database schema verification and synchronization auditor.
Validates:
1. database/01_schema.sql (Canonical Aggregate Schema - Single Source of Truth)
2. database/scripts/Run_All.sql (Scripts Runner Schema)
3. database/scripts/00_Create_Database.sql .. 08_Views.sql (Modular Schema Definitions)
4. database/02_seed.sql (Canonical Development Seed)

Semantic Checks Performed:
  - Safety Guards (ConfirmRecreate, DATA LOSS PREVENTION, SET NOEXEC ON)
  - Tables: Table existence, column names, column data types with precision/scale,
    nullability, identity specifications, and default values.
  - Constraints: Primary Keys, Unique Constraints, Foreign Keys, and Check Constraints
    (including column sets, references, and expressions).
  - Indexes: Target tables, uniqueness, columns, and column ordering.
  - Triggers: Target tables, event actions, and semantic logic bodies.
  - Views: Normalized view definitions and query semantics.
  - Seed Isolation: No demo seeds in schema files, BCrypt hash validation.
  - File Hygiene: No hardcoded 'file:///' URLs.
"""

import argparse
import os
import re
import sys
from dataclasses import dataclass, field
from typing import Optional

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SCHEMA_PATH = os.path.join(REPO_ROOT, "database", "01_schema.sql")
RUN_ALL_PATH = os.path.join(REPO_ROOT, "database", "scripts", "Run_All.sql")
CREATE_DB_PATH = os.path.join(REPO_ROOT, "database", "scripts", "00_Create_Database.sql")
SCRIPTS_DIR = os.path.join(REPO_ROOT, "database", "scripts")
SEED_PATH = os.path.join(REPO_ROOT, "database", "02_seed.sql")


# =====================================================================
# Semantic Schema Data Structures
# =====================================================================

@dataclass
class ColumnDef:
    name: str
    data_type: str
    nullable: bool
    identity: bool = False
    default: Optional[str] = None

    def diff(self, other: "ColumnDef") -> list[str]:
        diffs = []
        if self.data_type.upper() != other.data_type.upper():
            diffs.append(f"data type (canonical: {self.data_type}, target: {other.data_type})")
        if self.nullable != other.nullable:
            diffs.append(f"nullability (canonical: {'NULL' if self.nullable else 'NOT NULL'}, target: {'NULL' if other.nullable else 'NOT NULL'})")
        if self.identity != other.identity:
            diffs.append(f"identity (canonical: {self.identity}, target: {other.identity})")
        if (self.default or "").upper() != (other.default or "").upper():
            diffs.append(f"default constraint (canonical: {self.default}, target: {other.default})")
        return diffs


@dataclass
class ConstraintDef:
    name: str
    ctype: str  # 'PK', 'UQ', 'FK', 'CK'
    columns: tuple[str, ...]
    details: str = ""  # check expression, or ref table/col

    def diff(self, other: "ConstraintDef") -> list[str]:
        diffs = []
        if self.ctype != other.ctype:
            diffs.append(f"type (canonical: {self.ctype}, target: {other.ctype})")
        if [c.lower() for c in self.columns] != [c.lower() for c in other.columns]:
            diffs.append(f"columns (canonical: {list(self.columns)}, target: {list(other.columns)})")
        if self.details.upper() != other.details.upper():
            diffs.append(f"definition details (canonical: {self.details}, target: {other.details})")
        return diffs


@dataclass
class TableDef:
    name: str
    columns: dict[str, ColumnDef] = field(default_factory=dict)
    constraints: dict[str, ConstraintDef] = field(default_factory=dict)


@dataclass
class IndexDef:
    name: str
    table: str
    is_unique: bool
    columns: tuple[tuple[str, str], ...]  # ((col, 'ASC'|'DESC'), ...)

    def diff(self, other: "IndexDef") -> list[str]:
        diffs = []
        if self.table.lower() != other.table.lower():
            diffs.append(f"target table (canonical: {self.table}, target: {other.table})")
        if self.is_unique != other.is_unique:
            diffs.append(f"uniqueness (canonical: {self.is_unique}, target: {other.is_unique})")
        if self.columns != other.columns:
            diffs.append(f"column definition/order (canonical: {self.columns}, target: {other.columns})")
        return diffs


@dataclass
class ViewDef:
    name: str
    normalized_body: str


@dataclass
class TriggerDef:
    name: str
    table: str
    events: tuple[str, ...]
    normalized_body: str


@dataclass
class SchemaSnapshot:
    tables: dict[str, TableDef] = field(default_factory=dict)
    indexes: dict[str, IndexDef] = field(default_factory=dict)
    views: dict[str, ViewDef] = field(default_factory=dict)
    triggers: dict[str, TriggerDef] = field(default_factory=dict)


# =====================================================================
# Semantic SQL Parser
# =====================================================================

def strip_sql_comments(sql: str) -> str:
    # Remove block comments
    sql = re.sub(r"/\*.*?\*/", " ", sql, flags=re.DOTALL)
    # Remove line comments
    lines = [re.sub(r"--.*$", "", line) for line in sql.splitlines()]
    return "\n".join(lines)


def normalize_whitespace(text: str) -> str:
    return " ".join(text.split()).strip()


def normalize_expr(text: str) -> str:
    t = normalize_whitespace(text)
    # Strip unnecessary outer enclosing parentheses
    while t.startswith("(") and t.endswith(")"):
        # Check if outer paren matches the entire expression
        depth = 0
        matched = True
        for i, c in enumerate(t[:-1]):
            if c == "(":
                depth += 1
            elif c == ")":
                depth -= 1
                if depth == 0:
                    matched = False
                    break
        if matched:
            t = t[1:-1].strip()
        else:
            break
    # Normalize internal whitespace around commas and operators
    t = re.sub(r"\s*,\s*", ", ", t)
    t = re.sub(r"\s*=\s*", " = ", t)
    return t


def split_table_elements(body: str) -> list[str]:
    elements = []
    current = []
    depth = 0
    for char in body:
        if char == "(":
            depth += 1
            current.append(char)
        elif char == ")":
            depth -= 1
            current.append(char)
        elif char == "," and depth == 0:
            elem = "".join(current).strip()
            if elem:
                elements.append(elem)
            current = []
        else:
            current.append(char)
    last = "".join(current).strip()
    if last:
        elements.append(last)
    return elements


def extract_create_tables(sql: str) -> list[tuple[str, str]]:
    tables = []
    pattern = re.compile(r"CREATE\s+TABLE\s+(?:dbo\.)?\[?([A-Za-z0-9_]+)\]?\s*\(", re.IGNORECASE)
    for m in pattern.finditer(sql):
        tbl_name = m.group(1)
        start_idx = m.end() - 1  # position of opening '('
        depth = 0
        end_idx = start_idx
        for i in range(start_idx, len(sql)):
            c = sql[i]
            if c == "(":
                depth += 1
            elif c == ")":
                depth -= 1
                if depth == 0:
                    end_idx = i
                    break
        body = sql[start_idx + 1:end_idx]
        tables.append((tbl_name, body))
    return tables


def parse_schema(sql: str) -> SchemaSnapshot:
    clean_sql = strip_sql_comments(sql)
    snapshot = SchemaSnapshot()

    # 1. Parse all CREATE TABLE definitions with balanced parentheses
    for tbl_name, tbl_body in extract_create_tables(clean_sql):
        tbl = TableDef(name=tbl_name)
        elements = split_table_elements(tbl_body)
        for elem in elements:
            elem_clean = normalize_whitespace(elem)
            if not elem_clean:
                continue

            # Check if constraint
            pk_m = re.match(r"^CONSTRAINT\s+\[?([A-Za-z0-9_]+)\]?\s+PRIMARY\s+KEY\s*\((.*?)\)", elem_clean, re.IGNORECASE)
            if pk_m:
                cname = pk_m.group(1)
                cols = tuple(c.strip(" []") for c in pk_m.group(2).split(","))
                tbl.constraints[cname.lower()] = ConstraintDef(cname, "PK", cols)
                continue

            uq_m = re.match(r"^CONSTRAINT\s+\[?([A-Za-z0-9_]+)\]?\s+UNIQUE\s*\((.*?)\)", elem_clean, re.IGNORECASE)
            if uq_m:
                cname = uq_m.group(1)
                cols = tuple(c.strip(" []") for c in uq_m.group(2).split(","))
                tbl.constraints[cname.lower()] = ConstraintDef(cname, "UQ", cols)
                continue

            fk_m = re.match(r"^CONSTRAINT\s+\[?([A-Za-z0-9_]+)\]?\s+FOREIGN\s+KEY\s*\((.*?)\)\s+REFERENCES\s+(?:dbo\.)?\[?([A-Za-z0-9_]+)\]?\s*\((.*?)\)(.*)", elem_clean, re.IGNORECASE)
            if fk_m:
                cname = fk_m.group(1)
                fcols = tuple(c.strip(" []") for c in fk_m.group(2).split(","))
                ref_tbl = fk_m.group(3)
                ref_cols = tuple(c.strip(" []") for c in fk_m.group(4).split(","))
                extra = normalize_whitespace(fk_m.group(5))
                details = f"{ref_tbl}({', '.join(ref_cols)}) {extra}".strip()
                tbl.constraints[cname.lower()] = ConstraintDef(cname, "FK", fcols, details)
                continue

            ck_m = re.match(r"^CONSTRAINT\s+\[?([A-Za-z0-9_]+)\]?\s+CHECK\s*\((.*)\)$", elem_clean, re.IGNORECASE)
            if ck_m:
                cname = ck_m.group(1)
                expr = normalize_expr(ck_m.group(2))
                tbl.constraints[cname.lower()] = ConstraintDef(cname, "CK", (), expr)
                continue

            # Column definition
            col_m = re.match(r"^\[?([A-Za-z0-9_]+)\]?\s+([A-Za-z0-9_]+(?:\s*\([^)]*\))?)(.*)$", elem_clean, re.IGNORECASE)
            if col_m:
                col_name = col_m.group(1)
                data_type = re.sub(r"\s+", "", col_m.group(2)).upper()
                rest = col_m.group(3)

                is_identity = bool(re.search(r"\bIDENTITY\b", rest, re.IGNORECASE))
                is_nullable = not bool(re.search(r"\bNOT\s+NULL\b", rest, re.IGNORECASE))

                default_val = None
                df_m = re.search(r"(?:CONSTRAINT\s+\[?[A-Za-z0-9_]+\]?\s+)?DEFAULT\s*\((.*?)\)(?:\s+(?:NOT\s+NULL|NULL))?$", rest, re.IGNORECASE)
                if df_m:
                    default_val = normalize_expr(df_m.group(1))

                tbl.columns[col_name.lower()] = ColumnDef(
                    name=col_name,
                    data_type=data_type,
                    nullable=is_nullable,
                    identity=is_identity,
                    default=default_val,
                )

        snapshot.tables[tbl_name.lower()] = tbl

    # 2. Parse all CREATE INDEX definitions
    for idx_match in re.finditer(r"CREATE\s+(UNIQUE\s+)?INDEX\s+\[?([A-Za-z0-9_]+)\]?\s+ON\s+(?:dbo\.)?\[?([A-Za-z0-9_]+)\]?\s*\((.*?)\)\s*;?", clean_sql, re.IGNORECASE):
        is_unique = bool(idx_match.group(1))
        idx_name = idx_match.group(2)
        idx_tbl = idx_match.group(3)
        raw_cols = idx_match.group(4)
        cols = []
        for col_entry in raw_cols.split(","):
            parts = col_entry.strip().split()
            if not parts:
                continue
            cname = parts[0].strip(" []")
            cdir = parts[1].upper() if len(parts) > 1 else "ASC"
            cols.append((cname, cdir))
        snapshot.indexes[idx_name.lower()] = IndexDef(idx_name, idx_tbl, is_unique, tuple(cols))

    # 3. Parse all CREATE VIEW definitions (split batches to get view body until next GO)
    batches = re.split(r"^\s*GO\s*$", clean_sql, flags=re.MULTILINE | re.IGNORECASE)
    for raw_batch in batches:
        batch = raw_batch.strip()
        if not batch:
            continue
        view_match = re.search(r"CREATE\s+VIEW\s+(?:dbo\.)?\[?([A-Za-z0-9_]+)\]?\s+AS\s+(.*)", batch, re.DOTALL | re.IGNORECASE)
        if view_match:
            view_name = view_match.group(1)
            view_body = normalize_whitespace(view_match.group(2).strip(";"))
            snapshot.views[view_name.lower()] = ViewDef(view_name, view_body)
            continue

        trig_match = re.search(r"CREATE\s+TRIGGER\s+(?:dbo\.)?\[?([A-Za-z0-9_]+)\]?\s+ON\s+(?:dbo\.)?\[?([A-Za-z0-9_]+)\]?\s+(AFTER|INSTEAD\s+OF)\s+(.*?)\s+AS\s+(.*)", batch, re.DOTALL | re.IGNORECASE)
        if trig_match:
            trig_name = trig_match.group(1)
            trig_tbl = trig_match.group(2)
            events = tuple(sorted(e.strip().upper() for e in trig_match.group(4).split(",")))
            trig_body = normalize_whitespace(trig_match.group(5).strip(";"))
            snapshot.triggers[trig_name.lower()] = TriggerDef(trig_name, trig_tbl, events, trig_body)
            continue

    return snapshot


# =====================================================================
# Semantic Comparator
# =====================================================================

def compare_schemas(can: SchemaSnapshot, tgt: SchemaSnapshot, target_name: str) -> list[str]:
    errors = []

    # 1. Compare Tables
    can_tables = set(can.tables.keys())
    tgt_tables = set(tgt.tables.keys())

    for missing in sorted(can_tables - tgt_tables):
        errors.append(f"Table [{can.tables[missing].name}] present in canonical schema but missing in {target_name}")
    for extra in sorted(tgt_tables - can_tables):
        errors.append(f"Table [{tgt.tables[extra].name}] extra in {target_name} but absent in canonical schema")

    for tbl_key in sorted(can_tables & tgt_tables):
        c_tbl = can.tables[tbl_key]
        t_tbl = tgt.tables[tbl_key]

        # Compare Columns
        c_cols = set(c_tbl.columns.keys())
        t_cols = set(t_tbl.columns.keys())

        for missing_col in sorted(c_cols - t_cols):
            errors.append(f"Table [{c_tbl.name}]: Column [{c_tbl.columns[missing_col].name}] missing in {target_name}")
        for extra_col in sorted(t_cols - c_cols):
            errors.append(f"Table [{c_tbl.name}]: Column [{t_tbl.columns[extra_col].name}] extra in {target_name}")

        for col_key in sorted(c_cols & t_cols):
            c_col = c_tbl.columns[col_key]
            t_col = t_tbl.columns[col_key]
            diffs = c_col.diff(t_col)
            for d in diffs:
                errors.append(f"Table [{c_tbl.name}], Column [{c_col.name}]: Mismatched {d}")

        # Compare Constraints
        c_cons = set(c_tbl.constraints.keys())
        t_cons = set(t_tbl.constraints.keys())

        for missing_con in sorted(c_cons - t_cons):
            errors.append(f"Table [{c_tbl.name}]: Constraint [{c_tbl.constraints[missing_con].name}] missing in {target_name}")
        for extra_con in sorted(t_cons - c_cons):
            errors.append(f"Table [{c_tbl.name}]: Constraint [{t_tbl.constraints[extra_con].name}] extra in {target_name}")

        for con_key in sorted(c_cons & t_cons):
            c_con = c_tbl.constraints[con_key]
            t_con = t_tbl.constraints[con_key]
            diffs = c_con.diff(t_con)
            for d in diffs:
                errors.append(f"Table [{c_tbl.name}], Constraint [{c_con.name}]: Mismatched {d}")

    # 2. Compare Indexes
    can_indexes = set(can.indexes.keys())
    tgt_indexes = set(tgt.indexes.keys())

    for missing in sorted(can_indexes - tgt_indexes):
        errors.append(f"Index [{can.indexes[missing].name}] present in canonical schema but missing in {target_name}")
    for extra in sorted(tgt_indexes - can_indexes):
        errors.append(f"Index [{tgt.indexes[extra].name}] extra in {target_name} but absent in canonical schema")

    for idx_key in sorted(can_indexes & tgt_indexes):
        c_idx = can.indexes[idx_key]
        t_idx = tgt.indexes[idx_key]
        diffs = c_idx.diff(t_idx)
        for d in diffs:
            errors.append(f"Index [{c_idx.name}]: Mismatched {d}")

    # 3. Compare Views
    can_views = set(can.views.keys())
    tgt_views = set(tgt.views.keys())

    for missing in sorted(can_views - tgt_views):
        errors.append(f"View [{can.views[missing].name}] present in canonical schema but missing in {target_name}")
    for extra in sorted(tgt_views - can_views):
        errors.append(f"View [{tgt.views[extra].name}] extra in {target_name} but absent in canonical schema")

    for view_key in sorted(can_views & tgt_views):
        c_view = can.views[view_key]
        t_view = tgt.views[view_key]
        if c_view.normalized_body.upper() != t_view.normalized_body.upper():
            errors.append(f"View [{c_view.name}]: Query definition differs between canonical schema and {target_name}")

    # 4. Compare Triggers
    can_triggers = set(can.triggers.keys())
    tgt_triggers = set(tgt.triggers.keys())

    for missing in sorted(can_triggers - tgt_triggers):
        errors.append(f"Trigger [{can.triggers[missing].name}] present in canonical schema but missing in {target_name}")
    for extra in sorted(tgt_triggers - can_triggers):
        errors.append(f"Trigger [{tgt.triggers[extra].name}] extra in {target_name} but absent in canonical schema")

    for trig_key in sorted(can_triggers & tgt_triggers):
        c_trig = can.triggers[trig_key]
        t_trig = tgt.triggers[trig_key]
        if c_trig.table.lower() != t_trig.table.lower():
            errors.append(f"Trigger [{c_trig.name}]: Target table mismatch (canonical: {c_trig.table}, target: {t_trig.table})")
        if c_trig.events != t_trig.events:
            errors.append(f"Trigger [{c_trig.name}]: Trigger events mismatch (canonical: {c_trig.events}, target: {t_trig.events})")
        if c_trig.normalized_body.upper() != t_trig.normalized_body.upper():
            errors.append(f"Trigger [{c_trig.name}]: Execution body logic differs between canonical schema and {target_name}")

    return errors


# =====================================================================
# Main Audit Verification
# =====================================================================

def read_file(path: str) -> str:
    if not os.path.exists(path):
        raise FileNotFoundError(f"Missing file: {path}")
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def verify_safety_guard(content: str, name: str) -> list[str]:
    errors = []
    if "ConfirmRecreate" not in content:
        errors.append(f"{name}: Missing ConfirmRecreate variable check in safety guard")
    if "DATA LOSS PREVENTION" not in content:
        errors.append(f"{name}: Missing DATA LOSS PREVENTION warning banner")
    if "SET NOEXEC ON" not in content:
        errors.append(f"{name}: Missing SET NOEXEC ON protection command")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description="Database Semantic Schema Verification and Synchronization Auditor")
    parser.add_argument("--check", action="store_true", default=True, help="Verify schema sync without modifications")
    args = parser.parse_args()

    print("=" * 70)
    print("DATABASE SEMANTIC SCHEMA SYNCHRONIZATION AUDIT")
    print("=" * 70)

    schema_sql = read_file(SCHEMA_PATH)
    run_all_sql = read_file(RUN_ALL_PATH)
    create_db_sql = read_file(CREATE_DB_PATH)

    # 1. Safety Guard Check
    print("\n[Result 1/4] Safety Guards & Data Loss Prevention:")
    guard_errors = []
    guard_errors.extend(verify_safety_guard(schema_sql, "database/01_schema.sql"))
    guard_errors.extend(verify_safety_guard(run_all_sql, "database/scripts/Run_All.sql"))
    guard_errors.extend(verify_safety_guard(create_db_sql, "database/scripts/00_Create_Database.sql"))

    if guard_errors:
        for err in guard_errors:
            print(f"  [X] {err}")
    else:
        print("  [OK] Safety guards verified: ConfirmRecreate, DATA LOSS PREVENTION, and SET NOEXEC ON present.")

    # 2. Semantic Schema Equivalence Check
    print("\n[Result 2/4] Semantic Schema Equivalence (Deep Semantic Check):")
    canonical_snapshot = parse_schema(schema_sql)

    # Report canonical inventory invariants derived directly from canonical source
    print(f"  Canonical Schema Inventory (01_schema.sql):")
    print(f"    - Base Tables:       {len(canonical_snapshot.tables)}")
    print(f"    - Indexes:           {len(canonical_snapshot.indexes)}")
    print(f"    - Views:             {len(canonical_snapshot.views)}")
    print(f"    - Business Triggers: {len(canonical_snapshot.triggers)}")

    schema_errors = []

    # Check Run_All.sql against canonical
    run_all_snapshot = parse_schema(run_all_sql)
    run_all_diffs = compare_schemas(canonical_snapshot, run_all_snapshot, "database/scripts/Run_All.sql")
    schema_errors.extend(run_all_diffs)

    # Check Modular Scripts 01..08 against canonical
    module_sql_parts = []
    for step in range(1, 9):
        prefix = f"{step:02d}_"
        matches = [f for f in os.listdir(SCRIPTS_DIR) if f.startswith(prefix) and f.endswith(".sql")]
        if not matches:
            schema_errors.append(f"Missing modular script for step {prefix} in {SCRIPTS_DIR}")
            continue
        module_file = matches[0]
        module_sql_parts.append(read_file(os.path.join(SCRIPTS_DIR, module_file)))

    merged_modular_sql = "\nGO\n".join(module_sql_parts)
    modular_snapshot = parse_schema(merged_modular_sql)
    modular_diffs = compare_schemas(canonical_snapshot, modular_snapshot, "database/scripts/ (modules 01..08)")
    schema_errors.extend(modular_diffs)

    if schema_errors:
        for err in schema_errors:
            print(f"  [X] {err}")
    else:
        print("  [OK] Deep semantic parity verified across all tables, columns, data types, nullability, defaults, constraints, indexes, views, and triggers.")

    # 3. Seed Data Isolation & Drift Prevention Check
    print("\n[Result 3/4] Development Seed Data Isolation:")
    seed_errors = []
    if "INSERT INTO Users" in run_all_sql:
        seed_errors.append("Run_All.sql still contains seed records; seed data must only exist in database/02_seed.sql")

    seed_09_path = os.path.join(SCRIPTS_DIR, "09_Seed_Data.sql")
    if os.path.exists(seed_09_path):
        seed_09_content = read_file(seed_09_path)
        if "<hash>" in seed_09_content:
            seed_errors.append("09_Seed_Data.sql contains '<hash>' placeholder; canonical dev seed is database/02_seed.sql")

    if not os.path.exists(SEED_PATH):
        seed_errors.append(f"Canonical seed file not found: {SEED_PATH}")
    else:
        seed_content = read_file(SEED_PATH)
        if "$2a$11$" not in seed_content and "$2b$11$" not in seed_content:
            seed_errors.append("database/02_seed.sql does not contain valid BCrypt cost 11 password hashes")

    if seed_errors:
        for err in seed_errors:
            print(f"  [X] {err}")
    else:
        print("  [OK] Single source of truth for dev seed verified at database/02_seed.sql (BCrypt cost 11).")

    # 4. File Hygiene & Absolute Paths Check
    print("\n[Result 4/4] File Hygiene & Disallowed Absolute Links:")
    hygiene_errors = []
    for root, dirs, files in os.walk(os.path.join(REPO_ROOT, "database")):
        for f in files:
            if f.endswith((".md", ".sql")):
                full_path = os.path.join(root, f)
                txt = read_file(full_path)
                if "file:///" in txt:
                    hygiene_errors.append(f"Disallowed absolute link 'file:///' found in {os.path.relpath(full_path, REPO_ROOT)}")

    if hygiene_errors:
        for err in hygiene_errors:
            print(f"  [X] {err}")
    else:
        print("  [OK] Zero absolute file:/// paths found across all database documents and SQL scripts.")

    print("\n" + "-" * 70)
    total_errors = len(guard_errors) + len(schema_errors) + len(seed_errors) + len(hygiene_errors)
    if total_errors > 0:
        print(f"FAILED: Found {total_errors} verification error(s).")
        return 1
    else:
        print("SUCCESS: All 4 verification categories passed perfectly.")
        return 0


if __name__ == "__main__":
    sys.exit(main())
