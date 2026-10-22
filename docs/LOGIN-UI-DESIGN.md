# LOGIN UI DESIGN — SupplierSelectionDSS

## 1. Layout Overview

**Pattern:** Modern SaaS Split-Screen Login  
**Container:** Centered card, max-width 920px, min-height 540px  
**Split Ratio:** Left 45% | Right 55%

```
PAGE BACKGROUND (#F5F6FA)
┌────────────────────────────────────────────────────────┐
│   ┌───────────────────────────────────────────────┐    │
│   │  LEFT VISUAL (45%)  │  RIGHT FORM (55%)      │    │
│   │  Blue/Indigo grad.  │  White, clean form     │    │
│   │  Abstract fluid     │  Đăng nhập heading     │    │
│   │  Branding           │  Username input        │    │
│   │  AHP • TOPSIS       │  Password + show/hide  │    │
│   │                     │  Remember me           │    │
│   │                     │  Login button          │    │
│   └───────────────────────────────────────────────┘    │
└────────────────────────────────────────────────────────┘
```

---

## 2. Page Background

- Color: `#F5F6FA` (very light cool neutral gray)
- No pattern, no dots, no texture
- Sole purpose: make login card "float"

---

## 3. Login Card

| Property      | Value                                             |
|---------------|---------------------------------------------------|
| Background    | `#ffffff`                                         |
| Border-radius | `24px`                                            |
| Overflow      | `hidden`                                          |
| Shadow        | `0 20px 60px rgba(15, 23, 42, 0.10), 0 4px 12px rgba(15, 23, 42, 0.05)` |
| Border        | `none` (shadow handles depth)                     |
| Max-width     | `920px`                                           |
| Width         | `90vw`                                            |
| Min-height    | `540px`                                           |

---

## 4. Left Visual Panel

### Colors
- Gradient base: `linear-gradient(145deg, #1e3a8a 0%, #312e81 40%, #4338ca 70%, #6366f1 100%)`
- Highlight blob 1: `rgba(99, 102, 241, 0.35)` top-right
- Highlight blob 2: `rgba(34, 211, 238, 0.15)` bottom-left (cyan accent)
- Abstract shapes: large blurred organic gradients using pseudo-elements

### Content Composition
```
Top: Brand name "SUPPLIER SELECTION" / "DSS"
Middle: Headline + description (short)
Bottom-right: "AHP  ·  TOPSIS"
Background: fluid abstract gradient blobs
```

### Typography (on dark bg)
- Brand: `13px`, `700`, `letter-spacing: 1.5px`, `uppercase`, white
- Headline: `26px`, `600`, white, line-height 1.3
- Desc: `13px`, white, opacity 0.75
- Tags: `11px`, `600`, white, opacity 0.5

---

## 5. Right Form Panel

### Layout
- White background
- Padding: `48px 44px`
- Form max-width: `380px`, centered inside panel
- Vertical centering via flex

### Header
- Title "Đăng nhập": `28px`, `600`, `--ink`
- Subtitle: `14px`, `--muted`, line-height 1.5

### Form Fields

#### Username
- Label: "Tên đăng nhập", `13px`, `600`, `--ink`
- Input: height `44px`, border `1px solid #e2e8f0`, radius `10px`, focus ring blue
- Autocomplete: `username`
- Icon: user icon left (inside input)

#### Password
- Label: "Mật khẩu", same as above
- Input: height `44px`, same styling, `type=password`
- Show/hide toggle: eye icon right inside input (accessible button)
- Autocomplete: `current-password`

#### Remember Me
- Supported (backend sets `IsPersistent = model.RememberMe`)
- Small checkbox + label "Ghi nhớ đăng nhập"
- `13px`, `--muted`

#### Login Button
- Full width
- Height `46px`
- Background: `#1d4ed8`, hover `#1e40af`
- Border-radius: `10px`
- Font: `14px`, `600`, white
- Loading: spinner + "Đang đăng nhập..." with `disabled`

---

## 6. Colors Summary

| Token         | Value     | Usage                        |
|---------------|-----------|------------------------------|
| Page BG       | `#F5F6FA` | outer background             |
| Card BG       | `#ffffff` | login card                   |
| Panel Blue 1  | `#1e3a8a` | left panel gradient start    |
| Panel Blue 2  | `#4338ca` | left panel gradient mid      |
| Panel Indigo  | `#6366f1` | left panel gradient end      |
| Cyan accent   | `rgba(34, 211, 238, 0.18)` | blob highlight  |
| Button        | `#1d4ed8` | primary CTA                  |
| Input border  | `#e2e8f0` | default                      |
| Focus ring    | `#3b82f6` | focus state                  |

---

## 7. Typography Scale

| Element          | Size  | Weight | Color    |
|------------------|-------|--------|----------|
| Brand (left)     | 13px  | 700    | white    |
| Headline (left)  | 24px  | 600    | white    |
| Form title       | 28px  | 600    | `--ink`  |
| Subtitle         | 14px  | 400    | `--muted`|
| Label            | 13px  | 600    | `--ink`  |
| Input            | 14px  | 400    | `--ink`  |
| Helper/Error     | 12px  | 400    | red      |

---

## 8. Spacing System

- Panel padding: `48px 44px`
- Field gap: `20px` between form groups
- Heading → subtitle: `8px`
- Subtitle → first field: `28px`
- Remember → button: `20px`

---

## 9. Shadow

```css
box-shadow:
  0 20px 60px rgba(15, 23, 42, 0.10),
  0 4px 12px rgba(15, 23, 42, 0.05);
```

---

## 10. Responsive

| Breakpoint | Behavior                                              |
|------------|-------------------------------------------------------|
| > 768px    | 2-column split layout                                 |
| ≤ 768px    | Single column, left panel becomes compact top header  |
| ≤ 520px    | Full-width card, reduced padding                      |

---

## 11. Accessibility

- Semantic `<form>` with proper `<label for="...">` associations
- `autocomplete` attributes on inputs
- Visible focus ring on all interactive elements
- Show/hide password: `aria-label`, `aria-pressed`
- Error messages linked via `aria-describedby`
- Sufficient color contrast (≥4.5:1)

---

## 12. States

| State     | Description                                              |
|-----------|----------------------------------------------------------|
| Default   | Normal form, empty fields                                |
| Focus     | Blue ring on focused input                               |
| Error     | Red border on invalid field + inline message             |
| Loading   | Button disabled + spinner + "Đang đăng nhập..."          |
| Auth fail | Alert at top of form: "Tên đăng nhập hoặc mật khẩu không chính xác." |

---

## 13. Files to Modify

| File                                              | Action  |
|---------------------------------------------------|---------|
| `Views/Account/Login.cshtml`                      | Rewrite |
| `wwwroot/css/components/forms.css` (login section)| Rewrite |
| `Views/Account/AccessDenied.cshtml`               | No change |
| `Controllers/AccountController.cs`                | No change |
| `Models/LoginViewModel.cs`                        | No change |

---

## 14. Abstract Visual Technique

Use CSS pseudo-elements for fluid background blobs:
- `.login-visual::before`: large ellipse, top-right, indigo, blurred
- `.login-visual::after`: large ellipse, bottom-left, cyan, blurred
- Additional `.login-blob` elements for layered depth
- `filter: blur(50-80px)`, low opacity
- NO dots, NO grid, NO halftone
