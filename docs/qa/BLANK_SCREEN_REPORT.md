# BÁO CÁO ĐIỀU TRA & TÁI HIỆN LỖI MÀN HÌNH TRẮNG (BLANK SCREEN AUDIT REPORT)
**Dự án:** Supplier Selection Decision Support System (AHP + TOPSIS)  
**Môi trường:** ASP.NET Core 10 MVC, EF Core 10, SQL Server 2022  
**Database kiểm thử:** `SupplierSelectionDSS_QA` (tách biệt hoàn toàn DB production)  
**Ngày thực hiện:** 23/10/2026  
**Trạng thái kiểm thử:** Hoàn thành điều tra, tái hiện 100% bằng chứng và phân tích nguyên nhân gốc rễ (Root Cause Analysis).

---

## 1. TỔNG QUAN VẤN ĐỀ (EXECUTIVE SUMMARY)

Người dùng báo cáo sự cố nghiêm trọng (P0): **Khi truy cập hoặc thao tác trên một số trang, màn hình chỉ hiển thị một màu trắng hoàn toàn (Blank Screen), không có nội dung, không có thông báo lỗi, không có nút điều hướng.**

### Kết quả điều tra thực nghiệm (Automated Crawl Sweep):
- **Tổng số kịch bản/endpoint được quét:** 230 lượt kiểm tra trên 5 vai trò (Anonymous, Admin, Manager, ProcurementOfficer, WarehouseStaff).
- **Số lượt xuất hiện màn hình trắng (Blank Screen):** **73 lượt** (chiếm 31.7% số lượt kiểm tra).
- **Đặc điểm chung của các trường hợp màn hình trắng:**
  - HTTP Status Code: `404 Not Found` (73 trường hợp).
  - Response Body: **0 bytes (`Content-Length: 0`)**.
  - Không có bất kỳ thẻ HTML, DOM elements, hay tài nguyên nào được trả về. Trình duyệt render một tab trắng tinh.
- **Bằng chứng dữ liệu gốc:** Lưu tại `docs/qa/evidence/blank-screens/crawler_results.json`.

---

## 2. BẢNG THỐNG KÊ CHI TIẾT 73 TRƯỜNG HỢP MÀN HÌNH TRẮNG

| Nhóm Endpoint | Đường dẫn cụ thể | Tần suất xuất hiện | HTTP Code | Byte trả về | Nguyên nhân kích hoạt |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **MCDM Routes (Thiếu batchId)** | `/AHP`, `/AHP/Index` | 10 | 404 | 0 bytes | Người dùng truy cập trực tiếp menu hoặc thanh địa chỉ mà không truyền `batchId`. Controller trả về `NotFound()`. |
| **MCDM Routes (Thiếu batchId)** | `/TOPSIS`, `/TOPSIS/Index` | 10 | 404 | 0 bytes | Truy cập TOPSIS không truyền `batchId`. Controller trả về `BadRequest("...")` / `NotFound()`. |
| **MCDM Routes (Thiếu batchId)** | `/DecisionResult`, `/DecisionResult/Index` | 10 | 404 | 0 bytes | Truy cập trang Quyết định không truyền `batchId`. Controller trả về `NotFound()`. |
| **MCDM Routes (Thiếu batchId)** | `/Evaluation/Index`, `/Evaluation` | 8 | 404 | 0 bytes | Truy cập trang Nhập điểm không truyền `batchId`. Controller trả về `NotFound()`. |
| **MCDM Routes (Thiếu batchId)** | `/Report/Index`, `/Report/ViewReport` | 4 | 404 | 0 bytes | Truy cập Báo cáo không truyền `batchId`. Controller trả về `NotFound()`. |
| **Route không tồn tại / Gõ sai URL** | `/NonExistentPage` (mọi role) | 5 | 404 | 0 bytes | URL sai chính tả, route 404 thông thường từ người dùng. |
| **Bảo vệ tài nguyên tĩnh** | `/reports/secret.pdf` (mọi role) | 5 | 404 | 0 bytes | Middleware chặn truy cập trực tiếp webroot `/reports` trả về `NotFound()`. |
| **Thực thể không tồn tại (ID 9999)** | `/EvaluationBatch/Details/9999` | 4 | 404 | 0 bytes | Chi tiết đợt đánh giá với ID không tồn tại trong DB. |
| **Thực thể không tồn tại (ID 9999)** | `/Supplier/Edit/9999` | 2 | 404 | 0 bytes | Chỉnh sửa nhà cung cấp với ID rác/không tồn tại. |
| **Thực thể không tồn tại (ID 9999)** | `/ProductGroup/Edit/9999` | 2 | 404 | 0 bytes | Chỉnh sửa nhóm sản phẩm không tồn tại. |
| **Thực thể không tồn tại (ID 9999)** | `/Criteria/Edit/9999` | 2 | 404 | 0 bytes | Chỉnh sửa tiêu chí không tồn tại. |
| **Thực thể không tồn tại (ID 9999)** | `/User/Edit/9999` | 1 | 404 | 0 bytes | Chỉnh sửa tài khoản người dùng không tồn tại. |
| **Đợt đánh giá chưa khởi tạo dữ liệu** | `/EvaluationBatch/Details/1`, `/Evaluation?batchId=1`, `/EvaluationBatch/1/Result` | 10 | 404 | 0 bytes | Đợt đánh giá tồn tại nhưng chưa có bảng tiêu chí hoặc chưa qua bước tính toán. |
| **Tổng cộng** | | **73** | **404** | **0 bytes** | |

---

## 3. PHÂN TÍCH NGUYÊN NHÂN GỐC RỄ (ROOT CAUSE ANALYSIS)

### 3.1. Root Cause #1 (P0 - Kiến trúc Pipeline ASP.NET Core)
- **Vấn đề:** Trong `Program.cs`, middleware xử lý mã lỗi HTTP `UseStatusCodePagesWithReExecute` **hoàn toàn vắng bóng**.
- **Cơ chế gây lỗi:**
  - Khi bất kỳ controller nào trả về `NotFound()`, `BadRequest()`, `Forbid()`, hoặc khi route không khớp với route table, ASP.NET Core chỉ gán HTTP Header `Status: 404` (hoặc 400, 403, 429) và kết thúc pipeline mà **không ghi nội dung HTML vào response body** (`Content-Length: 0`).
  - Trình duyệt nhận được mã HTTP 404 kèm body rỗng nên hiển thị một trang trắng xóa.
  - Ngay cả trong môi trường Development, `app.UseDeveloperExceptionPage()` chỉ bắt các unhandled exceptions (HTTP 500), hoàn toàn không can thiệp vào các status codes như 404, 403, 400.

### 3.2. Root Cause #2 (P0 - Broken Action Link trên Dashboard)
- **Vấn đề:** Tại file `Views/Dashboard/Index.cshtml:796`:
  ```html
  <a asp-controller="Approval" asp-action="Approve" asp-route-batchId="@Model.SelectedBatchId.Value" class="btn-outline btn-mt-auto">
      <span>Submit for Approval</span>
  </a>
  ```
- **Cơ chế gây lỗi:** Toàn bộ hệ thống không hề tồn tại `ApprovalController`. Khi người dùng (vai trò Manager) click nút "Submit for Approval" từ thẻ Approval Status trên Dashboard, trình duyệt điều hướng tới `/Approval/Approve?batchId=...`. Pipeline tìm không thấy controller $\to$ trả về HTTP 404 rỗng $\to$ **Màn hình trắng ngay trước mắt người quản lý**.
- **Khắc phục:** Sửa thành `asp-controller="DecisionResult" asp-action="Index" asp-route-batchId="@Model.SelectedBatchId.Value"`.

### 3.3. Root Cause #3 (P0 - Xử lý thiếu tham số `batchId` trong các MCDM Controllers)
- **Vấn đề:**
  - Các controllers nghiệp vụ AHP, TOPSIS, DecisionResult, Evaluation, Report đều phụ thuộc chặt chẽ vào `batchId`.
  - Khi người dùng điều hướng từ thanh menu chính hoặc bookmark trình duyệt (`/AHP`, `/TOPSIS`, `/DecisionResult`), `batchId` nhận giá trị mặc định là `0` hoặc không có route parameter phù hợp.
  - Mã nguồn hiện tại xử lý:
    ```csharp
    if (batchId <= 0)
    {
        return NotFound(); // hoặc BadRequest()
    }
    ```
    Hành vi này trực tiếp đẩy người dùng vào HTTP 404 rỗng $\to$ màn hình trắng.
- **Khắc phục:** Thay vì trả về `NotFound()`, các action này cần:
  1. Hỗ trợ route mặc định không có `batchId` (`[HttpGet("AHP")]`, `[HttpGet("TOPSIS")]`, v.v.).
  2. Khi `batchId <= 0`, kiểm tra xem có đợt đánh giá khả dụng nào không; nếu không, chuyển hướng người dùng an toàn về `/EvaluationBatch` kèm thông báo `TempData["WarningMessage"]` hướng dẫn người dùng chọn đợt đánh giá cần thao tác.

### 3.4. Root Cause #4 (Trang lỗi mặc định chưa được đầu tư)
- **Vấn đề:** File `Views/Shared/Error.cshtml` vẫn là template mặc định tiếng Anh sơ sài của ASP.NET Core, không có bố cục Navy+Gold, không phân biệt các mã lỗi (404, 403, 400, 429, 500), và không có nút điều hướng.
- **Khắc phục:** Thiết kế lại `Views/Shared/Error.cshtml` theo chuẩn giao diện DSS hiện đại:
  - Header & logo nhận diện hệ thống.
  - Phân loại rõ ràng:
    - `404`: "Không tìm thấy trang" — URL không tồn tại hoặc dữ liệu đã bị xóa.
    - `403`: "Không có quyền truy cập" — Tài khoản không đủ thẩm quyền cho chức năng này.
    - `400`: "Yêu cầu không hợp lệ" — Dữ liệu gửi lên không đúng định dạng hoặc phiên làm việc đã hết hạn.
    - `429`: "Quá nhiều yêu cầu" — Bị giới hạn tốc độ truy cập.
    - `500`: "Lỗi xử lý hệ thống" — Gặp sự cố máy chủ nội bộ.
  - Các nút hành động: "Quay lại trang trước", "Về trang chủ Dashboard", "Hỗ trợ kỹ thuật".
  - Hiển thị `RequestId` để tra cứu trong hệ thống log.

### 3.5. Root Cause #5 & #6 (Thiếu CSS Tokens & Rủi ro Dark Mode)
- **Vấn đề:**
  - 5 biến CSS token được gọi trong mã CSS nhưng chưa được khai báo trong `tokens.css`: `--brand-primary`, `--primary`, `--muted-dark`, `--shadow-elevated`, `--shadow-sm`.
  - `tokens.css` thiếu khai báo các biến cho `[data-theme="dark"]`, có thể dẫn tới trường hợp trình duyệt kích hoạt dark mode khiến chữ trắng chìm trên nền trắng (White-on-White), tạo cảm giác như màn hình bị trắng nội dung.
- **Khắc phục:** Bổ sung đầy đủ 5 biến CSS token và hệ thống màu tương phản cao cho dark mode.

---

## 4. MA TRẬN GIẢI PHÁP & TIẾN ĐỘ THỰC HIỆN

| Mã | Hạng mục cần khắc phục | File liên quan | Mức độ ưu tiên | Trạng thái |
| :---: | :--- | :--- | :---: | :---: |
| **FIX-01** | Bật `UseStatusCodePagesWithReExecute("/Home/Error?statusCode={0}")` | `Program.cs` | **P0** | Sẵn sàng triển khai |
| **FIX-02** | Nâng cấp `HomeController.Error(int? statusCode)` & `ErrorViewModel` | `Controllers/HomeController.cs`, `Models/ErrorViewModel.cs` | **P0** | Sẵn sàng triển khai |
| **FIX-03** | Thiết kế lại `Views/Shared/Error.cshtml` (Navy+Gold, tiếng Việt) | `Views/Shared/Error.cshtml` | **P0** | Sẵn sàng triển khai |
| **FIX-04** | Sửa broken link `Approval` $\to$ `DecisionResult` trên Dashboard | `Views/Dashboard/Index.cshtml` | **P0** | Sẵn sàng triển khai |
| **FIX-05** | Bổ sung Graceful Redirect khi thiếu `batchId` trên MCDM Controllers | `Controllers/AHPController.cs`, `TOPSISController.cs`, `DecisionResultController.cs`, `EvaluationController.cs`, `ReportController.cs` | **P0** | Sẵn sàng triển khai |
| **FIX-06** | Bổ sung 5 tokens CSS còn thiếu trong `tokens.css` + Dark Mode tokens | `wwwroot/css/tokens.css` | **P1** | Sẵn sàng triển khai |
| **FIX-07** | Cấu hình Request Localization hỗ trợ cả dấu `.` và `,` cho số thập phân | `Program.cs` | **P1** | Sẵn sàng triển khai |
| **FIX-08** | Viết Integration Smoke Tests xác minh 100% routes không còn blank screen | `SupplierSelectionDSS.Tests/Integration/RouteSmokeAndBlankScreenSweepTests.cs` | **P0** | Sẵn sàng triển khai |

---

## 5. KẾT LUẬN & BƯỚC TIẾP THEO

Bằng chứng điều tra đã xác định chính xác và đầy đủ nguồn gốc của 73 trường hợp màn hình trắng. Tiến hành thực thi Giai đoạn 3: sửa đổi mã nguồn, cập nhật view, viết test kiểm chứng tự động và quét lại toàn bộ hệ thống để đảm bảo 0% màn hình trắng.
