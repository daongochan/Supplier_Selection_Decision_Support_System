# Release Notes — v1.4.0 (2026-10-23)

## Overview

Release **v1.4.0** delivers a comprehensive security, data integrity, concurrency, auditability, and UX remediation across the **Supplier Selection Decision Support System (Supplier Selection DSS)** following the full system audit of baseline commit `662a569d8c907458bbe61be4951be5785a07c6d8`.

---

## Highlights

### 1. Security & Session Integrity (P0)
- **DOM XSS Elimination (F01):** Replaced all `innerHTML` string interpolation in `Views/EvaluationBatch/Create.cshtml` with safe DOM node construction (`createElement`, `textContent`, `replaceChildren`).
- **Live Cookie Principal & Security Stamp Validation (F04):** Added `UserCookieSecurityStamp` (`dss:security_stamp`) and `CookieAuthenticationOptions.Events.OnValidatePrincipal` in `Program.cs`. Locking or deactivating an account, resetting a password, or changing a user's role takes effect immediately on the next HTTP request across all active sessions (including 7-day `RememberMe` cookies). Added safe local `ReturnUrl` redirection on sign-in.

### 2. Aggregate Concurrency & Database Invariants (P0 / P1)
- **Non-Destructive Score Model Binding (F02):** `EvaluationController.Save` validates `ModelState.IsValid` and preserves raw user inputs on binding/validation errors so invalid entries never delete existing stored scores. Added explicit `ClearedCells` tracking and prevented no-op score saves from wiping valid TOPSIS results.
- **Aggregate `RowVersion` Concurrency Protocol (F03):** Created `BatchConcurrencyHelper` to verify and atomically bump `EvaluationBatch.RowVersion` (`WHERE BatchID = @id AND RowVersion = @rv`) inside transactions across all batch, score, AHP, TOPSIS, and decision mutations.
- **SQL Trigger State Locks (F03):** Enhanced SQL Server triggers (`database/01_schema.sql`, `database/scripts/Run_All.sql`, `database/scripts/07_Triggers.sql`, `database/patches/20261023_trigger_batch_state_locks.sql`) to reject any mutation to child records when a batch is `Completed` or `Cancelled`.
- **Serialized Last-Admin Protection (F10):** Wrapped administrator role/status updates in `IsolationLevel.Serializable` transactions on SQL Server so concurrent demotions can never leave zero active administrators.

### 3. Immutable Audit Trail & Frozen Historical Snapshots (P1)
- **Append-Only Decision History (F05):** Removed `RemoveRange` on non-final `Approvals`. Prior `Rejected` and `PendingReevaluation` records remain in an append-only timeline and are marked as `[Superseded ...]` when scores or AHP weights change.
- **Self-Contained Frozen Snapshots (F06):** Upgraded `BatchRoundSnapshotData` (`SchemaVersion = 2`) to freeze full supplier/criterion metadata, raw scores, AHP weights, TOPSIS matrices, ranking, and sensitivity stability. Completed batches reconstruct `DecisionResult` views and PDF/Excel reports directly from the frozen snapshot so master-data renames never alter historical records.

### 4. Shared Workflow Readiness, UX & Accessibility (P1 / P2)
- **Shared Readiness Engine:** Added `BatchWorkflowReadinessService` used consistently by `EvaluationBatch/Details` and `DashboardService`.
- **Mobile Navigation Drawer (F07):** Rendered `_Topbar.cshtml` in `_Layout.cshtml` with `#sidebarToggle` (`aria-controls="appSidebar"`, `aria-expanded`) and responsive CSS for viewports $\le 900\text{px}$.
- **Async Race Guard on Product Group Selection (F08):** Added `AbortController`, request sequence counter, and selection preservation in `Views/EvaluationBatch/Create.cshtml`.
- **Shared AHP Matrix Disclosure (F09):** Added an explicit banner in `Views/AHP/Index.cshtml` clarifying that pairwise comparisons are shared at the batch level and guarded by `RowVersion`.
- **Accessible Confirmation Dialog (F11):** Updated `wwwroot/js/site.js` to run `form.checkValidity()`/`form.reportValidity()` before opening `#dssConfirmModal`, submit via `form.requestSubmit()`, handle async errors cleanly, and trap keyboard focus.
- **TOPSIS Progressive Disclosure & Consistent English UI (F12):** Reordered `Views/TOPSIS/Index.cshtml` so final rankings appear before collapsible `<details>` intermediate matrices, and unified the application shell and core workflow views in English (`<html lang="en">`).

### 5. Performance, Pagination & Verification Suite (P2)
- **Eliminated Duplicate TOPSIS Recomputations (F13):** Served completed batches directly from frozen snapshots in `DecisionResultService` and `ReportService`.
- **Gated Benchmark Runner (F14) & Overflow-Safe Pagination (F15):** Gated `PerfBenchmarkRunner` behind `DSS_RUN_PERF=1` / `DSS_WRITE_PERF_FILES=1` and protected pagination skip calculations with `long` arithmetic.
- **Expanded Verification Suite:** Added `McdmBenchmarkParityTests`, `AuditFindingsRegressionTests`, and `SqlServerConcurrencyAndTriggerTests` (**164 automated tests passing**).
