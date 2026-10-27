<!-- ARCHIVE_HEADER -->
> [!NOTE]
> **ARCHIVED DOCUMENT — SUPERSEDED**
> **Archived Date:** 2026-10-24  
> **Base Commit:** \d5b98db2b8b732bf0ce5491793aa73ba9136b678\  
> **Status:** Historical point-in-time document preserved for traceability. The active canonical documentation is located in \docs/\ (\DESIGN-SYSTEM.md\, \ARCHITECTURE.md\, \ALGORITHMS.md\, \WORKFLOW-AND-PERMISSIONS.md\, \docs/qa/\).

---

# SupplierSelectionDSS - UI/UX Design System Specification

**Version:** 2.0 Enterprise  
**Status:** Approved Architecture Draft  
**Scope:** Whole-system Design Tokens, Layout Framework, and Ergonomic Standards

---

## 1. Design Philosophy & Visual Language

`SupplierSelectionDSS` adopts the **Modern Enterprise Decision Support** visual style:
- **Calm, High-Information-Density, Data-Driven:** Clean backgrounds, refined neutral surfaces, high-contrast typography, and purposeful data visualization.
- **Human-in-the-Loop Transparency:** Every mathematical calculation (AHP eigenvector weighting, TOPSIS ideal solution distances) is presented with diagnostic clarity and clear operational status.
- **Zero Decorative Fluff:** No exaggerated floating gradient orbs, no marketing superlatives, no jarring animations, and no unnecessary cards. Every pixel serves situational awareness, analytical rigor, or decision accountability.

---

## 2. Design Tokens & Core System

### 2.1 Color Palette
The color system is organized into **Neutrals**, **Semantic Intent**, and **Analytical Accents**.

```css
:root {
    /* -------------------------------------------------------------
       CANVAS & NEUTRALS (Warm-Tinted Enterprise Warm Stone)
       ------------------------------------------------------------- */
    --canvas: #fcfbf9;            /* Page background - calm, glare-free */
    --canvas-soft: #f7f5f0;       /* Secondary background / hover */
    --surface: #ffffff;           /* Card & Table background */
    --surface-muted: #f4f1ea;     /* Inactive elements, table header */
    --border: #e6e2d8;            /* Hairline borders */
    --border-strong: #d4cebe;     /* Focus rings & active boundaries */

    /* -------------------------------------------------------------
       TEXT & CONTRAST (WCAG 2.1 AA Compliant)
       ------------------------------------------------------------- */
    --ink: #1a1917;               /* Headings, primary data values (15.8:1) */
    --body: #3c3935;              /* Body copy, table labels (9.4:1) */
    --muted: #747067;             /* Descriptions, metadata, breadcrumbs (4.8:1) */
    --disabled: #a8a49c;          /* Disabled controls, inactive icons */

    /* -------------------------------------------------------------
       BRAND & ACTION ACCENTS
       ------------------------------------------------------------- */
    --terracotta: #b75d45;        /* Primary CTA, active page indicator */
    --terracotta-hover: #9c4b35;  /* Hover state */
    --terracotta-soft: #fbeee9;   /* Subtle brand highlight */

    /* -------------------------------------------------------------
       SEMANTIC WORKFLOW & STATUS PALETTE
       ------------------------------------------------------------- */
    /* Success & Consistent (AHP CR <= 0.10, Completed, Active) */
    --sage-dark: #2d6a4f;
    --sage-soft: #e8f5ed;
    --sage-border: #b7e4c7;

    /* Danger & Inconsistent (AHP CR > 0.10, Cancelled, Inactive, Error) */
    --danger-dark: #b02a37;
    --danger-soft: #fdf0f1;
    --danger-border: #f8c9ce;

    /* Warning & Needs Review (Pending Approval, Draft) */
    --amber-dark: #92540c;
    --amber-soft: #fef7ec;
    --amber-border: #fbe4be;

    /* Analytical & Technical (AHP Weights, TOPSIS Scores, Benefit Criteria) */
    --lavender-dark: #5c4d7d;
    --lavender-soft: #f2f0f8;
    --lavender-border: #dcd6eb;

    /* Operational & Cost Criteria (Cost criteria, In-Progress Evaluation) */
    --sky-dark: #1e5a84;
    --sky-soft: #edf6fb;
    --sky-border: #bee0f2;
}
```

### 2.2 Typography Scale & Pairing Rules
To optimize both editorial authority and numerical density, typography is structured into two deliberate font families:
- **Primary Serif (`Source Serif 4`):** Used for Page Titles, Section Headings, and Primary KPI Totals to give an authoritative, executive feel.
- **Tabular Sans-Serif (`system-ui`, `-apple-system`, `Segoe UI`, `Roboto`, `Helvetica Neue`):** Used for Data Tables, Evaluation Matrices, Pairwise Scales, Decimal Scores ($CR, C_i$), and Form Inputs to ensure zero font-rendering distortion on small decimal values.

| Level | Size | Weight | Line Height | Font Family | Usage |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Eyebrow** | `11px` | 600 | 1.2 | Sans-Serif | All-caps tracking category header (`letter-spacing: 0.8px`) |
| **Page Title** | `26px` | 600 | 1.25 | Source Serif 4 | Main workspace page heading (`<h1>`) |
| **Section Title** | `18px` | 600 | 1.3 | Source Serif 4 | Major section heading (`<h2>`) |
| **Card Header** | `15px` | 600 | 1.35 | Sans-Serif | Sub-card or panel title |
| **Body Primary** | `14px` | 400 / 500 | 1.5 | Sans-Serif | Standard table text, modal body, descriptions |
| **Data / Numbers**| `13px` | 600 | 1.4 | Sans-Serif (Tabular) | Matrix scores, weights, closeness values |
| **Caption / Meta**| `12px` | 400 / 500 | 1.4 | Sans-Serif | Helper text, breadcrumbs, timestamps, pagination |

### 2.3 Spacing, Borders & Shadows
- **Base Grid:** 4px / 8px incremental scale (`--space-1: 4px`, `--space-2: 8px`, `--space-3: 12px`, `--space-4: 16px`, `--space-6: 24px`, `--space-8: 32px`).
- **Border Radius Hierarchy:**
  - Standard Inputs & Table Cells: `8px` (`--radius-sm`)
  - Panels, Modal Dialogs & Table Cards: `14px` (`--radius-md`)
  - Primary Action Buttons, Filter Pills & Status Badges: `9999px` (`--radius-pill`)
- **Elevation (Enterprise Subtle):**
  - `--shadow-card`: `0 2px 8px rgba(36, 33, 30, 0.04), 0 1px 2px rgba(36, 33, 30, 0.02)`
  - `--shadow-dropdown`: `0 8px 24px rgba(36, 33, 30, 0.08), 0 2px 6px rgba(36, 33, 30, 0.04)`
  - `--shadow-modal`: `0 16px 48px rgba(26, 25, 23, 0.16)`

---

## 3. Application Shell Framework

```
┌────────────────────────────────────────────────────────────────────────┐
│ TOPBAR: Logo/Identity | Context: Current Batch | User Role | Sign Out  │
├──────────────┬─────────────────────────────────────────────────────────┤
│              │ BREADCRUMB: ⌂ Trang chủ › Đợt đánh giá › Đợt #01        │
│   SIDEBAR    ├─────────────────────────────────────────────────────────┤
│              │ PAGE HEADER: Title, Description, Primary Action(s)      │
│  - Dashboard ├─────────────────────────────────────────────────────────┤
│  - Batches   │ DECISION WORKFLOW STEPPER (If in Batch Context)         │
│  - Suppliers ├─────────────────────────────────────────────────────────┤
│  - Criteria  │ SEARCH & FILTER PANEL (If Data View)                    │
│  - Products  ├─────────────────────────────────────────────────────────┤
│  - Users     │ MAIN WORKSPACE: Matrix / Table / Charts / Decision      │
│  - Audits    ├─────────────────────────────────────────────────────────┤
│              │ PAGINATION FOOTER (Only in Table Card)                  │
└──────────────┴─────────────────────────────────────────────────────────┘
```

### 3.1 Sidebar Navigation
- **Semantic Structure:** Grouped into clear role-based sections:
  1. *Workspace Overview:* Dashboard
  2. *Decision Operations:* Evaluation Batches (Hub for Evaluation, AHP, TOPSIS, Decision, Reports)
  3. *Master Data:* Suppliers, Product Groups, Criteria
  4. *Governance (Admin Only):* User Management, System Audit Logs
- **Visual Presentation:** Clean 240px wide panel on desktop with 12px horizontal icon-to-text spacing, centered 18px Bootstrap Icons, clear solid indicator on the active link (`var(--terracotta)`), and smooth background shift on hover.
- **Collapsible Behavior:** On screens $\le 900px$, the sidebar transitions off-canvas with a floating overlay and close-on-navigate trigger.

### 3.2 Topbar
- **Left Region:** Responsive hamburger toggle button (`<button id="sidebarToggle">` with `<i class="bi bi-list"></i>`) + Platform identity and context indicator ("SUPPLIER SELECTION DSS · Decision Workspace").
- **Right Region:** Current user profile chip (Avatar badge + Full name + Semantic role badge) + One-click Sign Out button (`.btn-logout`).

### 3.3 Breadcrumb Standard
- Renders as a semantic `<nav class="dss-breadcrumb" aria-label="Breadcrumb">`.
- Always displays `⌂ Trang chủ` with clickable home icon.
- Dividers are explicit `<span class="dss-breadcrumb-separator" aria-hidden="true"><i class="bi bi-chevron-right"></i></span>`.
- Terminal node represents the current page, marked with `aria-current="page"`, non-clickable, styled in bold neutral ink.

---

## 4. Standard Page Blueprint (The 7-Layer Layout)

Every data-management and decision view in `SupplierSelectionDSS` follows this strict 7-layer vertical hierarchy:

1. **Layer 1: Breadcrumb Trail** — Establishes hierarchical location in the information architecture.
2. **Layer 2: Page Header Block** — Eyebrow category tag + Clean `<h1>` title + Concise 1-sentence operational description.
3. **Layer 3: Primary Header Action(s)** — Contextual action buttons (e.g., `+ Add supplier`, `Calculate AHP`, `Approve Decision`).
4. **Layer 4: Contextual Stepper / Status Banner** — In batch-related pages, displays the 5-step Decision Progress Bar and consistency/readiness diagnostics.
5. **Layer 5: Search & Filter Card** — Specialized query panel containing keyword input, status dropdown, Search button, and Reset button. *Never contains row-count or pagination controls.*
6. **Layer 6: Primary Workspace Content** — Data Table, Comparison Matrix, or Analytical Diagnostics.
7. **Layer 7: Pagination & Results Summary** — Fixed strictly at the bottom of the table card:
   `Hiển thị 1 – 15 trên tổng số 85 kết quả` (left) + `‹ 1 2 3 ... 6 ›` (right).

---

## 5. Decision Workspace Paradigms

### 5.1 The 5-Phase Universal Batch Workflow
In any batch workspace (`EvaluationBatch/Details`, `Evaluation/Index`, `AHP/Index`, `TOPSIS/Index`, `DecisionResult/Index`), the user is guided by a unified, standardized Stepper Component:

```
[ 01 Setup ] ───► [ 02 Scoring ] ───► [ 03 AHP Weights ] ───► [ 04 TOPSIS ] ───► [ 05 Approval ]
```
- **State Visuals:**
  - `Completed`: Solid sage green with checkmark icon `✓`.
  - `Current / Active`: Terracotta solid pill with pulsing indicator.
  - `Pending / Upcoming`: Muted border with step number.
  - `Disabled / Locked`: Dimmed opacity with padlock icon when prerequisite rules fail.
- **Deep Navigation:** Clicking any completed step navigates directly to that workspace.

### 5.2 AHP Workspace Specification
- **Consistency Diagnostic Banner:** Prominently displays:
  - $\lambda_{max}$ (Principal Eigenvalue)
  - $CI$ (Consistency Index)
  - $RI$ (Random Index based on $n$)
  - $CR$ (Consistency Ratio)
  - **Verdict:** If $CR \le 0.10$, green banner: *"✓ Nhất quán (Consistent) - Đủ điều kiện chuyển sang TOPSIS"*. If $CR > 0.10$, warning red banner: *"⚠ Không nhất quán (Inconsistent) - Cần điều chỉnh ma trận so sánh cặp"*.
- **Reciprocal Comparison Matrix:** Clearly highlights the diagonal ($1.00$) and automatically mirrors reciprocal values ($a_{ji} = 1 / a_{ij}$) dynamically upon selection.
- **Weight Chart:** Horizontal bar visualization showing percentage share of each criterion ($w_j$).

### 5.3 TOPSIS Workspace Specification
- **Analytical Diagnostic Stages:** Displays the step-by-step mathematical transformations:
  1. *Normalized Decision Matrix ($R$)*
  2. *Weighted Normalized Matrix ($V$)*
  3. *Ideal Solutions ($PIS$ and $NIS$)*
  4. *Separation Distances ($S^+, S^-$) and Closeness Coefficients ($C_i$)*
- **Closeness Coefficient Progress Gauge:** Each supplier row features a proportional graphical bar showing $C_i \in [0, 1]$.
- **Rank Accents:** Rank #1 is highlighted with a gold badge and subtle surface tint to indicate analytical superiority.

### 5.4 Approval & Business Decision Specification
- Explicitly separates **Analytical Ranking** (TOPSIS Rank #1) from **Human Business Authority**.
- Features an **Executive Approval Card**:
  - Selected Supplier for Award (defaults to Rank #1, but allows justification if choosing an alternate).
  - Formal Sign-Off Note textarea.
  - Dual Decision Triggers:
    - `[ Approve & Finalize Batch ]` (Success button)
    - `[ Request Re-evaluation ]` (Warning button that resets batch state with audit logging).

---

## 6. Accessibility & Responsive Constraints

1. **Keyboard Traps & Hotkeys:**
   - Scoring matrix inputs support tab-through navigation.
   - Escape key always dismisses open modals.
2. **Screen Reader Landmarks:**
   - Topbar: `<header role="banner">`
   - Sidebar: `<aside role="navigation" aria-label="Sidebar">`
   - Content: `<main role="main">`
   - Tables: Semantic `<thead>`, `<tbody>`, `<th scope="col">`, `<th scope="row">`
3. **Responsive Breakpoints:**
   - **Desktop (> 1024px):** 240px static sidebar, multi-column search grid, complete data tables.
   - **Tablet (701px – 1024px):** Collapsible sidebar, 2-column filter grid, horizontally scrollable data tables inside card boundaries.
   - **Mobile ($\le 700px$):** Off-canvas drawer, stacked 1-column filters, card-adapted summaries, two-row pagination footer.
