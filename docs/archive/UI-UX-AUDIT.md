<!-- ARCHIVE_HEADER -->
> [!NOTE]
> **ARCHIVED DOCUMENT — SUPERSEDED**
> **Archived Date:** 2026-10-24  
> **Base Commit:** \d5b98db2b8b732bf0ce5491793aa73ba9136b678\  
> **Status:** Historical point-in-time document preserved for traceability. The active canonical documentation is located in \docs/\ (\DESIGN-SYSTEM.md\, \ARCHITECTURE.md\, \ALGORITHMS.md\, \WORKFLOW-AND-PERMISSIONS.md\, \docs/qa/\).

---

# SupplierSelectionDSS - UI/UX Comprehensive Audit & Analysis

**Audit Date:** September 2026  
**Auditor:** UI/UX Architect, Product Designer & Senior ASP.NET Core MVC Engineer  
**System:** SupplierSelectionDSS (Enterprise Multi-Criteria Decision Support System)  
**Tech Stack:** ASP.NET Core MVC (.NET 10), EF Core 10, SQL Server, Bootstrap Icons, Modular CSS, Vanilla JS.

---

## 1. Executive Summary

`SupplierSelectionDSS` is a mathematically sound Decision Support System (synthesizing Saaty's AHP and Hwang-Yoon's TOPSIS for multi-attribute supplier selection) built on ASP.NET Core MVC. While its calculation engines and database access layers are robust and well-tested, the **presentation layer currently suffers from fragmentation, UX friction, and aesthetic inconsistencies** that undermine its credibility as a modern enterprise decision platform.

The system currently presents as a collection of disjointed views rather than a unified **Decision Workspace**. Common UI controls (filters, steppers, status badges, alerts) have been re-implemented across different pages with differing markup, conflicting CSS selectors, and ad-hoc overrides (`!important`).

This audit systematically evaluates every screen, component, and interaction pattern to provide a factual foundation for our comprehensive redesign.

---

## 2. Global Architecture & Inconsistency Audit

### 2.1 CSS Architecture & Token Gaps
- **Ad-hoc `!important` Overrides:** `wwwroot/css/site.css` and `evaluation-batch.css` contain numerous `!important` rules to force layout overrides over `layout.css`, `base.css`, and `components/*.css`. This indicates a brittle specificity cascade.
- **Fragmented Page Styles:** Styles are split across 12 page-level CSS files (`supplier.css`, `criteria.css`, `evaluation-batch.css`, `ahp.css`, `topsis.css`, `decision-result.css`, `report.css`, `audit-log.css`, etc.) with substantial code duplication:
  - Button sizes (`88px` vs `auto`) redefined repeatedly.
  - Filter grids redefined under 5 different class names (`.supplier-filter`, `.batch-filter`, `.criteria-filter`, `.user-filter`, `.audit-filter`).
  - Card shadows, border radii, and internal paddings vary arbitrarily between 16px, 20px, and 24px.
- **Typography Inconsistencies:** The serif font (`Source Serif 4`) is used indiscriminately for technical numbers, table cell contents, form inputs, and uppercase labels. While elegant for headings and editorial copy, tabular data, numeric matrices, and code tags require tabular figures and high-legibility sans-serif fonts for optimal readability.

### 2.2 Dual Notification Problem (Toast vs. Inline Alerts)
- In `_Layout.cshtml`, the system renders `<partial name="_Toast" />`, which automatically catches `TempData["SuccessMessage"]`, `TempData["ErrorMessage"]`, `TempData["WarningMessage"]`, and `TempData["InfoMessage"]` to display floating, auto-dismissing toast notifications.
- However, across almost every Razor view (`Supplier/Index.cshtml`, `EvaluationBatch/Details.cshtml`, `AHP/Index.cshtml`, `TOPSIS/Index.cshtml`, `DecisionResult/Index.cshtml`), there are hardcoded blocks checking the exact same `TempData` keys and outputting bulky `.alert-success` and `.alert-danger` banners above the content.
- **Impact:** Users are bombarded with duplicated notifications simultaneously (both a popup toast in the corner and a banner pushing the page down).

### 2.3 Workflow Stepper Duplication
- Four different screens implement their own workflow stepper with completely divergent HTML markup and CSS classes:
  1. `EvaluationBatch/Details.cshtml`: `.batch-workflow`, `.batch-workflow-step`, `.batch-workflow-line`
  2. `AHP/Index.cshtml`: `.ahp-workflow`, `.ahp-workflow-step`, `.ahp-workflow-line`
  3. `TOPSIS/Index.cshtml`: Custom readiness card and status chips without a clear stepper
  4. `DecisionResult/Index.cshtml`: `.decision-workflow`, `.decision-workflow-step`, `.decision-workflow-line`
- **Impact:** Users lose situational awareness when transitioning between steps. There is no unified sense of progress across the 5 canonical phases of a batch: **Setup → Evaluation → AHP Weights → TOPSIS Ranking → Manager Approval**.

---

## 3. Screen-by-Screen UX & Technical Review

| Screen | Primary Purpose | Current Deficiencies & UX Friction | Priority |
| :--- | :--- | :--- | :--- |
| **01. Login** (`Account/Login`) | Authentication & Gateway | Background contains decorative orbs (`.login-orb`) that feel like a consumer SaaS landing page rather than a secure enterprise portal. Input fields lack icon indicators, focus states lack clear depth, and there is no quick role hint for test/evaluation environments. | Medium |
| **02. Dashboard** (`Dashboard/Index`) | Decision Workspace Overview | 1. Features a decorative marketing banner (`.dashboard-feature` with orb and marketing text: *"AHP weights. TOPSIS ranking. One decision workspace."*) which wastes prime screen estate.<br>2. Displays KPI counts as text numbers without interactive links to filtered data.<br>3. Tables have no direct deep-links into active decision workflows.<br>4. Lacks visual charts for criteria weights or batch completion velocity. | High |
| **03. Suppliers** (`Supplier/Index`, `Create`, `Edit`) | Supplier Directory & Status | 1. Search card previously had `Rows` dropdown mixed in (now resolved in Index).<br>2. Action buttons in table (`Edit`, `ToggleStatus`) use raw text links without icons or visual hierarchy.<br>3. Create/Edit form uses a basic 2-column grid without semantic field grouping (General Info, Contact, Business Profile). | Medium |
| **04. Product Groups** (`ProductGroup/Index`, `Create`, `Edit`) | Category & Sourcing Hierarchy | 1. Actions in table use plain inline text links.<br>2. Deactivate button has no confirmation modal or impact warning regarding associated active batches. | Medium |
| **05. Criteria** (`Criteria/Index`, `Create`, `Edit`) | DSS Criteria Configuration | 1. Cost vs. Benefit distinction is critical for TOPSIS normalization, but the badge styling is subtle and lacks explanatory tooltips.<br>2. Scale type and description are often truncated or missing in list view. | Medium |
| **06. Evaluation Batches** (`EvaluationBatch/Index`) | Sourcing Event Pipeline | 1. Batch status pill colors (`Draft`, `Evaluating`, `Completed`, `Cancelled`) lack clear semantic association.<br>2. Action links are stacked horizontally with minimal spacing.<br>3. Filter card layout does not clearly indicate batch status distribution. | High |
| **07. Batch Details** (`EvaluationBatch/Details`) | Hub for Sourcing Process | 1. Monolithic template (> 1,000 lines) attempting to render details, supplier checklists, criteria lists, score matrix previews, and action toolbars all in one scrollable page.<br>2. Multiple action buttons with unclear primary vs. secondary order.<br>3. Critical batch operations (`StartAsync`, `CancelAsync`, `ReevaluateAsync`) need standardized confirmation modals explaining state transition impacts. | High |
| **08. Evaluation Scores** (`Evaluation/Index`) | Performance Scoring Matrix | 1. Massive 2D grid (Suppliers × Criteria) that can overflow horizontally on large batches.<br>2. Missing sticky headers (Supplier names on left, Criteria codes on top).<br>3. Number inputs lack clear min/max bounds and validation hints.<br>4. Lack of completion progress indicator (e.g., "18 of 24 scores recorded"). | High |
| **09. AHP Workspace** (`AHP/Index`) | Pairwise Comparison & Weights | 1. Pairwise comparison inputs use raw select boxes without a visual reciprocal balance indicator (Saaty 1–9 scale is notoriously hard for non-experts without visual guidance).<br>2. Consistency metrics ($\lambda_{max}, CI, RI, CR$) are shown as raw numbers rather than an authoritative Decision Validation Banner.<br>3. Calculation button does not clearly communicate that calculating will cascade-invalidate existing TOPSIS rankings. | Critical |
| **10. TOPSIS Workspace** (`TOPSIS/Index`) | Ideal Solutions & Ranking | 1. Page titles use trailing periods like marketing copy (`Supplier ranking.`, `Criteria weights.`).<br>2. Closeness Coefficient ($C_i$) numbers are displayed without visual progress bars or comparative length indicators.<br>3. PIS ($A^+$) and NIS ($A^-$) separation distances ($S^+, S^-$) are buried in an unformatted data block rather than a clear diagnostic matrix.<br>4. Missing direct action to transition immediately into Decision Approval. | Critical |
| **11. Comparison** (`DecisionResult/Index`) | Multi-Attribute Comparison | 1. Comparison view only displays a raw ranking table rather than a multi-dimensional comparison matrix.<br>2. Lacks visual attribute breakdown showing *why* Rank #1 outperformed Rank #2 across individual criteria.<br>3. Does not highlight whether Rank #1 is recommended for approval. | High |
| **12. Decision & Approval** (`DecisionResult/Index` approval section) | Formal Managerial Sign-Off | 1. Conflates analytical ranking with business decision.<br>2. Approval notes input is a basic textarea without clear approval audit trail.<br>3. Re-evaluation request button does not clearly explain that it resets the batch back to `Evaluating` state. | High |
| **13. Reports** (`Report/Index`) | Document & Audit Generation | 1. Separate download triggers for PDF and Excel without preview or summary metadata.<br>2. Lacks report generation status and historical download log.<br>3. Buttons lack clear document format icons (PDF icon, Excel icon). | Medium |
| **14. User & Audit** (`User/Index`, `AuditLog/Index`) | Security & Governance | 1. Audit log table lacks JSON payload visualizer for before/after states.<br>2. Filter options for audit logs are rigid and slow to filter by date range.<br>3. User role management lacks clear visual permission summaries. | Low |

---

## 4. Accessibility (a11y) & Usability Gaps

1. **Color Contrast & State Conveyance:**
   - Status pills rely heavily on background tint (`--sage-soft`, `--lavender-soft`) with low-contrast text on some displays.
   - Criteria types (`Benefit` vs. `Cost`) are differentiated only by pastel purple and pastel blue, violating WCAG 2.1 (relying on color alone).
2. **Keyboard Navigation & Focus Management:**
   - Inputs in matrix grids (`Evaluation/Index` and `AHP/Index`) do not support Arrow key navigation across cells.
   - Custom modal dialog (`#dssConfirmModal`) lacks keyboard trap management and `aria-describedby` linkage.
3. **Form Semantics:**
   - Several inputs lack explicit `aria-describedby` pointing to validation error message spans.
   - Breadcrumb navigation previously used raw ordered lists or loose anchors; separator was not explicitly hidden with `aria-hidden="true"`.
4. **Mobile & Tablet Responsiveness:**
   - Matrix grids in Evaluation and AHP cause uncontained horizontal page overflow on screens < 1024px.
   - Action button groups wrap unpredictably and push table rows out of vertical alignment.

---

## 5. Decision Support System (DSS) Ergonomics

An enterprise DSS is fundamentally different from a CRUD admin portal:
- **CRUD Goal:** Fast, repetitive record creation and editing.
- **DSS Goal:** Synthesis of multi-source data, validation of mathematical assumptions, understanding trade-offs, and documenting accountable human decisions.

**Key DSS Ergonomic Flaws Identified:**
1. **Lack of Decision Context:** When scoring suppliers or reviewing TOPSIS ranks, users cannot easily inspect criteria weights or definitions without navigating back and forth across multiple tabs.
2. **Silent Invalidation Friction:** When AHP comparisons are modified, TOPSIS results are silently dropped in the backend (per `EvaluationBatchStateRules.cs`). The UI does not provide an explicit warning modal explaining that recalculating AHP will require re-running TOPSIS before approval can occur.
3. **Lack of Visual Sensitivity:** Numbers like $CR = 0.0421$ or $C_i = 0.7845$ are abstract without standard thresholds (e.g., $CR \le 0.10$ threshold line, or relative closeness comparison bars).

---

## 6. Audit Conclusion & Roadmap

The redesign of `SupplierSelectionDSS` must:
1. **Preserve Database & Backend Logic:** All EF Core entities, enums, calculation algorithms, and validation rules remain untouched.
2. **Unify the Application Shell:** Standardize Topbar, Sidebar, Breadcrumb, and eliminate obsolete or marketing-style components.
3. **Establish a Dedicated Component System:** Modularize badges, steppers, matrix tables, confirmation dialogs, and toast systems.
4. **Elevate AHP & TOPSIS into True Analysis Workspaces:** Convert raw data tables into rich, interactive decision workspaces with visual validation, consistency diagnostics, and clear managerial sign-offs.
