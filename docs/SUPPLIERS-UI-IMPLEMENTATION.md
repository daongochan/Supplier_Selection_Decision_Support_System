# Đặc tả Kỹ thuật & Triển khai Giao diện Quản lý Nhà cung cấp (Suppliers)
**Hệ thống:** SupplierSelectionDSS  
**Tác giả:** Senior UI/UX Designer & Senior ASP.NET Core MVC Engineer  
**Ngày lập:** 22/09/2026  

---

## 1. Hiện trạng Giao diện (Current UI)

- **Breadcrumb**: Sử dụng component chung `_Breadcrumb.cshtml`, hiển thị `Trang chủ > Nhà cung cấp`.
- **Page Header**: Đơn giản chỉ gồm tiêu đề `Suppliers`, mô tả ngắn và nút `Add supplier`.
- **KPI / Summary**: Hoàn toàn **chưa có** khu vực thống kê tổng quan (KPI cards).
- **Bộ lọc / Tìm kiếm**:
  - Form tìm kiếm chỉ có 2 trường: `keyword` (mã, tên, liên hệ) và `status` (All, Active, Inactive).
  - Chưa hỗ trợ lọc theo Nhóm hàng (`ProductGroup`).
  - Nút bấm `Search` và `Reset` đơn điệu.
- **Bảng dữ liệu (Table)**:
  - Các cột hiện tại: `Code`, `Supplier`, `Contact`, `Business field`, `Status`, `Actions`.
  - Chưa hiển thị thông tin Nhóm hàng (`Groups`) được liên kết qua bảng quan hệ nhiều-nhiều `SupplierGroups`.
  - Chưa hiển thị số điện thoại rõ ràng; người liên hệ và email bị gộp chung đơn giản.
  - Cột `Actions` chỉ có text-link `Edit` và nút `Activate` / `Deactivate` nằm sát nhau, chưa có xác nhận thân thiện qua Modal (chỉ submit form trực tiếp).
- **Phân trang**: Sử dụng partial `_Pagination.cshtml` ở chân bảng, hoạt động tốt nhưng chưa được tối ưu về mặt không gian và tương tác.

---

## 2. Các vấn đề cần cải tiến (Problems Identified)

1. **Thiếu tính định hướng dữ liệu (Data-driven Context)**:
   - Thiếu các chỉ số thống kê cơ bản: Tổng số nhà cung cấp, Đang hoạt động, Ngừng hoạt động, Số nhóm hàng cung ứng.
2. **Khả năng lọc còn hạn chế**:
   - Chưa cho phép lọc nhà cung cấp theo Nhóm hàng (`ProductGroup`), dù quan hệ dữ liệu `Supplier <-> ProductGroup` đã tồn tại sẵn trong database.
3. **Mật độ thông tin bảng chưa tối ưu (Information Density & Hierarchy)**:
   - Cột hành động thiếu sự tinh gọn, các nút thao tác nằm ngang chiếm diện tích và dễ bấm nhầm.
   - Thao tác kích hoạt / vô hiệu hóa chưa có hộp thoại xác nhận chuyên nghiệp (`Confirmation Modal`).
   - Cột trạng thái cần đồng bộ với quy chuẩn Status Pill hiện đại (có bullet indicator, màu sắc ngữ nghĩa rõ ràng).
4. **Trải nghiệm thao tác (Action & Usability)**:
   - Cần menu tác vụ `[⋯]` tiện lợi để gom nhóm các hành động: Xem / Chỉnh sửa, Phân nhóm hàng, Kích hoạt / Vô hiệu hóa.
   - Khi thực hiện thay đổi trạng thái, cần có thông báo Toast rõ ràng.

---

## 3. Thiết kế Mục tiêu (Target UI)

Dựa trên tài liệu yêu cầu và ảnh tham chiếu (Reference UI):

### 3.1. Phân tầng thị giác (Visual Hierarchy)
```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ⌂ Trang chủ › Nhà cung cấp                                                  │
│                                                                             │
│ Nhà cung cấp                                         [ + Thêm nhà cung cấp ]│
│ Quản lý thông tin và trạng thái các nhà cung cấp trong hệ thống.            │
├─────────────────────────────────────────────────────────────────────────────┤
│ [ 👥 Tổng NCC: 85 ] [ ✅ Hoạt động: 72 ] [ 🚫 Ngừng: 13 ] [ 📦 Nhóm hàng: 8 ]│
├─────────────────────────────────────────────────────────────────────────────┤
│ 🔍 TÌM KIẾM & LỌC                                                           │
│ [ Nhập mã, tên hoặc liên hệ... ] [ Trạng thái ▼ ] [ Nhóm hàng ▼ ] [Tìm][Lại]│
├─────────────────────────────────────────────────────────────────────────────┤
│ TABLE CARD                                                                  │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Mã NCC | Tên nhà cung cấp | Nhóm hàng | Liên hệ | Địa chỉ | Trạng thái |⋯│ │
│ ├─────────────────────────────────────────────────────────────────────────┤ │
│ │ SUP001 | Công ty TNHH ABC | Điện tử   | Mr. Nam | Hà Nội  | ● Hoạt động |⋯│ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│ Hiển thị 1 – 10 trên tổng số 85 kết quả                           ‹ 1 2 3 › │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2. Chi tiết từng vùng chức năng
1. **Breadcrumb**:
   - `⌂ Trang chủ › Nhà cung cấp` (nhỏ gọn, không card, không shadow).
2. **Page Header**:
   - Tiêu đề `Nhà cung cấp`, phụ đề mô tả rõ ràng.
   - Nút `+ Thêm nhà cung cấp` màu xanh thương hiệu (`var(--terracotta)`), kích thước vừa vặn.
3. **KPI Summary Area**:
   - 4 thẻ KPI đồng bộ:
     1. **Tổng nhà cung cấp**: Icon xanh dương, số lượng tổng.
     2. **Đang hoạt động**: Icon xanh lá, số lượng và tỷ lệ % hoạt động.
     3. **Ngừng hoạt động**: Icon xám/đỏ, số lượng và tỷ lệ % ngừng hoạt động.
     4. **Nhóm hàng cung ứng**: Icon tím indigo, số lượng nhóm hàng đang active.
   - Nguồn dữ liệu: Truy vấn thực tế từ `_context.Suppliers` và `_context.ProductGroups`, tuyệt đối không hardcode.
4. **Search & Filter Card**:
   - Card nền trắng (`var(--surface)`), viền mỏng (`var(--border)`), shadow nhẹ (`var(--shadow-card)`).
   - Tiêu đề card: `<i class="bi bi-funnel"></i> Tìm kiếm & Lọc`.
   - Ô tìm kiếm từ khóa (`keyword`): Chiếm ưu thế về chiều rộng (`minmax(0, 2.5fr)`).
   - Dropdown Trạng thái: `Tất cả`, `Đang hoạt động`, `Ngừng hoạt động`.
   - Dropdown Nhóm hàng: `Tất cả nhóm hàng` + danh sách các nhóm hàng đang hoạt động lấy từ database.
   - Cặp nút `Tìm kiếm` (`btn-primary`) & `Đặt lại` (`btn-outline`), cùng chiều cao 42px.
   - **Tuyệt đối không có Rows / PageSize selector** trong vùng tìm kiếm.
5. **Table Card & Actions**:
   - Bảng trung tâm, full-bleed trong card bo góc mềm mại.
   - Các cột:
     - `Mã NCC`: In đậm (`SUP001`).
     - `Tên nhà cung cấp`: Tên chính in đậm, hiển thị thêm Lĩnh vực kinh doanh bên dưới.
     - `Nhóm hàng`: Huy hiệu pill nhỏ cho từng nhóm hàng liên kết.
     - `Thông tin liên hệ`: Tên người liên hệ, số điện thoại, email với icon trực quan.
     - `Địa chỉ`: Địa chỉ công ty rút gọn, có tooltip nếu dài.
     - `Trạng thái`: Status pill chuẩn Enterprise (`● Hoạt động` / `● Ngừng hoạt động`).
     - `Thao tác`: Dropdown menu `[⋯]` gọn gàng:
       - Xem / Chỉnh sửa
       - Kích hoạt / Vô hiệu hóa (tích hợp `data-dss-confirm` mở Modal xác nhận đẹp mắt).
6. **Table Footer & Pagination**:
   - Nằm trọn vẹn trong `table-card`.
   - Bên trái: `Hiển thị X – Y trên tổng số Z kết quả`.
   - Bên phải: Phân trang `‹ 1 2 3 ... ›`.
7. **Confirmation Modal & Toast**:
   - Modal xác nhận hành động vô hiệu hóa / kích hoạt bằng modal hệ thống `dssConfirmModal`.
   - Toast tự động hiển thị thông báo thành công / thất bại sau thao tác.

---

## 4. Các tệp bị ảnh hưởng (Files Affected)

1. **Models / ViewModels**:
   - [`Models/SupplierIndexViewModel.cs`](../Models/SupplierIndexViewModel.cs):
     Bổ sung các trường KPI (`TotalSuppliers`, `ActiveSuppliers`, `InactiveSuppliers`, `TotalGroups`), trường lọc nhóm hàng (`GroupId`), và danh sách `Groups` cho dropdown.
2. **Services**:
   - [`Services/SupplierService.cs`](../Services/SupplierService.cs):
     - Thêm `.Include(x => x.Groups)` vào `SearchAsync`.
     - Thêm tham số lọc `int? groupId = null` vào `SearchAsync`.
     - Thêm hàm `GetKpiSummaryAsync()` lấy số liệu thống kê thực tế.
     - Thêm hàm `GetActiveGroupsAsync()` lấy danh sách nhóm hàng hoạt động cho dropdown lọc.
3. **Controllers**:
   - [`Controllers/SupplierController.cs`](../Controllers/SupplierController.cs):
     - Nhận tham số `int? groupId` trong action `Index`.
     - Gọi service lấy KPI và danh sách nhóm hàng, nạp vào `SupplierIndexViewModel`.
4. **Views**:
   - [`Views/Supplier/Index.cshtml`](../Views/Supplier/Index.cshtml):
     - Viết lại toàn bộ giao diện theo chuẩn thiết kế mục tiêu: KPI grid, Search card nâng cấp, Bảng dữ liệu hiện đại, Dropdown actions, Modal xác nhận, Empty state.
5. **CSS / Styles**:
   - [`wwwroot/css/pages/supplier.css`](../wwwroot/css/pages/supplier.css):
     - Thêm định kiểu cho KPI cards, Filter card, Table wrapper, Status pills, Dropdown action menu, Responsive breakpoints.
   - [`wwwroot/css/site.css`](../wwwroot/css/site.css):
     - Cập nhật phiên bản import cache-busting.

---

## 5. Hành vi Responsive (Responsive Behavior)

- **Desktop (>= 1200px)**:
  - 4 thẻ KPI xếp trên 1 hàng ngang.
  - Filter card trải dài với tỉ lệ: Search (2.5fr) | Trạng thái (1.2fr) | Nhóm hàng (1.2fr) | Nút (auto).
  - Bảng dữ liệu hiển thị đầy đủ các cột.
- **Tablet (768px – 1199px)**:
  - 4 thẻ KPI tự động chuyển thành lưới 2x2.
  - Bộ lọc chuyển thành 2 hàng (Search dòng 1; Trạng thái, Nhóm hàng và Nút ở dòng 2).
  - Bảng dữ liệu cho phép cuộn ngang mượt mà trong `.data-table-wrapper`.
- **Mobile (< 768px)**:
  - 4 thẻ KPI xếp dọc (1 cột).
  - Bộ lọc xếp chồng dọc (100% width cho từng field).
  - Nút thêm mới và tiêu đề chuyển sang dạng flex-column.
  - Phân trang ở chân bảng căn chỉnh linh hoạt không bị vỡ bố cục.

---

## 6. Tiêu chí Nghiệm thu (Acceptance Criteria)

- [x] Không thay đổi business rule hoặc database schema (Contract intact).
- [x] Breadcrumb nhẹ nhàng: `⌂ Trang chủ › Nhà cung cấp`.
- [x] Header rõ ràng với nút `+ Thêm nhà cung cấp`.
- [x] 4 thẻ KPI chính xác 100% từ database, không có số liệu giả.
- [x] Search card có tìm kiếm từ khóa, lọc trạng thái, lọc nhóm hàng; **không có selector Rows/PageSize**.
- [x] Bảng hiển thị các cột chuẩn xác, bao gồm Nhóm hàng được liên kết.
- [x] Status pill rõ ràng (`● Hoạt động`, `● Ngừng hoạt động`).
- [x] Action menu tinh gọn `[⋯]`, không có text links chật chội.
- [x] Thao tác Kích hoạt / Vô hiệu hóa kích hoạt Confirmation Modal đẹp mắt, không dùng `window.alert()`.
- [x] Toast notification hiển thị khi có thao tác thành công hoặc lỗi.
- [x] Phân trang nằm gọn trong Table Footer, hiển thị chính xác phạm vi kết quả.
- [x] Empty state đẹp mắt khi không tìm thấy kết quả hoặc hệ thống trống.
- [x] 100% Responsive, không gây tràn ngang toàn trang (`body overflow-x`).
- [x] Build và kiểm thử unit test đạt 100% thành công.
