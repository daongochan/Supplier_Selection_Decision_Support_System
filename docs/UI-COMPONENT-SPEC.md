# SupplierSelectionDSS - UI Component Catalog & Specification

**Status:** Technical Specification  
**Component Prefix:** `dss-` / Standardized class system  
**Target:** Shared Razor Partials, Modular CSS, and Reusable DOM Patterns

---

## 1. Buttons (`components/buttons.css`)

### 1.1 Variants & Hierarchy
- **Primary CTA (`.btn-primary`):**
  - *Styling:* Solid terracotta (`var(--terracotta)`), white text, pill radius (`var(--radius-pill)`), font-weight 600, height 42px.
  - *Usage:* Single most prominent action per section (`Add supplier`, `Save scores`, `Calculate AHP`, `Approve decision`).
- **Secondary / Outline (`.btn-outline`):**
  - *Styling:* Transparent background, border `1px solid var(--border-strong)`, color `var(--ink)`, pill radius, height 42px.
  - *Usage:* Supportive actions (`Reset filters`, `Cancel`, `Back to Batch`, `Export Excel`).
- **Danger Action (`.btn-danger`):**
  - *Styling:* Solid deep crimson (`var(--danger-dark)`), white text.
  - *Usage:* Irreversible actions (`Deactivate`, `Cancel Batch`).
- **Icon Button (`.btn-icon`):**
  - *Styling:* Square `34px × 34px`, centered icon, transparent background with soft hover tint.
  - *Usage:* Row-level actions, close buttons, table tools.

### 1.2 Interactive & Loading States
- **Hover:** Subtle 1px translation upward (`translateY(-1px)`), slight shadow boost.
- **Active:** Depresses to original position (`translateY(0)`).
- **Disabled:** 45% opacity, `cursor: not-allowed`, no box-shadow, pointer-events suppressed.
- **Loading State (`.btn-loading`):**
  - Button text shifts opacity slightly.
  - Automatically prepends an inline SVG / CSS spinner (`.spinner-border-sm`).
  - Button is disabled against double-clicks via `site.js`.

---

## 2. Form Controls (`components/forms.css`)

### 2.1 Standard Inputs (`.form-control`)
- **Height & Padding:** 44px uniform height (`padding: 10px 14px`), box-sizing `border-box`.
- **Borders & Radii:** 1px solid `var(--border-strong)`, `border-radius: var(--radius-sm)` (8px).
- **Focus State:** 2px outline / 3px box-shadow ring in `rgba(183, 93, 69, 0.15)` with `border-color: var(--terracotta)`.
- **Validation State (`.input-validation-error`):**
  - Border turns crimson `var(--danger-dark)`.
  - Focus ring glows in `rgba(176, 42, 55, 0.15)`.
  - Accompanied by `<span class="field-validation-error text-danger">`.

### 2.2 Select & Dropdowns (`select.form-control`)
- Native styled select with custom SVG chevron indicator (`background-image`).
- No default browser border artifacts.
- Options have consistent padding and dark neutral ink colors.

### 2.3 Form Grouping Layout (`.form-grid`)
- 2-column or 3-column CSS Grid with 16px row and column gaps.
- Full-width fields (`Address`, `Notes`, `PaymentTerms`) span all columns using `.form-group-full`.
- Semantic field labels in small bold uppercase font (`var(--muted)`, 12px, letter-spacing 0.7px).

---

## 3. Search & Filter Card (`Views/Shared/_SearchFilter.cshtml`)

### 3.1 Architecture & Separation
- Dedicated container `.filter-card`.
- **Strict Rule:** Contains *only* query controls (Keyword, Status, Custom Business Filters) and action buttons (Search, Reset).
- **Zero Pagination Controls:** Rows-per-page selectors and page buttons are strictly forbidden in this container.

### 3.2 Desktop & Responsive Layout
```css
.supplier-filter {
    display: grid;
    grid-template-columns: minmax(0, 2fr) minmax(180px, 1fr) auto;
    align-items: end;
    gap: 16px;
    width: 100%;
}
```
- On screens $\le 900px$: Collapses to 2 columns with action buttons spanning across row 2.
- On screens $\le 700px$: Stacks vertically (1 column) for thumb-friendly mobile input.

---

## 4. Data Tables (`components/tables.css`)

### 4.1 Structure & Styling
- Wrapped in `.data-table-wrapper` inside `.table-card`.
- **Header (`<thead>`):** Soft surface background (`var(--surface-muted)`), 12px uppercase labels, border bottom `1px solid var(--border)`.
- **Rows (`<tbody> tr`):**
  - Subtle bottom border `1px solid var(--border)`.
  - Row hover background tint `var(--canvas-soft)` with 0.15s transition.
  - Generous vertical cell padding (12px top/bottom) for comfortable scanability.
- **Numerical Alignment:** Monospace or tabular figures right-aligned; supplier codes, names, and statuses left-aligned; actions right-aligned.

### 4.2 Action Link Grouping (`.table-actions`)
- Compact flex container (`display: flex; align-items: center; justify-content: flex-end; gap: 8px;`).
- Actions formatted as pill buttons or discrete text links with hover underlines and minimum 8px touch clearance.

---

## 5. Pagination Component (`Views/Shared/_Pagination.cshtml`)

### 5.1 Structure & Layout
Fixed at the base of `.table-card` separated by a 1px top border.
- **Left (Summary Text):**
  ```html
  <div class="pagination-summary">
      Hiển thị <strong>1</strong> – <strong>10</strong> trên tổng số <strong>85</strong> kết quả
  </div>
  ```
- **Right (Navigation Buttons):**
  ```html
  <nav class="pagination" aria-label="Pagination">
      <a class="page-link" aria-label="Previous">‹</a>
      <span class="page-link page-link-active">1</span>
      <a class="page-link">2</a>
      <a class="page-link">3</a>
      <span class="page-ellipsis">…</span>
      <a class="page-link">9</a>
      <a class="page-link" aria-label="Next">›</a>
  </nav>
  ```
- **Active Page Link:** Terracotta solid fill (`var(--terracotta)`), white text.
- **Disabled State:** Opacity 0.4, cursor not-allowed.

---

## 6. Status Pills & Badges (`components/status.css`)

Uniform rounded pill tokens with high-contrast color pairings:

| Status Name | Background Variable | Text Color Variable | Icon Indicator |
| :--- | :--- | :--- | :--- |
| **Active / Approved / Consistent** | `var(--sage-soft)` | `var(--sage-dark)` | `bi-check-circle-fill` |
| **Inactive / Cancelled / Inconsistent**| `var(--danger-soft)` | `var(--danger-dark)` | `bi-x-circle-fill` |
| **Draft / Pending Approval** | `var(--amber-soft)` | `var(--amber-dark)` | `bi-hourglass-split` |
| **Evaluating / In Progress** | `var(--sky-soft)` | `var(--sky-dark)` | `bi-arrow-repeat` |
| **Benefit Criterion** | `var(--lavender-soft)`| `var(--lavender-dark)`| `bi-graph-up-arrow` |
| **Cost Criterion** | `var(--sky-soft)` | `var(--sky-dark)` | `bi-currency-dollar` |

---

## 7. Universal Decision Stepper (`Views/Shared/_WorkflowStepper.cshtml`)

A standardized 5-step workflow component rendered across all batch execution views:

```html
<div class="dss-stepper">
    <div class="dss-step complete">
        <span class="dss-step-badge"><i class="bi bi-check"></i></span>
        <div class="dss-step-label">
            <strong>01 Thiết lập</strong>
            <small>Nhà cung cấp & Tiêu chí</small>
        </div>
    </div>
    <div class="dss-step-divider complete"></div>
    <div class="dss-step active">
        <span class="dss-step-badge">02</span>
        <div class="dss-step-label">
            <strong>02 Chấm điểm</strong>
            <small>Ma trận thực nghiệm</small>
        </div>
    </div>
    <div class="dss-step-divider"></div>
    <div class="dss-step">
        <span class="dss-step-badge">03</span>
        <div class="dss-step-label">
            <strong>03 Trọng số AHP</strong>
            <small>So sánh cặp & CR</small>
        </div>
    </div>
    <div class="dss-step-divider"></div>
    <div class="dss-step">
        <span class="dss-step-badge">04</span>
        <div class="dss-step-label">
            <strong>04 Xếp hạng TOPSIS</strong>
            <small>Khoảng cách PIS/NIS</small>
        </div>
    </div>
    <div class="dss-step-divider"></div>
    <div class="dss-step">
        <span class="dss-step-badge">05</span>
        <div class="dss-step-label">
            <strong>05 Phê duyệt</strong>
            <small>Quyết định & Ký số</small>
        </div>
    </div>
</div>
```

---

## 8. Toast Notifications (`Views/Shared/_Toast.cshtml`)

- **Fixed Placement:** Bottom-right viewport overlay (`z-index: 1050; position: fixed; bottom: 24px; right: 24px;`).
- **Variants:** `Success`, `Danger`, `Warning`, `Info`.
- **Auto-Dismiss:** Closes automatically after 5000ms with a horizontal slide-out animation.
- **Manual Dismiss:** Explicit `X` close button on each toast.
- **Central Authority:** Replaces scattered inline alert blocks across views.

---

## 9. Confirmation Modal (`Views/Shared/_ConfirmationModal.cshtml`)

- **Role:** Protects critical state transitions (`CancelBatch`, `ApproveDecision`, `ReevaluateBatch`, `DeactivateSupplier`).
- **Layout & Structure:**
  - Semantic Header: Warning/Question Icon + Descriptive Title.
  - Impact Statement: Clearly warns of cascade effects (e.g., *"Thao tác này sẽ hủy bỏ đợt đánh giá và khóa toàn bộ điểm số."*).
  - Optional Input Group: Notes or justification input (e.g., re-evaluation reason).
  - Dual Action Buttons: Cancel (Outline) + Confirm (Terracotta or Crimson Danger).
- **Keyboard Handling:** Escape closes the modal; Enter triggers confirm if valid.

---

## 10. Empty & Error States (`Views/Shared/_EmptyState.cshtml`)

- **Empty State (`.empty-state`):**
  - Centered neutral icon (`bi bi-inbox`, `bi bi-search`, etc., 48px).
  - Clear heading explaining the empty condition (*"Chưa có nhà cung cấp nào"*).
  - Guidance copy (*"Hãy thêm nhà cung cấp đầu tiên để bắt đầu quá trình đánh giá."*).
  - Primary call-to-action button (*"+ Thêm nhà cung cấp"*).
- **Error State (`.error-state`):**
  - Error badge icon (`bi bi-exclamation-triangle-fill`).
  - Clear non-technical explanation without exposing raw stack traces.
  - Retry button (`Thử lại`) and Return link (`Quay lại danh sách`).

---

## 11. Matrix Input Controls (`Evaluation` & `AHP`)

- **Evaluation Score Matrix:**
  - Sticky supplier column (`left: 0; position: sticky; z-index: 2`).
  - Number inputs formatted with minimum step controls, tabular numbers, and dynamic row-completion counters.
  - Highlighted cell on active focus.
- **AHP Pairwise Comparison Matrix:**
  - Diagonal cells ($a_{ii}$) locked and styled in disabled slate tint (`1.00`).
  - Reciprocal balance indicator showing whether Criterion $A$ is favored over Criterion $B$ or vice versa.
  - Live consistency feedback bar updating $CR$ without full page reloads where feasible.
