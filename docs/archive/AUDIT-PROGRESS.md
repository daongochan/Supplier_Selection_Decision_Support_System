<!-- ARCHIVE_HEADER -->
> [!NOTE]
> **ARCHIVED DOCUMENT — SUPERSEDED**
> **Archived Date:** 2026-10-24  
> **Base Commit:** \d5b98db2b8b732bf0ce5491793aa73ba9136b678\  
> **Status:** Historical point-in-time document preserved for traceability. The active canonical documentation is located in \docs/\ (\DESIGN-SYSTEM.md\, \ARCHITECTURE.md\, \ALGORITHMS.md\, \WORKFLOW-AND-PERMISSIONS.md\, \docs/qa/\).

---

# System Audit & Remediation Progress Tracker

**Repository:** `https://github.com/daongochan/Supplier_Selection_Decision_Support_System`  
**Baseline Commit:** `662a569d8c907458bbe61be4951be5785a07c6d8` (`main`)  
**Target Branch:** `fix/system-audit-20261023`  
**Audit Date:** 2026-10-23

---

## 1. Phase Progress Checklist

- [x] **Phase 0: Baseline Verification & Full Endpoint/Use-Case Inventory**
  - [x] Verify `.NET 10.0.401` SDK, SQL Server (`sqlcmd`), and Python verification scripts.
  - [x] Checkout branch `fix/system-audit-20261023` from `main` (`662a569d8c907458bbe61be4951be5785a07c6d8`).
  - [x] Run baseline `dotnet build`, `dotnet test` (150 unit/integration tests pass; `PerfBenchmarkRunner` identified as running live benchmarks during `dotnet test`), `verify_schema_sync.py`.
  - [x] Complete full inventory of 14 controllers, all actions, roles, views, JS modules, and SQL triggers.
  - [x] Create `docs/AUDIT-PROGRESS.md` and `docs/USE-CASE-MATRIX.md` (70 use cases mapped).
- [x] **Phase 1: Reproduce & Fix P0 / P1 Data Integrity & Security Findings**
  - [x] **F01 (P0):** Stored DOM XSS in `Views/EvaluationBatch/Create.cshtml` (`innerHTML` supplier rendering) & audit all JS/Razor sinks.
  - [x] **F02 (P0):** Score model binding (`ModelState.IsValid`), explicit clear vs invalid cell handling, foreign criteria rejection, and no-op save preservation in `EvaluationController` / `EvaluationService`.
  - [x] **F03 (P0):** Aggregate concurrency protocol across `EvaluationBatch` and child mutations (`EvaluationService.SaveAsync`, `AHPService.SaveComparisonsAsync`, `AHPService.CalculateAsync`, `TOPSISService.CalculateAsync`, `EvaluationBatchService.StartAsync`/`CancelAsync`, `DecisionResultService.ApproveAsync`/`RejectAsync`/`ReevaluateAsync`) using `RowVersion` compare-and-swap inside SQL Server transactions + SQL completed/cancelled lock triggers.
  - [x] **F04 (P0):** Cookie session validation (`OnValidatePrincipal`) against live user status, role, and password/security stamp + safe local `returnUrl` redirect on login.
  - [x] **F05 (P1):** Immutable append-only `Approvals` decision history (`Superseded` status/notes instead of `RemoveRange`) and no-op save preservation.
  - [x] **F06 (P1):** Self-contained frozen `FinalApproval` and `Reevaluation` snapshots in `BatchRoundSnapshots`, snapshot-backed `DecisionResultService` & `ReportService` for completed batches, and read-only round history inspection.
  - [x] **F08 (P1):** `AbortController` and request sequence guard in `Views/EvaluationBatch/Create.cshtml` for rapid product-group switching, preserving selected supplier IDs.
  - [x] **F09 (P1):** Explicit shared batch AHP pairwise matrix labeling and concurrent overwrite protection.
  - [x] **F10 (P1):** Serializable/locking transaction around last-active-administrator check in `UserService.UpdateAsync` and `ToggleStatusAsync`.
  - [x] **F15 (P2):** Pagination `int` overflow protection and page clamping in `PagedResult.CreateAsync` and `AuditLogService.SearchAsync`.
- [x] **Phase 2: Shared Workflow Readiness Service & Use-Case Matrix Verification**
  - [x] Introduce `BatchWorkflowReadinessService` / `BatchWorkflowReadiness` model shared across `EvaluationBatch`, `Evaluation`, `AHP`, `TOPSIS`, `DecisionResult`, `Report`, and `Dashboard`.
  - [x] Verify all 70 use cases in `docs/USE-CASE-MATRIX.md` with automated tests.
- [x] **Phase 3: UI/UX — Consistent English, Mobile Navigation, Progressive Disclosure & Accessible Modals**
  - [x] Update `<html lang="en">`, unify layout, navigation, login, and core workflow screens in clear English while keeping DB enum/status constants intact.
  - [x] Render mobile navigation toggle (`#sidebarToggle`) in `Views/Shared/_Layout.cshtml` with `aria-expanded`, `aria-controls`, overlay click, and Escape close.
  - [x] Reorder `Views/TOPSIS/Index.cshtml` so final ranking appears before intermediate matrices (`<details>` progressive disclosure for Normalized Matrix, Weighted Matrix, Ideal Solutions, and Sensitivity Analysis).
  - [x] Upgrade `wwwroot/js/site.js` confirmation modal: `checkValidity()`/`reportValidity()` before opening modal, `form.requestSubmit()`, focus trap, Escape key, and error recovery.
- [x] **Phase 4: Performance & Benchmark Runner Remediation**
  - [x] Fix `PerfBenchmarkRunner` (F14) so it does not silently pass when DB is missing or overwrite committed JSON during normal `dotnet test`; fix warmup/percentile calculation.
  - [x] Eliminate duplicate `TOPSISService.GetWorkspaceAsync` recomputation in `DecisionResultService` and `ReportService` (F13).
  - [x] Make `tools/verify/verify_mcdm.py` runnable with a pure-Python fallback when `numpy` is not installed, while keeping exact NumPy comparison when present.
- [x] **Phase 5: Comprehensive Verification & CI**
  - [x] Add C# tests verifying `AHPCalculationService` and `TOPSISCalculationService` against `tools/verify/mcdm_benchmarks.json` (`McdmBenchmarkParityTests`).
  - [x] Add disposable SQL Server relational integration & concurrency tests (`SupplierSelectionDSS_Test_<guid>` in `SqlServerConcurrencyAndTriggerTests`).
  - [x] Run full build, test suite (`164 passed, 0 failed`), schema sync check, and MCDM verification.
- [x] **Phase 6: Documentation, Conventional Commits, Push & Pull Request**
  - [x] Write `docs/SYSTEM-AUDIT.md`, `docs/USE-CASE-MATRIX.md`, `docs/WORKFLOW-AND-PERMISSIONS.md`, `docs/UX-CHANGES.md`, `docs/PERFORMANCE-RESULTS.md`, `docs/RELEASE-NOTES.md`, and update `CHANGELOG.md` and `AGENTS.md`.
  - [x] Commit in logical Conventional Commits, push `fix/system-audit-20261023`, and open PR to `main`.

---

## 2. Finding Status Matrix (F01 – F15)

| ID | Priority | Area | Summary | Status |
|---|---|---|---|---|
| **F01** | P0 | Security / XSS | `Views/EvaluationBatch/Create.cshtml` interpolates `supplierCode` & `supplierName` into `innerHTML` | Fixed & Verified |
| **F02** | P0 | Data Integrity | `EvaluationController.Save` skips `ModelState.IsValid` and `EvaluationService.SaveAsync` deletes existing scores when bound value is `null` | Fixed & Verified |
| **F03** | P0 | Concurrency | `EvaluationBatch.RowVersion` is not checked or updated on child mutations (Scores, AHP, TOPSIS, Reject); SQL triggers do not lock `Completed`/`Cancelled` batches | Fixed & Verified |
| **F04** | P0 | Auth / Session | Cookie authentication lacks `OnValidatePrincipal` to revoke/refresh sessions when user status, role, or password changes | Fixed & Verified |
| **F05** | P1 | Auditability | `EvaluationService` and `AHPService` call `RemoveRange` on nonfinal `Approvals`, erasing rejection/reevaluation history; no-op saves invalidate TOPSIS | Fixed & Verified |
| **F06** | P1 | Reproducibility | `FinalApproval` snapshot lacks full metadata/inputs; `DecisionResultService` & `ReportService` query live entities instead of frozen snapshot | Fixed & Verified |
| **F07** | P1 | Responsive UX | `_Layout.cshtml` omits `_Topbar` / `#sidebarToggle`, leaving mobile viewports (`<= 900px`) without a menu button | Fixed & Verified |
| **F08** | P1 | Frontend Async | `Create.cshtml` supplier fetch lacks `AbortController` / request sequence guard and loses checked suppliers on group re-selection | Fixed & Verified |
| **F09** | P1 | Domain Clarity | AHP uses a single shared pairwise comparison matrix per batch; needs explicit UI disclosure and concurrency guard | Fixed & Verified |
| **F10** | P1 | Concurrency | Last active administrator check in `UserService` is not serialized inside a locking transaction | Fixed & Verified |
| **F11** | P2 | Accessibility / JS | `site.js` confirmation handler uses `form.submit()` (bypassing HTML5 validation) and lacks dialog focus trap | Fixed & Verified |
| **F12** | P2 | UX / Language | Mixed Vietnamese and English across views/messages; TOPSIS displays intermediate matrices before final ranking; inconsistent readiness logic | Fixed & Verified |
| **F13** | P2 | Performance | `DecisionResultService` and `ReportService` recompute full TOPSIS workspace and sensitivity analysis on every read | Fixed & Verified |
| **F14** | P2 | Test / Perf | `PerfBenchmarkRunner` runs as a regular `[Fact]`, silently passes when DB is unavailable, or mutates `tools/perf/*.json` during `dotnet test` | Fixed & Verified |
| **F15** | P2 | Reliability | `PagedResult.CreateAsync` and `AuditLogService.SearchAsync` compute `(pageNumber - 1) * pageSize` before clamping `pageNumber`, risking `int` overflow | Fixed & Verified |
