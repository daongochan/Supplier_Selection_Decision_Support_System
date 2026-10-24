# CHANGELOG — Project Change Log

All notable changes to the architecture, algorithms, security, and features of the **Supplier Selection DSS** system will be documented in this file in accordance with the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standard.

---

## [1.5.0] - 2026-10-24

### Full-Sweep QA Audit & Blank Screen (P0) Remediation

* **Complete Elimination of 100% Blank Screen Errors (P0):**
* **Experimental Reproduction:** Automatically scanned 230 route × role combinations, accurately detecting and cataloging 73 instances returning HTTP 404 with a 0-byte empty body (`Content-Length: 0`).
* **Pipeline Configuration:** Activated `app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}")` to intercept all empty-body HTTP status codes 400-599 for GET/HEAD requests, ensuring 100% of failing routes display a valid HTML page with a `#main-content` structure, logo, and navigation.
* **POST Endpoint Protection:** Added middleware restricting `StatusCodePages` to apply only to GET/HEAD to preserve the original status codes for CSRF-rejected POST forms (HTTP 400 Bad Request).


* **Error Page UI Redesign (`Views/Shared/Error.cshtml`):**
* Designed a luxurious Executive Navy+Gold UI card, synchronized with the system's Design System.
* Localized friendly, clear Vietnamese error messages for each response code: 404 (Page Not Found), 403 (Access Denied), 400 (Bad Request / CSRF Expired), 429 (Too Many Requests), 500 (Internal Server Error).
* Integrated quick action buttons: "Go Back", "Return to Dashboard", along with displaying the `RequestId` for log lookup.


* **Fixed Broken Link on Dashboard (P0):**
* Fixed the non-existent `ApprovalController` link at `Views/Dashboard/Index.cshtml:796` to `DecisionResultController.Index` with `batchId`, directing managers to the correct evaluation batch approval screen.


* **Graceful Routing for MCDM Controllers (AHP, TOPSIS, DecisionResult, Evaluation, Report):**
* Added default routes without passing `batchId` (`/AHP`, `/TOPSIS`, `/DecisionResult`, `/Evaluation`, `/Report`).
* Replaced `NotFound()` behavior with `RedirectToAction("Index", "EvaluationBatch")` alongside a `TempData["WarningMessage"]` instructing the user to select an evaluation batch.


* **Added CSS Design Tokens & Dark Theme Palette (`tokens.css`):**
* Added 5 missing CSS tokens: `--brand-primary`, `--primary`, `--muted-dark`, `--shadow-elevated`, `--shadow-sm`.
* Added all `[data-theme="dark"]` / `[data-bs-theme="dark"]` variables with high contrast, eliminating the risk of white text on a white background when the browser enables dark mode.


* **Request Localization Configuration:**
* Configured `vi-VN` with `NumberDecimalSeparator = "."` and `NumberGroupSeparator = ","` so the model binder accurately parses decimals from both HTML5 inputs and text formats.


* **Expanded Test Suite:**
* Added integration test suite `SupplierSelectionDSS.Tests/Integration/RouteSmokeAndBlankScreenSweepTests.cs`.
* Increased the total number of automated tests from 190 to **199 tests**, 100% Passed.
* Re-ran the crawler: **0 blank screen instances** across all 230 route × role combinations.



---

## [1.4.2] - 2026-10-23

### AHP Reciprocal Input Overhaul & Workflow Hardening

* **AHP Pairwise Scale Centralization & Reciprocal Mapping (Priority 1):**
* Added `Constants/SaatyScale.cs` with the complete 17-point Saaty scale (`1/9` to `9`), providing bidirectional mapping, exact 4-decimal fractions (`0.3333m` <-> `3.0000m`, `0.1111m` <-> `9.0000m`), and automated reciprocal calculation.
* Replaced decimal inputs in `Views/AHP/Index.cshtml` with discrete dropdowns defaulting to `"Select..."`, featuring instant lower-triangle reciprocal mirroring and real-time natural language pair explanations.
* Resolved floating point reciprocal drift that previously caused `1 / 0.1111 = 9.0009m`, strictly satisfying database constraint `CK_AHPComp_Value (BETWEEN 0.1111 AND 9)`.
* Adjusted `AHPCalculationService.ReciprocityTolerance` to `0.01` to safely accommodate standard 4-decimal fraction representations.
* Replaced technical RowVersion warning with a user-friendly concurrency advisory banner.


* **Criteria Creation Blank Screen Fix:**
* Added `@section Styles` referencing `criteria.css` in `Views/Criteria/Create.cshtml`.
* Loaded base jQuery in `Views/Shared/_ValidationScriptsPartial.cshtml` before unobtrusive validation to eliminate `ReferenceError: jQuery is not defined`.
* Added top header back navigation and standardized validation feedback messages in English.


* **Report Route HTTP 404 Resolution:**
* Corrected report view link in `Views/Dashboard/Index.cshtml` to target `Index` with `batchId`.
* Added backward-compatible `[HttpGet] [Route("Report/ViewReport")]` and `[Route("Report/Index")]` endpoints in `ReportController` to smoothly redirect legacy link structures.


* **Product Group Supplier Assignment Overhaul:**
* Replaced collapsed inline checkbox list with a modern 2-column responsive card grid in `Views/ProductGroup/Suppliers.cshtml` and `wwwroot/css/pages/product-group.css`.
* Added instant search text filtering, selected counter badge, bulk selection controls ("Select All Visible", "Clear All"), full-card clickability, and unsaved changes navigation warnings.
* Preserved already-assigned inactive suppliers with dedicated `(Inactive)` badges.


* **Audit Log Sensitive Credential Masking & Diff Pre-formatting:**
* Added regex sanitization in `AuditLogService` redacting `PasswordHash`, `password`, `token`, and `secret` in both logging and search workflows.
* Formatted JSON diff views in `Views/AuditLog/Index.cshtml` with scrollable `<pre class="audit-change-pre">` containers.


* **English UI Synchronization & Automated Test Suite Expansion:**
* Localized remaining Vietnamese UI strings across Criteria, Report, AHP, and Product Group screens into clean English.
* Expanded automated test suite from 164 to **190 passing xUnit tests** with `CriteriaWorkflowTests` and `SaatyScaleAndWorkflowHardeningTests`.



---

## [1.4.1] - 2026-10-23

### UI Assets, Layout Consistency & English Workspace Synchronization

* **Bootstrap Icons Font Path Fix:**
* Corrected `@font-face` URL in `wwwroot/css/bootstrap-icons.css` from `fonts/bootstrap-icons.woff2` to `../fonts/bootstrap-icons/bootstrap-icons.woff2` (and `.woff`), resolving HTTP 404/decode failures and restoring all glyph icons across sidebar, breadcrumbs, buttons, KPI cards, tables, and empty states.


* **Product Groups & Shared Layout Polish:**
* Promoted `.filter-card` with consistent padding (`20px 24px`) and bottom margin (`24px`) into shared `wwwroot/css/components/cards.css` and `product-group.css`, eliminating border flushness and table contact.
* Standardized `.btn-action-dots` across shared `wwwroot/css/components/buttons.css` and `product-group.css`, replacing unstyled browser-native buttons with refined dropdown triggers.
* Added `aria-haspopup="true"`, dynamic `aria-expanded` attributes, and `Escape` key focus restoration to dropdown menus in both `ProductGroup/Index.cshtml` and `Supplier/Index.cshtml`.


* **CSS Pipeline & Typography Unification:**
* Modernized typography token `--font-serif` to clean enterprise sans-serif (`-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`) across all headers, cards, and labels.
* Added `font-variant-numeric: tabular-nums` to `table`, `.data-table`, `.card-value`, `.ranking-score`, `.kpi-value`, and metric cells for aligned financial/operational numbers.
* Made `tools/perf/bundle_css.py` portable across all operating systems using repository-relative root calculation, and regenerated `wwwroot/css/site.css` (77.6 KB).


* **Dashboard Data Clarity & English UI Synchronization:**
* Synchronized complete English UI across Suppliers, Product Groups, and Dashboard views (page headers, KPI labels, filters, table headers, status badges, confirmation modals, and empty states).
* Clarified trend chart metric as **Supplier Participations** across batches with subtitle `"Monthly batch count and total supplier participations across batches"`.
* Upgraded donut legend and tooltip percentages to decimal formatting (`(val * 100.0 / total).ToString("0.#") + "%"`), resolving integer division truncation.
* Consolidated duplicate empty boxes into a single informative 2-column card (`.panel-card-span2`) when TOPSIS calculations are pending, providing clear guidance and role-aware action links.
* Made workflow next-step links and action buttons role-aware, gracefully falling back to `EvaluationBatch/Details` for unauthorized roles to prevent 403 errors.
* Updated `AGENTS.md` language guidelines to standardize on English administrative UI.



---

## [1.4.0] - 2026-10-23

### Comprehensive System Audit Remediation (Security, Concurrency, Auditability & UX)

* **Security & Session Integrity (P0 — F01, F04):**
* Replaced all `innerHTML` string interpolation in `Views/EvaluationBatch/Create.cshtml` with safe DOM creation (`createElement`, `textContent`, `replaceChildren`) to eliminate stored DOM XSS sinks.
* Added `UserCookieSecurityStamp` (`dss:security_stamp`) and `CookieAuthenticationOptions.Events.OnValidatePrincipal` in `Program.cs` to validate active user status, password hash stamp, and live role on every request; added safe local `ReturnUrl` redirection on login.


* **Aggregate Concurrency & Data Integrity (P0/P1 — F02, F03, F10):**
* Updated `EvaluationController.Save` and `EvaluationService.SaveAsync` to inspect `ModelState.IsValid`, preserve raw invalid inputs without deleting existing scores, require explicit `ClearedCells` flags to delete cells, and treat unchanged score saves as no-ops.
* Added `BatchConcurrencyHelper` to verify and atomically bump `EvaluationBatch.RowVersion` (`WHERE BatchID = @id AND RowVersion = @rv`) inside transactions across all batch, score, AHP, TOPSIS, and decision mutations.
* Updated SQL Server triggers (`01_schema.sql`, `Run_All.sql`, `07_Triggers.sql`, `20261023_trigger_batch_state_locks.sql`) to block child table mutations when a batch is `Completed` or `Cancelled`.
* Wrapped administrator role/status updates in `IsolationLevel.Serializable` transactions in `UserService` to prevent concurrent demotions from leaving zero active administrators.


* **Immutable Decision History & Frozen Snapshots (P1 — F05, F06):**
* Removed `RemoveRange` on non-final `Approvals`; decision events remain append-only and are marked as `[Superseded ...]` when scores or AHP weights change.
* Upgraded `BatchRoundSnapshotData` (`SchemaVersion = 2`) to store full supplier/criterion metadata, raw scores, AHP weights, TOPSIS matrices, ranking, and sensitivity stability; updated `DecisionResultService` and `ReportService` to serve completed batches directly from frozen snapshots.


* **Workflow Readiness, UX & Accessibility (P1/P2 — F07, F08, F09, F11, F12):**
* Introduced `BatchWorkflowReadinessService` shared across `EvaluationBatch/Details` and `DashboardService`.
* Rendered `_Topbar.cshtml` in `_Layout.cshtml` with `#sidebarToggle` (`aria-controls="appSidebar"`, `aria-expanded`) for viewports `<= 900px`.
* Added `AbortController`, request sequence counter, and supplier selection preservation in `Views/EvaluationBatch/Create.cshtml`.
* Added Shared Batch Matrix disclosure banner in `Views/AHP/Index.cshtml`, progressive disclosure (`<details>`) in `Views/TOPSIS/Index.cshtml`, and native `checkValidity()`/`requestSubmit()` + keyboard focus trap in `wwwroot/js/site.js`.


* **Performance, Pagination & Automated Verification (P2 — F13, F14, F15):**
* Eliminated duplicate `TOPSISService.GetWorkspaceAsync` calls in `DecisionResultService` and `ReportService`, gated `PerfBenchmarkRunner` behind `DSS_RUN_PERF=1`, and protected pagination against `int` overflow.
* Expanded automated test suite to **164 passing xUnit tests**, including `McdmBenchmarkParityTests`, `AuditFindingsRegressionTests`, and disposable SQL Server `SqlServerConcurrencyAndTriggerTests`.



---

## [1.3.0] - 2026-10-22

### Performance, UI & Business Workflow Integrity Optimization (Round 3 Optimization)

* **Measurement & Data Access Optimization:**
* **Dashboard:** Reduced SQL queries by 53% (from 17 down to 8 statements), eliminated Cartesian product (`Suppliers` × `Criteria`), added `IMemoryCache` buffering (30 seconds) for 6 system KPIs, limited dropdowns to `Take(50)`. p50 response time dropped from **25,649.8 ms** to **155.2 ms** (faster by **165x / 99.4% reduction**).
* **Audit Log:** Added `IX_AuditLogs_ActionTime` index on 10,000 log rows. p50 response time dropped from **580.4 ms** to **40.1 ms** (faster by **14.5x / 93.1% reduction**).
* **Split Queries:** Applied `.AsSplitQuery()` for simultaneous eager loading queries of `Suppliers` and `Criteria` in `EvaluationService`, `ReportService`, and `TOPSISService`, preventing data transfer explosion.


* **Static Asset Delivery & Optimization:**
* **100% Font & Icon Localization:** Downloaded and embedded all Source Serif 4 (WOFF2) fonts and Bootstrap Icons v1.11.3 into local storage (`wwwroot/fonts/`), completely removing CDN links (`fonts.googleapis.com`, `cdn.jsdelivr.net`), enabling the system to run 100% independently in intranet/offline environments.
* **Eliminated `@import` waterfall:** Bundled all tokens, typography, layouts, and components into `site.css` (76 KB), removing the sequence of 26 sequentially loaded sub-files. Specific views load custom CSS via `@section Styles` with `asp-append-version="true"`.
* **Response Compression & Caching:** Enabled Brotli + Gzip compression (`CompressionLevel.Fastest`) and configured `Cache-Control: public, max-age=31536000, immutable` for versioned static assets.
* **WebP Image Conversion:** Reduced system image size by 75.2% (`access-denied-duck.webp` 4.1 KB, `login-left.webp` 18.7 KB, `sidebar-bg.webp` 2.9 KB) with `<picture>` fallback tags.


* **Accessibility (A11y) & User Experience (UX):**
* Added **Skip-link** (`#main-content`) and standardized `:focus-visible` for full keyboard navigation support.
* Mobile Sidebar equipped with a blur backdrop, body scroll locking (`overflow: hidden`), and support for closing via the `Escape` key or outside clicks.
* AHP pairwise comparison matrix enhanced with `scope="col"`, `scope="row"`, detailed `aria-label` attributes for each Saaty scale cell, and sticky row/column headers (`position: sticky`) during horizontal/vertical scrolling.
* Dashboard charts assigned `role="img"`, `aria-label`, and hidden data summary tables for screen readers (`.sr-only`).
* Separated all inline scripts on the AHP page to `wwwroot/js/pages/ahp.js` and cleaned up 35 static `style="..."` attributes on the Dashboard into CSS classes within `dashboard.css`.
* Supported motion-sensitive users via `@media (prefers-reduced-motion: reduce)`.


* **Business Logic & Data Hardening:**
* **Evaluation Round Snapshot Logging (C1):** Added `BatchRoundSnapshots` table to preserve full UTF-8 JSON integrity of the evaluation batch upon **Re-evaluation** or **Final Approval** requests, preventing the loss of historical round data.
* **Auto-cancel pending approvals upon score/weight modification (C2):** Automatically invalidate incomplete `Approvals` records when saving evaluation scores or updating the AHP matrix.
* **Mandatory justification when selecting non-Top 1 Supplier (C4):** Required explanation input when approving a supplier not ranked 1st by TOPSIS (`RankNo != 1`), adding a visual warning badge on the approval interface.
* **Optimistic Concurrency Control (C5):** Caught `DbUpdateConcurrencyException` and translated it into friendly Vietnamese messages across all score saving, AHP saving, and approval tasks.
* **Lock in-use criteria units (C6):** Prevented editing of the `Unit` attribute on criteria that already have evaluation scores or AHP/TOPSIS data.
* **Reuse existing reports (A5):** Provided instant download buttons for already-generated PDF/Excel reports alongside a "Regenerate" button for updates.


* **Group AHP Architecture Proposal:**
* Drafted academic design document `docs/GROUP-AHP-PROPOSAL.md` using the Geometric Mean method (AIJ by Thomas L. Saaty), including DDL designs for 2 tables `AHPEvaluatorAssignments` and `AHPEvaluatorComparisons`, mathematical formulas, and group consensus indices.


* **Database Synchronization & Testing:**
* 100% Database synchronization via patch `database/patches/20261022_round3_indexes_and_snapshots.sql`, updating `01_schema.sql` and `database/scripts/*`.
* Achieved 100% validation via the checking tool `python tools/verify/verify_schema_sync.py` (18 tables, 21 indexes, 7 triggers, 2 views).
* Automated test suite increased to **151 test cases** (`151 passed, 0 failed`).



---

## [1.2.0] - 2026-10-22

### Comprehensive Hardening & CI Automation (Round 2 Hardening)

* **Compilation Fixes & 100% Green CI:** Fixed P0 return type error of `PagedResult<T>.CreateAsync`, localized CSS/font assets, and cleaned up EOL warnings.
* **Database Safety & Schema Synchronization:**
* Added `SET NOEXEC ON` safety latch and `@ConfirmRecreate = 'YES'` for database creation/deletion scripts.
* Added `tools/verify/verify_schema_sync.py` tool to automatically cross-check 17 tables, 7 triggers, 2 views, 16 indexes.
* Replaced all absolute `file:///` links with relative links.


* **Security & Separation of Concerns (Security & Hardening):**
* Separated `ILoginAttemptService` & `LoginAttemptService` using capped `IMemoryCache` (`SizeLimit = 10,000`, 15 minutes, locked by `IP + Username`) combined with ASP.NET Core IP `RateLimiter` (`"LoginLimiter"`).
* Replaced demo password hashes with standard BCrypt `$2a$11$...`.
* Implemented middleware blocking static access to `/reports/*` before `UseStaticFiles()`, returning HTTP 404 Not Found.
* Added 11 integrated security tests (`LoginAttemptServiceTests`, `SecurityIntegrationTests`).


* **Quality Enhancement & Exception Handling:**
* Eliminated `"PDF"`/`"Excel"` magic strings, injected `ILogger<T>` across all services and controllers.
* Cleaned up redundant policies, created `BusinessRuleException` class to standardize friendly error messages.


* **Optimistic Concurrency Control:**
* Added `RowVersion ROWVERSION NOT NULL` column on the `EvaluationBatches` table.
* Configured EF Core `[Timestamp]` / `IsRowVersion()` and handled `DbUpdateConcurrencyException`.
* Added patch script `database/patches/20261022_add_rowversion.sql` and concurrency verification unit tests.


* **Algorithm Verification & Coverage Measurement:**
* Collected actual Cobertura coverage: **141 tests** (141 passed).
* Coverage: `TOPSISCalculationService` 94.95% lines (89.02% branches), `SensitivityAnalysisService` 95.20% lines (90.62% branches), `AHPCalculationService` 85.48% lines (80.88% branches).
* Independently verified RGMM vs Eigenvector error margin via `tools/verify/verify_mcdm.py` ($\approx 1.2 \times 10^{-3}$).
* Drafted safe Git history cleanup documentation `docs/HISTORY_CLEANUP.md`.



---

## [1.1.0] - 2026-10-22

### Phase 1: Repository Hygiene

* Added standardized `.gitignore` for .NET and Visual Studio, preventing leakage of `.vs/`, `bin/`, `obj/`, `*.user`, `*.suo`, and personal report files.
* Untracked junk files and personal data files from the Git index (`.vs/`, `obj/`, `SupplierSelectionDSS.csproj.user`, `wwwroot/reports/*.pdf|xlsx`).
* Added `.gitattributes` to standardize EOL (CRLF/LF) and UTF-8 encoding.
* Added open-source software license file `LICENSE` (MIT License).
* Added `.editorconfig` configuration to standardize C# and Razor coding styles.

### Phase 2: Environment & Database Reproducibility

* Integrated the `SupplierSelectionDSS.Tests` test project directly into the repo in xUnit format, syncing the `.slnx` solution structure.
* Configured `<DefaultItemExcludes>` in `SupplierSelectionDSS.csproj` to exclude test code from the main build process.
* Moved `Moq` and testing libraries out of the main web project; downgraded `FluentAssertions` to `7.2.0` (safe Apache-2.0 license).
* Created `database/` directory containing script file `01_schema.sql` (17 tables, 7 integrity control triggers, 2 statistical views) and `02_seed.sql` (sample data: product groups, 8 Benefit/Cost criteria, 5 suppliers, 4 BCrypt-hashed user accounts).
* Added documentation files `AGENTS.md` and `database/README.md`.

### Phase 3: Algorithm Lock via Independent Testing

* **TOPSIS - Zero-variance criteria handling:**
* *Previous behavior:* Threw `InvalidOperationException` halting the entire evaluation process if a criterion had a value of 0 across all suppliers (e.g., error rate = 0% for all suppliers).
* *New behavior:* Does not throw an error. Normalizes this column to 0, recording the criterion index into `ZeroVarianceCriteriaIndices` within `TOPSISCalculationResult` to warn that this criterion lacks the ability to differentiate between suppliers.


* **TOPSIS - Ranking with equal Closeness Coefficients:**
* *Previous behavior:* Assigned sequential rankings (1, 2, 3, 4) even if two suppliers had the same CC and distance $D^+$.
* *New behavior:* Applies Standard Competition Ranking (1, 2, 2, 4). Provides a `HasTies = true` flag when ties occur.


* **TOPSIS - Case $D^+ + D^- = 0$ (Identical Suppliers):**
* *Previous behavior:* Assigned $CC = 0.0$ but still assigned sequential rankings 1, 2, 3.
* *New behavior:* Mandates a minimum of 2 suppliers. When all suppliers have identical scores across all criteria, all receive $CC = 0.0$ and tie for rank 1 (`IsIndistinguishable = true`).


* **TOPSIS - Supplier Quantity Constraint:**
* Requires a minimum of 2 suppliers to perform TOPSIS multi-criteria comparison.


* **Independent Test Suite:**
* Added independent verification tool using Python & NumPy (`tools/verify/verify_mcdm.py`).
* Cross-checked AHP weights between Geometric Mean and Principal Eigenvector (error margin $< 0.0015$).
* Cross-checked 100% of TOPSIS intermediate data (PIS, NIS, $D^+$, $D^-$, CC, Rank) against an independent benchmark suite.
* Tested mathematical invariance properties: positive Scale Invariance and Permutation Invariance.
* Achieved 114 passing tests with absolute algorithm branch coverage.



### Phase 4: Security & Hardening

* **Report File Protection:** Moved all report storage outside of webroot into an isolated `App_Data/reports` directory (supporting custom configuration via `appsettings.json`).
* **Static Access Blocking:** Configured `UseStaticFiles` middleware to completely block direct calls to static `/reports/*` URLs without authentication, returning HTTP 404 Not Found.
* **Path Traversal Prevention:** Applied `Path.GetFullPath` and matched normalized directory prefixes in `ReportService.GetDownloadAsync`.
* **Login Protection:**
* User Enumeration Prevention: Standardized generic error messages for both incorrect account and incorrect password scenarios.
* Timing Attack Prevention: Executed dummy BCrypt authentication when a user does not exist.
* Account Lockout: Automatically locks login for 15 minutes after 5 consecutive failed attempts.
* Audit logging and `ILogger` system recording for all login attempts.


* **Administrator Role Protection:** Blocked disabling or demoting the last active Administrator account of the system in `UserService.UpdateAsync`.
* **Global CSRF Filter:** Added `AutoValidateAntiforgeryTokenAttribute` for all HTTP POST, PUT, DELETE requests.
* **Security Response Headers:** Automatically injected `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy: strict-origin-when-cross-origin`.
* **Health Check:** Added `/health` endpoint to check system operational status.

### Phase 5: Code Quality

* Eliminated magic strings, standardizing them into clear constants:
* `Constants/CriteriaUnits.cs`: Constant `Rating = "Điểm"` accompanied by a scale validation method.
* `Constants/ReportTypes.cs`: Constants `Pdf = "PDF"` and `Excel = "Excel"`.


* Clamped pagination parameter `pageSize` in `PagedResult.CreateAsync` (`Math.Clamp(pageSize, 1, 100)`).
* Thoroughly resolved all nullable compiler warnings (CS8602, CS8604), bringing the project's compiler warning count down to **0 warnings, 0 errors**.

### Phase 6: Academic Features

* **TOPSIS Intermediate Matrix Display:**
* Added `TOPSISMatrixRowViewModel` data structure.
* Visually displayed the Vector Normalized Matrix ($R = [r_{ij}]$) and Weighted Normalized Matrix ($V = [v_{ij}]$) on the `Views/TOPSIS/Index.cshtml` interface.
* Integrated "Normalized Matrix" and "Weighted Matrix" sheets into the Excel export file (ClosedXML).


* **Sensitivity Analysis:**
* Built `SensitivityAnalysisService` simulating weight variations of each criterion by $\pm 10\%, \pm 20\%$.
* Rebalanced the proportions of remaining criteria so the total weight is always conserved exactly at 1.0.
* Evaluated the stability of the leading supplier (#1), detected sensitive criteria, and calculated the `RobustnessPercentage`.
* Displayed the sensitivity analysis table on the TOPSIS interface, Approval Decision page (`DecisionResult`), Excel export file, and PDF report (QuestPDF).
* Added automated unit test suite for `SensitivityAnalysisService` (bringing total tests to 123).



### Phase 7: Academic Documentation & CI Pipeline

* Drafted in-depth mathematical documentation `docs/ALGORITHMS.md`.
* Drafted software architecture, security, and business lifecycle documentation `docs/ARCHITECTURE.md`.
* Drafted bilingual `README.md` (English - Vietnamese) alongside bare-metal project setup instructions.
* Updated all statuses in the independent audit report `docs/AUDIT.md`.
* Established automated CI Pipeline using GitHub Actions `.github/workflows/ci.yml` on .NET 10.
