# BACKGROUND DESIGN SYSTEM SPECIFICATION
## SupplierSelectionDSS — Modern Enterprise SaaS Visual Atmosphere

---

## 1. Visual Concept & Design Philosophy

### 1.1. Core Philosophy
> **"BACKGROUND LÀ NỀN — CONTENT LÀ TRUNG TÂM"**
> (Background serves as the ambient canvas; content remains the absolute hero.)

Hệ thống **SupplierSelectionDSS** là nền tảng hỗ trợ ra quyết định doanh nghiệp (Decision Support System) đòi hỏi tính chính xác, tập trung cao và cảm giác tin cậy. Background visual system mới được thiết kế để:
- Thoát khỏi sự đơn điệu, phẳng lỳ của nền trắng thuần túy.
- Mang lại chiều sâu thị giác (visual depth), sự bình tĩnh (calmness), phong thái doanh nghiệp hiện đại (Modern Enterprise SaaS) tương đồng với tinh thần của hình ảnh tham chiếu.
- Tôn vinh các thẻ dữ liệu (`.surface-card`, `.dashboard-card`, `.table-card`, v.v.), giúp người dùng phân định rõ ranh giới giữa **Canvas** (nền không gian) và **Surface** (mặt phẳng dữ liệu).
- Tuyệt đối **không cạnh tranh**, không làm mờ chữ, không làm rối mắt hay giảm độ tương phản của bảng biểu, ma trận so sánh AHP, kết quả TOPSIS hay biểu đồ phân tích.

---

## 2. Background Hierarchy (5-Layer Architecture)

Hệ thống được phân cấp theo 5 lớp layer chặt chẽ từ sau ra trước:

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 5: Content Surface & Interactive Controls (z: 1)    │
│  [Cards (#fff), Tables, Forms, Inputs, Buttons, Modals]     │
├─────────────────────────────────────────────────────────────┤
│  Layer 4: Subtle Grid / Dot Matrix Pattern (z: 0)           │
│  [6x5 dot grid, precision contour lines, margin-pinned]     │
├─────────────────────────────────────────────────────────────┤
│  Layer 3: Abstract Geometric Shapes & Facets (z: 0)         │
│  [Top-right translucent polygon ridges, bottom-left facet]  │
├─────────────────────────────────────────────────────────────┤
│  Layer 2: Ambient Atmospheric Gradient (z: 0)               │
│  [Diagonal cool gradient: #f0f6fd -> #f8fbfe -> #f3f7fd]    │
├─────────────────────────────────────────────────────────────┤
│  Layer 1: Base Canvas Background (z: -1)                    │
│  [Base tone: #f4f8fc]                                       │
└─────────────────────────────────────────────────────────────┘
```

1. **Layer 1 — Application Base Canvas**:
   - Màu nền tảng: `#f4f8fc` (màu trắng ngà ánh xanh cực nhạt, dịu mắt, giảm mỏi mắt so với trắng chói `#ffffff`).
2. **Layer 2 — Atmospheric Gradient**:
   - Gradient đa điểm tuyến tính và xuyên tâm rất nhẹ, tạo ánh sáng khuếch tán từ góc trên bên phải lan tỏa xuống trung tâm.
3. **Layer 3 — Abstract Geometric Shapes & Facets**:
   - Các đỉnh núi/đa giác hình học trừu tượng (abstract geometric ridge layers) đặt tại **góc trên bên phải** với độ mờ đục rất thấp (`opacity: 0.18 - 0.35`) và hiệu ứng chuyển màu mờ dần (`fade-out mask`) về phía giữa trang.
   - Hình khối đa giác góc tròn mềm mại ở **góc dưới bên trái** (`opacity: 0.20 - 0.28`).
4. **Layer 4 — Precision Dot Matrix & Geometric Outline**:
   - Cụm ma trận chấm hạt (dot matrix 6x5) và đường viền contour lục giác bo góc (rounded hexagonal contour) ở **góc dưới bên phải**, biểu trưng cho cấu trúc dữ liệu, ma trận phân tích toán học DSS.
5. **Layer 5 — Content Surfaces & Cards**:
   - Các card chứa dữ liệu (`#ffffff`), bảng biểu, form nhập liệu được nâng nổi bật phía trên bằng viền tinh tế (`border: 1px solid var(--border)`) và đổ bóng đa tầng (`box-shadow: var(--shadow-card)`).

---

## 3. Color System & Design Tokens

Tận dụng và chuẩn hóa hệ thống Design Tokens trong [`tokens.css`](../wwwroot/css/tokens.css):

### 3.1. Background & Surface Tokens
| Token Name | Value | Purpose |
| :--- | :--- | :--- |
| `--bg-app-base` | `#f4f8fc` | Màu nền gốc của toàn bộ ứng dụng (`body`, `.app-main`) |
| `--bg-app-gradient` | `linear-gradient(145deg, #f7faff 0%, #f1f6fc 50%, #edf4fc 100%)` | Lớp gradient khí quyển bao phủ |
| `--bg-surface` | `#ffffff` | Màu nền bề mặt của tất cả Cards, Tables, Forms |
| `--bg-surface-elevated`| `#ffffff` | Màu nền của Dropdown, Modal, Popover |
| `--bg-surface-subtle` | `#f8fafc` | Màu nền phụ, table hover, header strip |

### 3.2. Background Decoration Tokens
| Token Name | Value | Purpose |
| :--- | :--- | :--- |
| `--bg-deco-ridge-1` | `rgba(186, 214, 247, 0.28)` | Đỉnh đa giác xa nhất (top-right layer 1) |
| `--bg-deco-ridge-2` | `rgba(165, 202, 245, 0.24)` | Đỉnh đa giác trung gian (top-right layer 2) |
| `--bg-deco-ridge-3` | `rgba(205, 227, 252, 0.32)` | Đỉnh đa giác trước (top-right layer 3) |
| `--bg-deco-contour` | `rgba(147, 197, 253, 0.36)` | Đường viền hình học lục giác (bottom-right) |
| `--bg-deco-dots` | `rgba(125, 175, 235, 0.38)` | Ma trận chấm hạt tinh xảo (bottom-right dots) |
| `--bg-deco-poly-left`| `rgba(195, 220, 248, 0.22)` | Đa giác mềm góc dưới bên trái |

---

## 4. Sidebar as Visual Anchor

Theo đúng tinh thần ảnh tham chiếu và mục 8 trong yêu cầu:
- **Sidebar**: Đóng vai trò là mỏ neo thị giác vững chãi (Visual Anchor), tạo độ tương phản mạnh mẽ với nền sáng của Workspace:
  - Màu nền: Nền xanh than đậm (Dark Executive Navy `#0d1726` / `#0f1d32`).
  - Viền phải: `1px solid rgba(255, 255, 255, 0.08)`.
  - Brand & Navigation: Chữ sáng màu (`#f8fafc`), icon thanh lịch, trạng thái active làm nổi bật với accent color (`var(--terracotta)` hoặc xanh sáng dịu).
  - Footer người dùng: Được tinh chỉnh hài hòa trên nền tối.

---

## 5. Decoration Placement & Layout Safety Zones

Decorations được neo cố định (`position: fixed`) sau lớp nội dung (`z-index: 0`, `pointer-events: none`):

```
┌─────────────────────────────────────────────────────────────┐
│ [TOPBAR: sticky, glassmorphic blur 12px]                   │
├─────────────────────────────────────────────────────────────┤
│                                     [TOP-RIGHT DECORATION]  │
│                                     Soft polygon ridges     │
│                                     (Fades out to center)   │
│                                                             │
│         [SAFE CONTENT ZONE: NO DIRECT DECORATION]           │
│         - KPI Grid Cards                                    │
│         - Data Tables                                       │
│         - AHP Pairwise Matrices                             │
│         - TOPSIS Calculation Tables                         │
│         - Forms & Inputs                                    │
│                                                             │
│ [BOTTOM-LEFT DECORATION]               [BOTTOM-RIGHT DECO]  │
│ Soft translucent facet                 Contour + Dot Grid   │
└─────────────────────────────────────────────────────────────┘
```

- **Quy tắc bất di bất dịch**: Toàn bộ decorative shapes chỉ chiếm các góc rìa (margins và corners). Tuyệt đối không đặt hình khối đặc hay họa tiết tương phản cao ngay phía sau tâm điểm đọc nội dung.

---

## 6. Card Compatibility & Shadow Interaction

Để bề mặt thẻ nội dung luôn nổi bật sắc nét trên nền mới:
- **Card Surface**: Luôn là `#ffffff` nguyên khối.
- **Card Border**: `1px solid #e2e8f0` (hoặc `rgba(226, 232, 240, 0.8)`).
- **Card Shadow**:
  - Standard card: `box-shadow: 0 1px 3px rgba(15, 23, 42, 0.05), 0 1px 2px rgba(15, 23, 42, 0.03);`
  - Hover / Elevated card: `box-shadow: 0 8px 24px rgba(15, 23, 42, 0.07), 0 2px 6px rgba(15, 23, 42, 0.03); transform: translateY(-2px);`
  - Dropdown menu: `box-shadow: 0 12px 30px rgba(15, 23, 42, 0.10);`
  - Modal: `box-shadow: 0 24px 60px rgba(15, 23, 42, 0.18);`

---

## 7. Responsive Behavior

| Thiết bị | Breakpoint | Hành vi của Background System |
| :--- | :--- | :--- |
| **Desktop** | `> 1200px` | Đầy đủ 5 layer: Atmospheric gradient + Top-Right ridges + Bottom-Right contour/dots + Bottom-Left facet. |
| **Tablet** | `768px – 1200px` | Giữ Atmospheric gradient, làm mờ nhẹ các ridges và ẩn dot matrix để tối ưu độ tập trung. |
| **Mobile** | `< 768px` | Ẩn toàn bộ vector shapes phức tạp. Chỉ giữ lớp Atmospheric gradient nhẹ nhàng để đảm bảo 100% hiệu năng, loại bỏ hoàn toàn nguy cơ horizontal overflow. |

---

## 8. Accessibility & Performance (Zero Regression)

1. **Accessibility**:
   - Tất cả các phần tử background trang trí đều có `aria-hidden="true"` và `role="presentation"`.
   - `pointer-events: none` đảm bảo không bao giờ chặn hay gây cản trở chuột/chạm của người dùng.
   - Độ tương phản văn bản: Nền văn bản nằm trên card trắng đạt tỷ lệ tương phản chuẩn WCAG AAA (> 7:1) với `--ink: #0f172a`.
2. **Performance**:
   - Sử dụng 100% **CSS gradient** kết hợp **vector SVG siêu nhẹ** (chỉ vài KB code inline, không tạo HTTP request raster image lớn nào).
   - Sử dụng thuộc tính tăng tốc phần cứng GPU `will-change: transform; transform: translateZ(0)` nếu cần, không có animation liên tục ngốn CPU/GPU.
   - Không gây layout shift (CLS = 0).

---

## 9. Implementation Blueprint

- **Bước 1**: Cập nhật Tokens trong `wwwroot/css/tokens.css`.
- **Bước 2**: Tạo partial view tái sử dụng `Views/Shared/_AppBackground.cshtml` chứa các vector background shapes tinh xảo với `aria-hidden="true"`.
- **Bước 3**: Thêm stylesheet chuyên biệt `wwwroot/css/components/background.css` và liên kết trong `wwwroot/css/site.css`.
- **Bước 4**: Nhúng `_AppBackground.cshtml` vào `Views/Shared/_Layout.cshtml` ngay bên trong `.app-main`.
- **Bước 5**: Cập nhật sidebar style trong `wwwroot/css/base/layout.css` để trở thành **Dark Executive Navy Visual Anchor** đúng chuẩn tham chiếu.
- **Bước 6**: Build và kiểm tra trực quan trên toàn bộ các màn hình (Dashboard, Suppliers, Batches, AHP, TOPSIS, Reports, AuditLog).
