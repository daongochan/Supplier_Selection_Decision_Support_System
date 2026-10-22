# TÀI LIỆU THIẾT KẾ GIAO DIỆN DASHBOARD (ENTERPRISE DECISION SUPPORT WORKSPACE)

**Hệ thống:** SupplierSelectionDSS  
**Phân hệ:** Decision Dashboard Workspace  
**Phiên bản:** 2.0 (Modern Enterprise SaaS)  
**Tài liệu tham chiếu:** Mockup chuẩn tham khảo (`media_1790043220267`) & Hệ thống thiết kế `docs/BACKGROUND-DESIGN-SPEC.md`

---

## 1. Mục Tiêu & Tầm Nhìn Thiết Kế

Dashboard của **SupplierSelectionDSS** được định vị là **Enterprise Decision Support Workspace** (Không gian làm việc hỗ trợ ra quyết định doanh nghiệp), hoàn toàn khác biệt với các bảng điều khiển CRUD Admin thông thường.

### Trọng Tâm Nghiệp Vụ:
1. **Context-Centric:** Đợt đánh giá (`EvaluationBatch`) là trung tâm ngữ cảnh của toàn bộ hệ thống. Mọi phân tích, tính toán AHP, xếp hạng TOPSIS và phê duyệt đều chuyển động đồng bộ theo Đợt đánh giá được chọn.
2. **Decision-Driven:** Giúp người ra quyết định (Purchasing Manager, Expert, Ban Lãnh Đạo) nắm bắt tức thì:
   - Quy mô & năng lực hệ thống nhà cung cấp hiện hành.
   - Trạng thái nhất quán của ma trận trọng số AHP ($CR \le 0.10$).
   - Bảng xếp hạng nhà cung cấp tối ưu theo phương pháp TOPSIS ($C_i$).
   - Tiến độ thực thi quy trình đánh giá 5 bước và tình trạng phê duyệt quyết định.
3. **Visual Calm & High Density:** Trực quan hóa dữ liệu sắc nét, bố cục khoa học, tỷ lệ khoảng trắng cân đối, hệ thống đổ bóng tinh tế (`--shadow-card`, `--shadow-elevated`), tương phản văn bản cao theo chuẩn WCAG AA.

---

## 2. Bố Cục Không Gian & Visual Hierarchy

Bố cục tổng thể tuân theo luồng đọc tự nhiên của nhà quản trị:  
**Context → KPI → Analysis → Ranking → Workflow → Decision**

```text
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ Breadcrumb: ⌂ Trang chủ › Dashboard                                                   │
├──────────────────────────────────────────────────────────┬─────────────────────────────┤
│ Page Title: Dashboard                                     │ Thời gian thực              │
│ Subtitle: Hệ thống hỗ trợ ra quyết định...                │ [Đợt đánh giá: BATCH-002 ▼] │
├──────────────────────────────────────────────────────────┴─────────────────────────────┤
│ CURRENT BATCH CONTEXT BANNER                                                           │
│ [Icon] Mã đợt & Tên đợt │ Nhóm hàng │ Trạng thái │ Tiến độ đánh giá │ [Xem chi tiết ➔] │
├────────────────────────────────────────────────────────────────────────────────────────┤
│ 6 KPI SUMMARY CARDS                                                                    │
│ 1.Tổng NCC  │ 2.Nhóm hàng │ 3.Đợt ĐG  │ 4.Tiêu chí │ 5.CR (AHP)  │ 6.Top 1 (TOPSIS)        │
├────────────────────────────┬─────────────────────────────┬─────────────────────────────┤
│ ROW 1: TỔNG QUAN & XU HƯỚNG │                             │                             │
│ 1.1 Tình trạng đợt ĐG      │ 1.2 Xu hướng ĐG thời gian   │ 1.3 Phân bố NCC theo status │
│ (Donut Chart)              │ (Multi-line Chart)          │ (Donut Chart)               │
├────────────────────────────┴─────────────┬───────────────┴─────────────────────────────┤
│ ROW 2: TOPSIS RANKING & TIẾN ĐỘ          │                                             │
│ 2.1 Top 5 Nhà Cung Cấp TOPSIS            │ 2.2 Biểu đồ Xếp Hạng TOPSIS                 │ 2.3 Tiến độ Đợt ĐG
│ (Table: Rank, NCC, Nhóm, CC, Thao tác)   │ (Horizontal Bar Chart)                      │ (5-Step Stepper + Stage)
├────────────────────────────┬─────────────┴───────────────┬───────────────┬─────────────┤
│ ROW 3: CHI TIẾU SÂU & QUYẾT ĐỊNH                         │               │             │
│ 3.1 Trọng số AHP           │ 3.2 Radar So Sánh TOP 3     │ 3.3 Thông Tin │ 3.4 Trạng Thái
│ (Horizontal Progress Bars) │ (Radar Multi-criteria Chart)│ Hệ Thống      │ Phê Duyệt   │
└────────────────────────────┴─────────────────────────────┴───────────────┴─────────────┘
```

---

## 3. Hệ Thống Màu Sắc & Tokens Giao Diện

| Token / Nhóm | Giá trị màu | Ý nghĩa nghiệp vụ | Ứng dụng trên Dashboard |
| :--- | :--- | :--- | :--- |
| **Primary Blue** | `#1d4ed8` / `#2563eb` | Hành động chính, nhận diện DSS | Stepper active, nút bấm, line chart số NCC, thanh bar TOPSIS |
| **Sage / Emerald** | `#15803d` / `#16a34a` | Hoàn thành, Hợp lệ, Lợi ích | CR AHP đạt chuẩn ($CR \le 0.1$), trạng thái Hoạt động, Đã phê duyệt |
| **Amber / Warning** | `#d97706` / `#f59e0b` | Đang thực hiện, Chi phí, Chờ xử lý | Đợt đang đánh giá, line chart số đợt, badge Chờ phê duyệt |
| **Terracotta / Red** | `#b85042` / `#dc2626` | Không hợp lệ, Từ chối, Đã hủy | CR AHP không đạt ($CR > 0.1$), Đợt đã hủy, Phê duyệt bị từ chối |
| **Indigo / Purple** | `#7c3aed` / `#6366f1` | Trọng số tiêu chí, Phân loại đặc thù | Progress bar tiêu chí AHP, badge nhóm hàng |
| **Surface** | `#ffffff` | Nền thẻ làm việc chính | Mặt các card nội dung, bảng, container biểu đồ |
| **Canvas Soft** | `#f8fafc` | Nền phụ trợ | Nền hover, background thẻ thông tin |
| **Border Subtle** | `#e2e8f0` | Đường phân chia | Viền card, phân tách hàng bảng dữ liệu |

---

## 4. Đặc Tả Chi Tiết Từng Khối Giao Diện

### 4.1. Header & Bộ Chọn Ngữ Cảnh (Current Batch Selector)
- **Vị trí:** Header góc trên bên phải.
- **Hiển thị:**
  - Đồng hồ thời gian thực: Giờ, ngày tháng năm (tự động cập nhật nhịp 1s bằng JavaScript nhẹ).
  - Dropdown `[Đợt đánh giá: {BatchCode} ▼]`: Cho phép chuyển nhanh giữa các đợt đánh giá đang có trong hệ thống (`EvaluationBatches`). Khi chọn một đợt khác, Dashboard cập nhật toàn bộ các chỉ số của đợt đó.

### 4.2. Current Batch Context Banner
- Thẻ nổi bật chiều ngang (`grid-template-columns: auto 1.5fr 1fr 1fr 1.5fr auto`):
  - **Biểu tượng đợt:** Icon khối hộp 3D (`bi-boxes` hoặc `bi-box-seam`) trên nền xanh nhạt.
  - **Thông tin đợt:** Mã đợt (In đậm, font mono/sans) + Tên đợt đánh giá.
  - **Nhóm hàng:** Tên nhóm sản phẩm/dịch vụ áp dụng (`Group.GroupName`).
  - **Trạng thái:** Status pill (`Đang đánh giá` vàng cam, `Hoàn thành` xanh lá, `Bản nháp` xám, `Đã hủy` đỏ).
  - **Tiến độ nhập liệu:** Tỷ lệ nhà cung cấp đã được chấm điểm trên tổng số NCC của đợt (`X / Y nhà cung cấp - Z%`).
  - **Hành động nhanh:** Nút `[ Xem chi tiết ➔ ]` dẫn thẳng tới màn hình chi tiết đợt (`/EvaluationBatch/Details/{id}`).

### 4.3. 6 Thẻ KPI Tinh Gọn (KPI Metric Cards)
- Bố cục lưới 6 cột trên màn hình lớn (`repeat(6, 1fr)`), tự động co giãn 3 cột trên tablet và 1 cột trên mobile:
  1. **Tổng nhà cung cấp:** Số lượng NCC hệ thống + Tỷ lệ hoạt động.
  2. **Nhóm hàng:** Tổng số nhóm hàng hóa/dịch vụ danh mục.
  3. **Đợt đánh giá:** Tổng số đợt + Số đợt đã hoàn thành.
  4. **Tiêu chí:** Tổng số tiêu chí đánh giá hoạt động.
  5. **CR (AHP):** Tỷ số nhất quán $CR$ của đợt hiện tại (VD: `0.0130`), kèm badge `✓ Đạt yêu cầu` ($CR \le 0.10$) hoặc `⚠ Cần kiểm tra` ($CR > 0.10$).
  6. **Top 1 (TOPSIS):** Tên nhà cung cấp dẫn đầu đợt hiện tại kèm hệ số gần gũi $C_i$ (VD: `CC: 0.823`).

### 4.4. Hàng Biểu Đồ 1: Tình Trạng & Xu Hướng (Row 1 Charts)
1. **Tình trạng đợt đánh giá (Donut Chart):**
   - Trực quan hóa tỷ lệ các đợt đánh giá theo 4 trạng thái: Hoàn thành, Đang đánh giá, Bản nháp, Đã hủy.
   - Trung tâm donut hiển thị tổng số đợt (`10 Tổng số đợt`).
2. **Xu hướng đánh giá theo thời gian (Line Chart):**
   - Thể hiện số lượng đợt đánh giá và số lượt nhà cung cấp được đưa vào đánh giá qua các tháng.
   - Đường cong mượt mà (tension: 0.35), điểm nhấn tròn rõ ràng, tooltip chi tiết từng tháng.
3. **Phân bố nhà cung cấp theo trạng thái (Donut Chart):**
   - Thể hiện cơ cấu nhà cung cấp `Hoạt động` vs `Ngừng hoạt động`.
   - Trung tâm donut hiển thị tổng số NCC (`6 Tổng số NCC`).

### 4.5. Hàng 2: TOPSIS Ranking & Tiến Độ Quy Trình (Row 2 Workspace)
1. **Top 5 Nhà Cung Cấp theo TOPSIS (Data Table):**
   - Cột: Hạng (Badge tròn nổi bật: #1 Vàng kim, #2 Bạc, #3 Đồng, #4-#5 Slate), Tên nhà cung cấp & Mã NCC, Nhóm hàng, Hệ số gần gũi ($C_i$ định dạng `0.0000`), Trạng thái hoạt động, Nút thao tác `⋯`.
2. **Biểu đồ xếp hạng TOPSIS (Horizontal Bar Chart):**
   - Các thanh ngang tỷ lệ chuẩn xác theo giá trị $C_i \in [0, 1]$.
   - Màu sắc phân tầng: Top 1 thanh màu xanh dương chủ đạo (`#2563eb`), các vị trí tiếp theo dùng sắc thái chuyển tiếp trang nhã.
3. **Tiến độ đánh giá hiện tại (Evaluation Workflow Stepper):**
   - **5 bước chuẩn quy trình DSS:**
     1. `Nhập dữ liệu` (Điểm đánh giá các NCC theo tiêu chí)
     2. `AHP` (Ma trận so sánh cặp & Tính trọng số $w_j$, kiểm tra $CR$)
     3. `TOPSIS` (Chuẩn hóa ma trận, tính PIS/NIS, khoảng cách và $C_i$)
     4. `So sánh` (Phân tích radar & biểu đồ so sánh)
     5. `Phê duyệt` (Trình phê duyệt và ra quyết định lựa chọn)
   - **Thẻ trạng thái giai đoạn hiện tại:** Hiển thị vòng tròn tiến độ phần trăm giai đoạn thực tế (VD: `60%`, `Đang thực hiện: TOPSIS`, ghi chú mô tả giai đoạn hiện tại).

### 4.6. Hàng 3: Phân Tích Chuyên Sâu & Phê Duyệt Quyết Định (Row 3 Deep Dive)
1. **Phân bố trọng số tiêu chí AHP (Criteria Weights):**
   - Dạng Horizontal Progress Bars thể hiện tỷ trọng % đóng góp của từng tiêu chí trong đợt đánh giá (lấy từ `AHPCriteriaWeights`).
   - Tên tiêu chí tiếng Việt, thanh tiến độ bo tròn, nhãn phần trăm căn lề phải rõ ràng.
2. **So sánh nhà cung cấp TOP 3 (Radar Chart):**
   - Trục radar là danh sách các tiêu chí đánh giá của đợt.
   - Các đa giác so sánh tương ứng với Top 3 nhà cung cấp dẫn đầu, thể hiện trực quan thế mạnh/điểm yếu của từng nhà cung cấp trên các phương diện (Chất lượng, Giá cả, Giao hàng, Dịch vụ...).
3. **Thông tin hệ thống (System Information):**
   - Tổng số người dùng trong hệ thống.
   - Số lượng vai trò phân quyền.
   - Phiên bản ứng dụng (`v1.0.0 Enterprise`).
   - Cơ sở dữ liệu (`Microsoft SQL Server`).
   - Tình trạng vận hành (`Sẵn sàng 100%`).
4. **Trạng thái phê duyệt (Approval Status Card):**
   - Badge trạng thái quyết định: `Đã phê duyệt` / `Chờ phê duyệt` / `Yêu cầu đánh giá lại` / `Chưa tạo phê duyệt`.
   - Nhà cung cấp được chọn phê duyệt.
   - Cán bộ/Lãnh đạo phê duyệt & Thời điểm phê duyệt.
   - Các nút tác vụ nhanh: `[ Xem báo cáo PDF ]`, `[ Tải Excel ]` (Tích hợp trực tiếp với `ReportService`).

---

## 5. Trạng Thái Giao Diện & Khả Năng Thích Ứng (States & Responsive)

- **Loading Skeleton State:** Hiệu ứng shimmer tinh tế cho thẻ KPI, thanh tiến độ và container biểu đồ trong thời gian tải dữ liệu.
- **Empty State Nghiệp Vụ:**
  - Nếu chưa có đợt đánh giá nào: Hiển thị minh họa rỗng kèm nút hành động `[ + Tạo đợt đánh giá mới ]`.
  - Nếu đợt đánh giá được chọn chưa tính AHP hoặc TOPSIS: Hiển thị thẻ hướng dẫn chuyên môn giải thích bước cần thực hiện tiếp theo (VD: "Đợt này đang ở bước Nhập dữ liệu, bấm vào đây để chuyển sang màn hình tính AHP").
- **Responsive Breakpoints:**
  - `> 1280px`: Bố cục đầy đủ 3 cột biểu đồ và bảng xếp hạng rộng rãi.
  - `992px - 1280px`: Hàng biểu đồ chuyển sang 2 cột, các panel tự động xếp chồng hài hòa.
  - `< 768px`: Toàn bộ các cột chuyển về 1 cột dọc (Single Column Flow), bảng có thanh cuộn ngang `data-table-wrapper`, biểu đồ co giãn tỷ lệ 100% theo chiều rộng màn hình.
