<!-- ARCHIVE_HEADER -->
> [!NOTE]
> **ARCHIVED DOCUMENT — SUPERSEDED**
> **Archived Date:** 2026-10-24  
> **Base Commit:** \d5b98db2b8b732bf0ce5491793aa73ba9136b678\  
> **Status:** Historical point-in-time document preserved for traceability. The active canonical documentation is located in \docs/\ (\DESIGN-SYSTEM.md\, \ARCHITECTURE.md\, \ALGORITHMS.md\, \WORKFLOW-AND-PERMISSIONS.md\, \docs/qa/\).

---

# Supplier Selection DSS — Performance & Benchmark Remediation Report

**Repository:** `https://github.com/daongochan/Supplier_Selection_Decision_Support_System`  
**Branch:** `fix/system-audit-20261023`  
**Date:** 2026-10-23

---

## 1. Elimination of Redundant TOPSIS Workspace Computations (F13)

### Root Cause
In the baseline commit (`662a569d8c907458bbe61be4951be5785a07c6d8`):
1. `DecisionResultService.GetResultAsync` queried `TopsisResults` directly and also invoked `TOPSISService.GetWorkspaceAsync(batchId)` to compute tie/zero-variance flags and sensitivity analysis even when the batch was already `Completed` and had a frozen `BatchRoundSnapshot`.
2. `ReportService.LoadReportDataAsync` queried `EvaluationBatch`, `EvaluationScores`, `AHPCriteriaWeights`, `AHPResults`, `TopsisResults`, and `TopsisIdealSolutions`, and then called `TOPSISService.GetWorkspaceAsync(batchId)` again—which re-queried the same tables and re-ran full TOPSIS vector normalization, distance calculations, and $4 \times n$ sensitivity perturbations on every PDF or Excel export.

### Remediation
- **Snapshot-First Fast Path for Completed Batches:**
  - `DecisionResultService.GetResultAsync` and `ReportService.LoadReportDataAsync` now check for the frozen `FinalApproval` snapshot (`BatchRoundSnapshotData`, `SchemaVersion >= 2`) when `batch.Status == BatchStatus.Completed`.
  - When present, both services reconstruct the full ranking, criteria weights, raw decision matrix, normalized/weighted matrices, PIS/NIS, and sensitivity summary directly from the deserialized JSON snapshot in memory—**eliminating 100% of redundant `TOPSISService.GetWorkspaceAsync` SQL queries and sensitivity recomputations on completed batches**.
- **Single-Pass Workspace Evaluation for Active Batches:**
  - When a batch is still in `Evaluating` status, `TOPSISService.GetWorkspaceAsync` is invoked at most once per request, and its precomputed matrices are reused without duplicate round-trips.

---

## 2. Benchmark Runner Hygiene & Determinism (F14)

### Root Cause
`SupplierSelectionDSS.Tests/Perf/PerfBenchmarkRunner.cs` was decorated with `[Fact]` and executed unconditionally during every `dotnet test` run:
- If `SupplierSelectionDSS_Perf` did not exist on the machine, the test silently returned (`return;`), reporting a misleading green pass without running any benchmark.
- If `SupplierSelectionDSS_Perf` did exist, running `dotnet test` overwrote tracked files (`tools/perf/baseline_results.json` and `tools/perf/post_optimization_results.json`) in the working tree.

### Remediation
- Gated `PerfBenchmarkRunner` execution behind the explicit environment variable `DSS_RUN_PERF=1`:
  - During standard `dotnet test` runs (`DSS_RUN_PERF` unset), the runner exits immediately without touching SQL Server or mutating tracked repository files.
  - When `DSS_RUN_PERF=1` is set and `SupplierSelectionDSS_Perf` is missing, the runner throws an explicit `InvalidOperationException` instead of silently passing.
- Gated writing to `tools/perf/*.json` behind `DSS_WRITE_PERF_FILES=1` so benchmark verification can be executed without dirtying the git working tree unless explicitly requested.

---

## 3. Pagination Overflow Protection (F15)

- Updated `PagedResult<T>.CreateAsync` (`Models/PagedResult.cs`) and `AuditLogService.SearchAsync` (`Services/AuditLogService.cs`) to:
  1. Clamp `pageSize` to `[1, 100]` and `pageNumber` to $\ge 1$.
  2. Execute `CountAsync()` first and compute `totalPages`.
  3. Clamp `pageNumber` to `[1, Math.Max(1, totalPages)]` **before** computing `skip`.
  4. Compute `long skipLong = (long)(pageNumber - 1) * pageSize` and clamp to `int.MaxValue`, preventing 32-bit integer overflow to negative `.Skip()` offsets when `pageNumber == int.MaxValue`.
