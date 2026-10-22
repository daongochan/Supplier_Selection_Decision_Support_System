# Sidebar Background Design Specification

## 1. Design Concept: "Visual Anchor"
The sidebar will serve as the strong, foundational visual anchor for the SupplierSelectionDSS system. It will transition from a plain white background to a Deep Navy base, providing a "Modern Enterprise SaaS" feel.

**Keywords:** Modern Enterprise, Clean SaaS, Professional, Subtle Depth, Technology / Decision Support.

## 2. Color Palette & Typography
- **Base Background:** Deep Navy (`#0d1726`).
- **Text Primary:** Bright Slate/White (`#f8fafc`) for readability.
- **Text Muted:** Soft Slate (`#94a3b8`) for secondary elements (subtitles, section headers).
- **Hover State:** Subtle white overlay (`rgba(255, 255, 255, 0.06)`).
- **Active State:** Terracotta/Blue tint (`rgba(29, 78, 216, 0.20)`) with a strong left border accent (`#3b82f6` or `#1d4ed8`).

## 3. Decorative Elements: Subtle Geometric Depth
- **No Dots / Grids:** Strict adherence to the constraint of avoiding dot patterns or complex textures.
- **Abstract Shapes:** We will use pseudo-elements (`::before` and `::after`) on a background wrapper or the sidebar itself to create large, soft geometric polygons/waves.
- **Positioning:** The decorative shapes will primarily emerge from the bottom 35% of the sidebar (starting around 65% height and going down) so they do not interfere with the dense navigation items at the top.
- **Layering & Opacity:** The shapes will use very low opacity soft blue/cyan gradients (e.g., `rgba(59, 130, 246, 0.05)`) with `backdrop-filter: blur()` if necessary, ensuring they sit firmly in the background (z-index: 0) while content sits above (z-index: 2).

## 4. CSS Architecture & Changes
### `tokens.css`
The tokens for dark sidebar already exist. We will ensure they are utilized:
- `--sidebar-bg-dark: #0d1726;`
- `--sidebar-text-dark: #f8fafc;`
- `--sidebar-text-muted: #94a3b8;`
- Add gradient tokens for the geometric shapes.

### `layout.css`
- Update `.app-sidebar` to use `var(--sidebar-bg-dark)` and adjust text colors.
- Introduce an `.app-sidebar::before` and `.app-sidebar::after` for the geometric background elements. Example:
  - Shape 1 (Bottom Left): A soft, large skewed polygon with a subtle blue gradient.
  - Shape 2 (Bottom Right): A complementary shape to create overlapping depth.
- Ensure `.sidebar-nav`, `.sidebar-brand`, and `.sidebar-footer` have `position: relative; z-index: 2;` (which they currently do based on the audit).
- Update `.sidebar-link` colors to contrast against the dark background.
- Update `.sidebar-section-title` and brand text colors.

## 5. Implementation Steps
1. **Apply Base Colors:** Change the background and text colors of the sidebar elements.
2. **Implement Decorative Shapes:** Add the CSS for the subtle geometric depth.
3. **Refine States:** Adjust hover, active, and focus states for dark mode contrast.
4. **Test Responsiveness:** Ensure the shapes scale correctly on different screen heights.
