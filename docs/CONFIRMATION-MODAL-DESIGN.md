# Confirmation Modal Design & Implementation Specification

> **Document Status**: Draft / Ready for Implementation  
> **System**: SupplierSelectionDSS  
> **Component**: Unified Confirmation Modal System (`_ConfirmationModal.cshtml`, `site.js`, `site.css`)  
> **Asset Reference**: `wwwroot/images/duck.jpg` (564 × 752 px, Royal Cobalt Blue `#024ba9`)

---

## 1. Executive Summary & Goals

The goal of this redesign is to replace disparate and legacy confirmation dialogs across SupplierSelectionDSS with a **single, unified, friendly, and enterprise-grade Confirmation Modal**.

### Key Objectives
1. **Unified User Experience**: All destructive, consequential, or confirmation actions (Activate, Deactivate, Delete, Cancel Batch, Approval, Re-evaluation) share one cohesive visual language.
2. **Distinctive Split-Screen Layout**:
   - **Left Panel (36–40%)**: Real visual asset `duck.jpg` (curious duck with "???"), providing immediate emotional resonance, human warmth, and intuitive pause-for-thought before proceeding.
   - **Right Panel (60–64%)**: Clean, structured, accessible confirmation content (icon, dynamic title, contextual message, optional input, and dual action buttons).
3. **Enterprise Safety & Semantics**: Proper semantic color-coding for primary vs. destructive actions, double-click/double-submit prevention with loading state, keyboard accessibility (Esc, Tab), and full integration with the existing toast feedback system.

---

## 2. Image Asset Specification

| Property | Value | Notes |
|---|---|---|
| **File Path** | `wwwroot/images/duck.jpg` | Official static asset |
| **Dimensions** | 564 × 752 px | 3:4 portrait aspect ratio |
| **Dominant Color** | `#024ba9` (RGB: 2, 75, 169) | Royal Cobalt Blue background |
| **Subject** | White duck looking up with "???" expression | Located bottom-right to center |
| **Strict Constraints** | **NO** filter, **NO** recolor, **NO** redraw, **NO** dot patterns, **NO** distortion | Original asset must remain 100% untouched |
| **Rendering** | `object-fit: cover; object-position: center bottom;` | Preserves duck facial expression and "???" question marks |

---

## 3. Modal Architecture & Layout

### 3.1 Visual Blueprint

```
┌────────────────────────────────────────────────────────────────────────┐
│                               MODAL OVERLAY                            │
│  rgba(15, 23, 42, 0.5) with backdrop-filter: blur(4px)                │
│                                                                        │
│       ┌─────────────────────────────────────────────────────────┐      │
│       │                   MODAL CONTAINER (600px)               │      │
│       │       border-radius: 20px, overflow: hidden             │      │
│       │  ┌───────────────────┬───────────────────────────────┐  │      │
│       │  │                   │ [Icon] Title              [X] │  │      │
│       │  │                   ├───────────────────────────────┤  │      │
│       │  │                   │                               │  │      │
│       │  │     duck.jpg      │ Description message           │  │      │
│       │  │  (Left Panel 38%) │                               │  │      │
│       │  │   bg: #024ba9     │ Consequence note (optional)   │  │      │
│       │  │                   │                               │  │      │
│       │  │                   ├───────────────────────────────┤  │      │
│       │  │                   │        [ Hủy bỏ ] [ Xác nhận ]│  │      │
│       │  └───────────────────┴───────────────────────────────┘  │      │
│       └─────────────────────────────────────────────────────────┘      │
└────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Dimensions & Proportions
- **Container Max-Width**: `600px` (Desktop).
- **Proportion**: Left panel `38%` (`~228px`), Right panel `62%` (`~372px`).
- **Container Radius**: `20px` with `overflow: hidden`.
- **Shadow**: `0 24px 60px -12px rgba(15, 23, 42, 0.28), 0 0 0 1px rgba(15, 23, 42, 0.06)`.
- **Backdrop Overlay**: `rgba(15, 23, 42, 0.52)` with `backdrop-filter: blur(4px)`.

---

## 4. Component Structure & Elements

### 4.1 Left Panel (Visual)
- `<div class="dss-modal-visual">`:
  - `background-color: #024ba9;`
  - `<img src="/images/duck.jpg" alt="" class="dss-modal-img" aria-hidden="true" />`
  - CSS: `object-fit: cover; object-position: center bottom; width: 100%; height: 100%;`

### 4.2 Right Panel (Confirmation Content)
- `<div class="dss-modal-content">`:
  - **Header**:
    - Title group: Small semantic Bootstrap icon + Dynamic Title (`<h3>`).
    - Dismiss button (`<button class="dss-modal-close" aria-label="Đóng"><i class="bi bi-x-lg"></i></button>`).
  - **Body**:
    - Contextual message `<p class="dss-modal-message">`.
    - Consequence / warning callout if applicable `<div class="dss-modal-consequence">`.
    - Input area (hidden by default, shown when `requireInput: true` for audit logs or reasons).
  - **Actions (Footer)**:
    - Secondary Button: `Hủy bỏ` (neutral outline).
    - Primary Action Button: Dynamic label (`Xác nhận`, `Kích hoạt`, `Ngừng hoạt động`, `Hủy đợt`, `Phê duyệt`), semantic color (`btn-primary`, `btn-danger`, `btn-warning`, `btn-success`).

---

## 5. Action Semantics & Styling

| Action Category | Target Actions | Icon | Button Class | Accent Color |
|---|---|---|---|---|
| **Normal / Informative** | Calculate AHP, Calculate TOPSIS, Generic Confirmation | `bi-question-circle-fill` | `btn-primary` | Enterprise Blue (`#1d4ed8`) |
| **Activation / Approval** | Activate Supplier, Product Group, Criterion; Approve Supplier | `bi-check-circle-fill` | `btn-success` or `btn-primary` | Emerald / Blue (`#16a34a` / `#1d4ed8`) |
| **Destructive / Danger** | Deactivate Supplier/Group/Criterion, Deactivate User, Cancel Batch, Reject | `bi-exclamation-triangle-fill` | `btn-danger` | Crimson / Rose (`#dc2626`) |
| **Warning / Reset** | Re-evaluate Batch (invalidates previous calculations) | `bi-arrow-counterclockwise` | `btn-warning` | Amber (`#d97706`) |

---

## 6. Interaction & Lifecycle States

### 6.1 Open Flow
1. User clicks an element with `[data-dss-confirm]` OR code calls `DSS.showConfirmModal(options)`.
2. Modal updates DOM elements (`title`, `message`, `icon`, `btn-text`, `btn-class`).
3. Modal displays with entrance animation (`fade + slight scale up`).
4. Focus is automatically trapped or shifted to the primary action button (or input if required).

### 6.2 Processing / Loading State
1. User clicks the confirm button.
2. Confirm button enters loading state:
   - Text updates to `Đang xử lý...` (or dynamic `Đang kích hoạt...`, `Đang hủy...`).
   - Spinner icon prepended (`<span class="spinner-border spinner-border-sm me-2"></span>`).
   - `disabled = true` set on both Confirm and Cancel buttons.
3. ESC key and backdrop click are temporarily **disabled** during processing to avoid race conditions and duplicate submissions.
4. Form is submitted or callback is triggered.

### 6.3 Dismiss Flow (Cancel / ESC / Click Outside)
- If NOT in processing state:
  - Close button [X], `Hủy bỏ`, ESC key, or clicking outside overlay closes the modal.
  - Focus returns to the trigger element that initiated the modal.

---

## 7. Responsive Design

### 7.1 Desktop (`> 640px`)
- Split layout: Horizontal side-by-side (`row`).
- Left panel width: `38%`, Right panel width: `62%`.
- Height adjusts naturally to content with minimum height `320px`.

### 7.2 Mobile (`<= 640px`)
- Stacked layout: Vertical (`column`).
- Top panel: `duck.jpg` with height `160px` and `object-position: center 30%`.
- Bottom panel: Confirmation content, full width, padded comfortably.
- Buttons stack or stretch to full width for easy touch interaction.

---

## 8. Audit of Existing Usages to Migrate

| File | Current Implementation | Migration Action |
|---|---|---|
| `Views/Shared/_ConfirmationModal.cshtml` | Old single-column modal box with inline `<style>` | Upgrade to split layout with `duck.jpg` left panel |
| `wwwroot/js/site.js` | `DSS.showConfirmModal`, `data-dss-confirm` click listener | Update to handle button loading, semantics, focus return |
| `Views/EvaluationBatch/Details.cshtml` (line 594) | `onclick="return confirm('Bắt đầu đợt đánh giá này?');"` | Migrate to `data-dss-confirm="Bạn có chắc chắn muốn bắt đầu tiến trình đánh giá cho đợt '@Model.BatchName'?"` |
| `Views/EvaluationBatch/Details.cshtml` (line 759) | `onclick="return confirm('Bạn có chắc chắn muốn hủy đợt đánh giá này không?...');"` | Migrate to `data-dss-confirm="Bạn có chắc chắn muốn hủy đợt đánh giá '@Model.BatchName'? Thao tác này không thể hoàn tác."` |
| `Views/Supplier/Index.cshtml` | `data-dss-confirm` | Already using declarative syntax, will automatically use the new modal |
| `Views/ProductGroup/Index.cshtml` | `data-dss-confirm` | Already using declarative syntax, will automatically use the new modal |
| `Views/Criteria/Index.cshtml` | `data-dss-confirm` | Already using declarative syntax, will automatically use the new modal |
| `Views/User/Index.cshtml` | `data-dss-confirm` | Already using declarative syntax, will automatically use the new modal |
| `Views/EvaluationBatch/Index.cshtml` | `data-dss-confirm` | Already using declarative syntax, will automatically use the new modal |
| `Views/AHP/Index.cshtml` | `data-dss-confirm` | Already using declarative syntax, will automatically use the new modal |
| `Views/TOPSIS/Index.cshtml` | `data-dss-confirm` | Already using declarative syntax, will automatically use the new modal |
| `Views/DecisionResult/Index.cshtml` | `data-dss-confirm` | Already using declarative syntax, will automatically use the new modal |

---

## 9. Accessibility (a11y) Conformance

- **ARIA Attributes**:
  - Container has `role="dialog"`, `aria-modal="true"`, `aria-labelledby="dssConfirmTitle"`, `aria-describedby="dssConfirmMessage"`.
- **Keyboard Navigation**:
  - `Escape` closes the modal (when not processing).
  - `Tab` navigates between interactive elements (Close [X], Cancel, Confirm).
- **Focus Management**:
  - Saves `activeElement` prior to opening and restores focus upon dismissal.
- **Image Semantics**:
  - Visual image has `aria-hidden="true"` and empty `alt=""` because it is purely emotional/decorative and conveys no textual business logic.
