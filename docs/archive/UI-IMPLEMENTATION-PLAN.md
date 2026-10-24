<!-- ARCHIVE_HEADER -->
> [!NOTE]
> **ARCHIVED DOCUMENT — SUPERSEDED**
> **Archived Date:** 2026-10-24  
> **Base Commit:** \d5b98db2b8b732bf0ce5491793aa73ba9136b678\  
> **Status:** Historical point-in-time document preserved for traceability. The active canonical documentation is located in \docs/\ (\DESIGN-SYSTEM.md\, \ARCHITECTURE.md\, \ALGORITHMS.md\, \WORKFLOW-AND-PERMISSIONS.md\, \docs/qa/\).

---

# SupplierSelectionDSS - UI/UX Implementation Plan

**Execution Strategy:** 16 Structured Phases with Incremental Regression Testing  
**Primary Goal:** Transform SupplierSelectionDSS into a world-class Enterprise Decision Support System  
**Technical Boundary:** ASP.NET Core MVC (.NET 10), EF Core 10, SQL Server, Razor Views. Zero database schema alterations.

---

## Roadmap Overview

```
Phase 1: Global Tokens & Base CSS
  │
Phase 2: Application Shell (Sidebar, Topbar, Responsive Layout)
  │
Phase 3: Standard Breadcrumbs & Page Headers
  │
Phase 4: Unified Search/Filter, Data Tables & Pagination
  │
Phase 5: Refactored Master Data CRUD Pages (Suppliers, Groups, Criteria, Users)
  │
Phase 6: Evaluation Batch Management & Creation Wizard
  │
Phase 7: Evaluation Score Matrix Workspace
  │
Phase 8: AHP Analysis Workspace (Pairwise Matrix & Consistency Diagnostics)
  │
Phase 9: TOPSIS Analysis Workspace (Ideal Solutions & Closeness Coefficients)
  │
Phase 10: Multi-Attribute Comparison & Sensitivity Workspace
  │
Phase 11: Formal Business Decision & Approval Workspace
  │
Phase 12: Decision-Centric Executive Dashboard
  │
Phase 13: Standardized Report Generation & Audit Workspaces
  │
Phase 14: Accessibility (a11y) & Cross-Device Responsive Hardening
  │
Phase 15: Code Optimization, CSS Consolidation & Dead Code Cleanup
  │
Phase 16: Comprehensive End-to-End QA & Build Verification
```

---

## Detailed Phase Breakdown

### Phase 1: Global Design Tokens & Foundation Refinement
- **Objective:** Establish clean, standardized CSS custom properties in `tokens.css` and typography in `base/typography.css`.
- **Key Tasks:**
  - Define semantic variables: `--canvas`, `--surface`, `--ink`, `--body`, `--muted`, `--terracotta`, `--sage`, `--lavender`, `--sky`, `--amber`, `--danger`.
  - Configure dual typography: `Source Serif 4` for display/headings, modern sans-serif tabular fonts for numbers, tables, and form inputs.
  - Establish spacing tokens (4px–32px), standard border radii (`8px`, `14px`, `9999px`), and subtle enterprise shadows.
- **Verification:** Compile stylesheet, verify zero syntax errors.

### Phase 2: Application Shell (Sidebar & Topbar)
- **Objective:** Eliminate layout bugs, align sidebar items cleanly, and refine header identity.
- **Key Tasks:**
  - Update `Views/Shared/_Sidebar.cshtml`: Group navigation into logical domains (Workspace, Decision Operations, Master Data, Governance). Standardize 18px Bootstrap Icons with 12px label spacing.
  - Update `Views/Shared/_Topbar.cshtml`: Replace raw unicode `☰` with `<i class="bi bi-list"></i>`, enhance user avatar chip with role badge, and refine Sign Out action.
  - Remove unused or obsolete footer references per design specification.
- **Verification:** Test desktop view and mobile sidebar drawer toggle.

### Phase 3: Breadcrumb Trail & Page Header Standardization
- **Objective:** Provide consistent spatial awareness across every screen.
- **Key Tasks:**
  - Refine `Views/Shared/_Breadcrumb.cshtml` with standard `⌂ Trang chủ › [Module] › [Action]` hierarchy.
  - Standardize the Page Header structure across all views (`.page-eyebrow`, `h1.page-title`, `.page-description`, `.page-header-actions`).
- **Verification:** Navigate across different controllers; confirm breadcrumb trail accurately reflects route hierarchy without numbering.

### Phase 4: Search/Filter, Data Tables & Pagination Component
- **Objective:** Complete the strict separation between Search/Filter and Pagination.
- **Key Tasks:**
  - Standardize `.filter-card` across all listing views (`Supplier`, `ProductGroup`, `Criteria`, `EvaluationBatch`, `User`, `AuditLog`).
  - Ensure zero `Rows` selectors exist inside search cards.
  - Standardize `_Pagination.cshtml` across all views with `Hiển thị X – Y trên tổng số Z kết quả` on left, and pill-shaped page links on right.
  - Apply clean row hover, generous padding, and proper numerical alignment in `.data-table`.
- **Verification:** Perform keyword filtering, status filtering, and pagination clicks. Ensure route parameters (`keyword`, `status`, `page`, `pageSize`) persist seamlessly.

### Phase 5: Master Data CRUD Pages Refactoring
- **Objective:** Upgrade Supplier, Product Group, and Criteria forms to enterprise standards.
- **Key Tasks:**
  - Update `Create.cshtml` and `Edit.cshtml` for `Supplier`, `ProductGroup`, `Criteria`, and `User`.
  - Group inputs into logical semantic sections (e.g., General Information, Contact & Billing, DSS Properties).
  - Add clear required indicators, helper text, and accessible validation spans.
  - Standardize Save/Cancel button pairs with double-submit loading prevention.
- **Verification:** Submit valid and invalid forms; confirm inline validation errors and toast feedback.

### Phase 6: Evaluation Batch Management & Creation
- **Objective:** Elevate `EvaluationBatch/Index` and `EvaluationBatch/Create` to an intuitive sourcing wizard.
- **Key Tasks:**
  - In `EvaluationBatch/Create`: Implement clean supplier multi-selection checklist and criteria picker with live count indicators.
  - In `EvaluationBatch/Index`: Display batch status chips (`Draft`, `Evaluating`, `Completed`, `Cancelled`) with semantic colors and operational action menus.
- **Verification:** Create a new batch with $\ge 2$ suppliers and $\ge 2$ criteria; verify state transitions.

### Phase 7: Evaluation Score Matrix Workspace
- **Objective:** Transform `Evaluation/Index` into a dense, readable performance evaluation matrix.
- **Key Tasks:**
  - Implement sticky first column for supplier names and codes.
  - Add sticky column headers for criteria with Benefit/Cost indicator pills.
  - Format numeric inputs with clear range validations.
  - Add real-time matrix completion tracker (e.g., "16 of 20 scores entered").
- **Verification:** Enter performance scores across all cells; save and verify database persistence.

### Phase 8: AHP Analysis Workspace
- **Objective:** Turn `AHP/Index` into an authoritative Pairwise Comparison & Consistency Diagnostics workspace.
- **Key Tasks:**
  - Implement dynamic reciprocal display for pairwise comparison scale (Saaty 1–9).
  - Add prominent **AHP Consistency Diagnostic Banner**:
    - Display $\lambda_{max}$, $CI$, $RI$, $CR$.
    - Clear semantic verdict: Green *"✓ Nhất quán ($CR \le 0.10$)"* or Red *"⚠ Không nhất quán ($CR > 0.10$)"*.
  - Visualize calculated criteria weights ($w_j$) with horizontal percentage bars.
  - Add warning modal: recalculating AHP will invalidate downstream TOPSIS results.
- **Verification:** Execute AHP calculation with consistent and inconsistent matrices; verify state updates.

### Phase 9: TOPSIS Analysis Workspace
- **Objective:** Turn `TOPSIS/Index` into a multi-attribute ideal solution ranking workspace.
- **Key Tasks:**
  - Remove marketing-style trailing periods from headings.
  - Present step-by-step diagnostic tabs:
    1. Weighted Normalized Matrix ($V$)
    2. Ideal Solutions ($PIS$ and $NIS$)
    3. Distance Measures ($S_i^+, S_i^-$)
    4. Final Closeness Coefficients ($C_i$) and Ranking
  - Add visual progress bar for $C_i \in [0, 1]$ on each supplier row.
  - Add prominent Rank #1 accent badge.
  - Add direct primary action: `[ Proceed to Decision Approval ]`.
- **Verification:** Run TOPSIS calculation; confirm rankings and distance metrics align with mathematical formulas.

### Phase 10: Multi-Attribute Comparison & Sensitivity Workspace
- **Objective:** Provide managerial insight into *why* the top supplier won.
- **Key Tasks:**
  - Create a side-by-side comparison view between top contenders.
  - Highlight criteria where Rank #1 excelled vs. where alternatives were competitive.
  - Avoid proprietary score formulas; strictly present TOPSIS attribute contributions.
- **Verification:** Review comparison view with batches having varying numbers of criteria.

### Phase 11: Business Decision & Approval Workspace
- **Objective:** Elevate `DecisionResult/Index` into an accountable managerial sign-off terminal.
- **Key Tasks:**
  - Distinguish Analytical Recommendation (TOPSIS Rank #1) from Business Authority.
  - Build executive approval form with:
    - Selected supplier confirmation.
    - Decision justification / notes textarea.
    - `[ Approve & Finalize Batch ]` (Success button).
    - `[ Request Re-evaluation ]` (Warning action with mandatory reason input).
  - Record approval audit trail (approver, date, notes).
- **Verification:** Approve batch as Manager/Admin; verify transition to `Completed` state and locking of earlier steps.

### Phase 12: Decision-Centric Executive Dashboard
- **Objective:** Transform `Dashboard/Index` from card collection into an executive decision terminal.
- **Key Tasks:**
  - Remove marketing banner (`.dashboard-feature` orb and promotional tagline).
  - Implement interactive KPI cards linking directly to filtered views.
  - Add **Active Decisions Pipeline**: highlights batches currently in `Evaluating` state needing AHP or approval.
  - Display **Recent TOPSIS Winners** with closeness coefficient bars.
  - Add high-level Criteria Weight distribution visualization.
- **Verification:** Inspect dashboard across various roles (Admin, Purchasing, Expert, Manager).

### Phase 13: Standardized Report Generation & Audit Workspaces
- **Objective:** Polish `Report/Index` and `AuditLog/Index`.
- **Key Tasks:**
  - In `Report/Index`: Display report metadata card, batch summary, and distinct buttons for PDF download and Excel export with format icons.
  - In `AuditLog/Index`: Format audit trail table with formatted action badges, user details, and JSON payload viewer modal.
- **Verification:** Test PDF and Excel export generation for completed batches.

### Phase 14: Accessibility (a11y) & Cross-Device Responsiveness
- **Objective:** Ensure WCAG 2.1 AA compliance and flawless responsive performance.
- **Key Tasks:**
  - Check color contrast ratios across all text and status badges.
  - Verify full keyboard navigability (Tab, Enter, Escape, Arrow keys).
  - Add `aria-label`, `aria-expanded`, `aria-hidden` attributes across interactive widgets.
  - Verify breakpoints: Desktop (> 1024px), Tablet (701px–1024px), Mobile ($\le 700px$).
- **Verification:** Run responsive browser testing and keyboard navigation check.

### Phase 15: Performance, CSS Consolidation & Cleanup
- **Objective:** Eliminate redundant CSS, inline styles, and dead code.
- **Key Tasks:**
  - Consolidate repetitive utility classes into `components/shared.css`.
  - Remove conflicting `!important` declarations.
  - Ensure zero console errors or missing asset 404s.
- **Verification:** Inspect network and CSS bundle size.

### Phase 16: End-to-End Regression Testing & Final QA
- **Objective:** Validate the entire platform against all business invariants.
- **Key Tasks:**
  - Execute full end-to-end user journeys:
    1. Admin creates Supplier, Product Group, Criteria, and Batch.
    2. Purchasing Staff inputs Evaluation Scores.
    3. Expert performs AHP Pairwise Comparisons and validates Consistency.
    4. Expert runs TOPSIS calculation and inspects rankings.
    5. Manager inspects Comparison and executes formal Approval.
    6. System exports PDF and Excel reports.
  - Run all automated unit tests (`dotnet test`).
- **Verification:** Build output is 0 errors, 0 warnings; all test suites pass.
