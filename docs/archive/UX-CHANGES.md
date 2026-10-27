<!-- ARCHIVE_HEADER -->
> [!NOTE]
> **ARCHIVED DOCUMENT — SUPERSEDED**
> **Archived Date:** 2026-10-24  
> **Base Commit:** \d5b98db2b8b732bf0ce5491793aa73ba9136b678\  
> **Status:** Historical point-in-time document preserved for traceability. The active canonical documentation is located in \docs/\ (\DESIGN-SYSTEM.md\, \ARCHITECTURE.md\, \ALGORITHMS.md\, \WORKFLOW-AND-PERMISSIONS.md\, \docs/qa/\).

---

# Supplier Selection DSS — UI/UX & Accessibility Improvements

**Repository:** `https://github.com/daongochan/Supplier_Selection_Decision_Support_System`  
**Branch:** `fix/system-audit-20261023`  
**Date:** 2026-10-23

---

## 1. Responsive Mobile Navigation & Topbar Integration (F07)

### Problem
In the baseline commit (`662a569d8c907458bbe61be4951be5785a07c6d8`), `Views/Shared/_Layout.cshtml` did not render `Views/Shared/_Topbar.cshtml`. Because `#sidebarToggle` was defined only inside `_Topbar.cshtml` and `.app-sidebar` is hidden off-canvas at viewports $\le 900\text{px}$, mobile and tablet users had no button to open the navigation drawer.

### Remediation
- Rendered `<partial name="_Topbar" />` inside `.app-main` in `Views/Shared/_Layout.cshtml`.
- Added `id="appSidebar"` to `<aside class="app-sidebar">` in `Views/Shared/_Sidebar.cshtml` and wired `aria-controls="appSidebar"` and `aria-expanded="false"` on `#sidebarToggle` in `Views/Shared/_Topbar.cshtml`.
- Styled `.app-topbar` and `.sidebar-toggle` in `wwwroot/css/site.css` so the sticky topbar appears cleanly on viewports $\le 900\text{px}$ while staying hidden on desktop viewports ($> 900\text{px}$).
- Updated `wwwroot/js/site.js` (`initSidebar`) to synchronize `aria-expanded` (`"true"` / `"false"`), lock background body scroll while open, close on backdrop click or `Escape`, and restore focus to `#sidebarToggle`.

---

## 2. Accessible Confirmation Modal & Native Form Validation (F11)

### Problem
`wwwroot/js/site.js` intercepted form submissions and button clicks with `[data-dss-confirm]`, opened `#dssConfirmModal`, and executed `form.submit()` upon confirmation. Calling `form.submit()` bypassed native HTML5 constraint validation (`required`, `min`, `max`) and `submit` event listeners. Furthermore, custom callbacks were not awaited, and keyboard `Tab` focus could escape `#dssConfirmModal`.

### Remediation
- **Pre-Modal HTML5 Validation:** Before opening `#dssConfirmModal`, `site.js` now checks `typeof form.checkValidity === "function" && !form.checkValidity()` and invokes `form.reportValidity()`, preventing invalid forms from opening the confirmation dialog.
- **Standards-Compliant Submission:** Replaced `form.submit()` with `form.requestSubmit(submitter)` (with a bypassed flag for the confirmed submission) so browser validation and `submit` listeners run properly.
- **Async Error Recovery:** `confirmBtn` handler is now `async` and `await`s `Promise` callbacks inside a `try/catch`. If an async action rejects, the button re-enables (`isConfirming = false`) so the user can retry or cancel instead of being trapped in a locked modal.
- **Keyboard Focus Trap:** Added a `keydown` listener on `#dssConfirmModal` that cycles `Tab` and `Shift+Tab` between focusable controls inside the dialog and closes on `Escape`.

---

## 3. TOPSIS Progressive Disclosure & Information Hierarchy (F12)

### Problem
`Views/TOPSIS/Index.cshtml` previously placed the Criteria Weights table and the raw Decision Matrix above the final TOPSIS Ranking table, followed by three more dense intermediate matrices ($R$, $V$, PIS/NIS) and Sensitivity Analysis, forcing managers and experts to scroll past five tables to see the actual supplier ranking.

### Remediation
- Reordered `Views/TOPSIS/Index.cshtml` so the **Workflow Readiness Banner**, **Primary Action Bar (`Calculate TOPSIS` / `Proceed to Decision & Approval`)**, **Final TOPSIS Ranking Table**, and **Executive Recommendation Summary** appear immediately at the top of the workspace.
- Placed detailed analytical tables inside collapsible `<details class="table-card topsis-matrix-card topsis-details-card">` panels:
  1. **Weight Sensitivity Analysis ($\pm 10\%, \pm 20\%$)** (with stability badge in `<summary>`)
  2. **Input Decision Matrix ($X$) & AHP Criteria Weights ($w_j$)**
  3. **Intermediate TOPSIS Matrices: Vector-Normalized ($R$), Weighted Normalized ($V$), and Ideal Solutions ($A^+, A^-$)**

---

## 4. Language Consistency & Shared AHP Matrix Disclosure (F09, F12)

- Updated `<html lang="en">` in `Views/Shared/_Layout.cshtml` and standardized navigation (`_Sidebar.cshtml`, `_Topbar.cshtml`, `_Breadcrumb.cshtml`, `_ConfirmationModal.cshtml`), authentication (`Views/Account/Login.cshtml`), batch details (`Views/EvaluationBatch/Details.cshtml`), AHP (`Views/AHP/Index.cshtml`), TOPSIS (`Views/TOPSIS/Index.cshtml`), and Decision Result (`Views/DecisionResult/Index.cshtml`) in clear, consistent English while keeping all persisted database enum constants (`Draft`, `Evaluating`, `Completed`, `Cancelled`, `Benefit`, `Cost`, `Active`, `Inactive`, `Locked`, `Approved`, `Rejected`, `PendingReevaluation`) untouched.
- Added an explicit **Shared Batch Matrix** notice banner in `Views/AHP/Index.cshtml` clarifying that the pairwise comparison matrix is shared at the batch level and protected by optimistic concurrency (`RowVersion`).
