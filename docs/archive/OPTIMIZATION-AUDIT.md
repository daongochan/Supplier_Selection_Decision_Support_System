<!-- ARCHIVE_HEADER -->
> [!NOTE]
> **ARCHIVED DOCUMENT — SUPERSEDED**
> **Archived Date:** 2026-10-24  
> **Base Commit:** \d5b98db2b8b732bf0ce5491793aa73ba9136b678\  
> **Status:** Historical point-in-time document preserved for traceability. The active canonical documentation is located in \docs/\ (\DESIGN-SYSTEM.md\, \ARCHITECTURE.md\, \ALGORITHMS.md\, \WORKFLOW-AND-PERMISSIONS.md\, \docs/qa/\).

---

# BÁO CÁO KIỂM TRA TOÀN DIỆN HỆ THỐNG (OPTIMIZATION AUDIT)
**Dự án:** SupplierSelectionDSS – Decision Support System (AHP & TOPSIS)  
**Vai trò:** Senior Software Architect & Lead Developer  
**Ngày thực hiện:** 19/09/2026  
**Nền tảng:** ASP.NET Core MVC (.NET 10), Entity Framework Core 10, SQL Server  

---

## MỤC LỤC
1. [Phát hiện về Kiến trúc (Architecture)](#1-phát-hiện-về-kiến-trúc)
2. [Phát hiện về Chất lượng Code (Code Quality)](#2-phát-hiện-về-chất-lượng-code)
3. [Phát hiện về Bảo mật & Phân quyền (Security & Authorization)](#3-phát-hiện-về-bảo-mật--phân-quyền)
4. [Phát hiện về Database & Data Access](#4-phát-hiện-về-database--data-access)
5. [Phát hiện về Thuật toán AHP](#5-phát-hiện-về-thuật-toán-ahp)
6. [Phát hiện về Thuật toán TOPSIS](#6-phát-hiện-về-thuật-toán-topsis)
7. [Phát hiện về Tính toàn vẹn Workflow (Workflow Integrity)](#7-phát-hiện-về-tính-toàn-vẹn-workflow)
8. [Phát hiện về Giao diện & Trải nghiệm (UI/UX)](#8-phát-hiện-về-giao-diện--trải-nghiệm-uiux)
9. [Phát hiện về Hiệu năng (Performance)](#9-phát-hiện-về-hiệu-năng)
10. [Nợ kỹ thuật (Technical Debt)](#10-nợ-kỹ-thuật-technical-debt)
11. [Mã nguồn Legacy & Trùng lặp (Legacy & Duplication)](#11-mã-nguồn-legacy--trùng-lặp)
12. [Thứ tự ưu tiên xử lý (Action Priority)](#12-thứ-tự-ưu-tiên-xử-lý)

---

## 1. PHÁT HIỆN VỀ KIẾN TRÚC

### Issue ARC-01: Cấu trúc thư mục đơn phẳng, thiếu phân tách rõ giữa Domain, Application, Infrastructure và Web
- **File:** Toàn bộ root project (`/Controllers`, `/Services`, `/Models`, `/Data`, `/Constants`)
- **Vị trí tương đối:** Thư mục gốc project
- **Vấn đề:** Dự án định hướng kiến trúc phân tầng nhưng hiện tại toàn bộ service (tính toán, nghiệp vụ, xuất báo cáo, xác thực) nằm chung trong thư mục phẳng `/Services`. Models/DTO/ViewModels cùng nằm trong `/Models`. Các quy tắc nghiệp vụ (Domain rules) bị rải rác giữa Controller và View.
- **Ảnh hưởng:** Khó bảo trì, vi phạm nguyên lý Single Responsibility Principle (SRP), khó viết unit test độc lập cho từng use-case.
- **Giải pháp đề xuất:** Tổ chức lại namespace/thư mục nội bộ trong cùng 1 project (không tách đa project):
  - `Domain/`: Chứa Enums, Rules (`EvaluationBatchStateRules.cs`), Constants.
  - `Application/`: Chứa `Algorithms/` (AHP, TOPSIS độc lập), `Services/` (Application use cases), `DTOs/`.
  - `Infrastructure/`: Chứa `Persistence/` (Data/Entities), `Exports/` (Excel, PDF export tách khỏi service), `Audit/`.
  - `Web/`: Chứa `Controllers/`, `ViewModels/`, `Authorization/` (Policies), `Extensions/`.

### Issue ARC-02: Test framework dependencies bị tham chiếu trực tiếp trong Production csproj
- **File:** `SupplierSelectionDSS.csproj`
- **Vị trí tương đối:** Dòng 12, dòng 22
- **Vấn đề:** Gói `FluentAssertions` (v8.10.0) và `Moq` (v4.20.72) được khai báo trực tiếp trong production web project thay vì chỉ nằm trong `SupplierSelectionDSS.Tests.csproj`.
- **Ảnh hưởng:** Làm phình kích thước assembly production, vi phạm ranh giới giữa test tool và runtime production.
- **Giải pháp đề xuất:** Chuyển `FluentAssertions` và `Moq` sang `SupplierSelectionDSS.Tests.csproj`, gỡ bỏ khỏi `SupplierSelectionDSS.csproj`.

### Issue ARC-03: `ReportService` là một "God Class" vi phạm nghiêm trọng SRP (1,799 dòng)
- **File:** `Services/ReportService.cs`
- **Vị trí tương đối:** Toàn bộ file (1,799 dòng)
- **Vấn đề:** `ReportService` vừa truy vấn database, vừa format dữ liệu nghiệp vụ, vừa dựng layout PDF chi tiết qua QuestPDF (hàng trăm dòng DSL), vừa render bảng tính Excel qua ClosedXML (hàng trăm dòng định dạng cell), vừa quản lý file vật lý I/O trên đĩa, vừa ghi audit log.
- **Ảnh hưởng:** Cực kỳ khó bảo trì, rủi ro cao khi thay đổi định dạng báo cáo, không thể unit test logic sinh báo cáo mà không kéo theo dependencies đồ họa/IO.
- **Giải pháp đề xuất:** Tách thành các thành phần chuyên biệt:
  - `IReportExporter` và hai adapter: `QuestPdfReportExporter.cs` và `ClosedXmlReportExporter.cs` đặt trong `Infrastructure/Exports/`.
  - `ReportService` đóng vai trò Orchestrator: tập hợp dữ liệu DSS, gọi exporter tương ứng và lưu trữ record.

### Issue ARC-04: `AccountController` truy cập trực tiếp `ApplicationDbContext` bỏ qua Application Service
- **File:** `Controllers/AccountController.cs`
- **Vị trí tương đối:** Dòng 45–47 (truy vấn User), dòng 119–121 (cập nhật LastLoginAt)
- **Vấn đề:** Controller đăng nhập inject và gọi trực tiếp `ApplicationDbContext` để truy vấn và cập nhật `LastLoginAt`, trong khi dự án đã có `UserService`.
- **Ảnh hưởng:** Bất nhất kiến trúc; logic xác thực tài khoản bị phân tán giữa Controller và Service.
- **Giải pháp đề xuất:** Bổ sung phương thức `AuthenticateAsync(username, password)` vào `UserService` hoặc `AuthenticationService`, Controller chỉ điều hướng HTTP và thiết lập Cookie Claims.

### Issue ARC-05: Entity Leakage vào tầng Presentation
- **File:** `Controllers/EvaluationBatchController.cs` và `Views/EvaluationBatch/Details.cshtml`
- **Vị trí tương đối:** `EvaluationBatchController.cs` dòng 475–490; `Details.cshtml` dòng 1
- **Vấn đề:** `EvaluationBatchController.Details` trả thẳng entity `EvaluationBatch` kèm 10 navigation collections (`Suppliers`, `Criteria`, `EvaluationScores`, `Ahpresults`, `TopsisResults`, `Approvals`...) vào Razor View `@model EvaluationBatch`. View tự tính toán tiến độ workflow, đếm collection, parse trạng thái.
- **Ảnh hưởng:** Rò rỉ cấu trúc dữ liệu Entity Framework vào Presentation, vi phạm tính độc lập tầng, View bị phụ thuộc vào DbContext tracking behavior.
- **Giải pháp đề xuất:** Tạo `EvaluationBatchDetailViewModel` chứa sẵn các thông số tổng hợp (Counts, WorkflowSteps, Flags), mapping sạch tại Service trước khi truyền sang View.

---

## 2. PHÁT HIỆN VỀ CHẤT LƯỢNG CODE

### Issue COD-01: Unit test trong test suite đang bị FAIL do chuỗi so sánh sai
- **File:** `SupplierSelectionDSS.Tests/Calculation/TOPSISCalculationServiceTests.cs`
- **Vị trí tương đối:** Dòng 54–63 (`Calculate_InvalidCriteriaType_Throws`)
- **Vấn đề:** Test assert lỗi bằng pattern `.WithMessage("*Benefit hoặc Cost*")`, trong khi exception ném ra thực tế trong `TOPSISCalculationService.cs` (dòng 462) là: `"CriteriaType 'InvalidType' không hợp lệ. Chỉ hỗ trợ 'Benefit' hoặc 'Cost'."`. Do có dấu nháy đơn `'` xen giữa, pattern wildcard không khớp và test bị FAIL khi chạy `dotnet test`.
- **Ảnh hưởng:** CI/CD pipeline thất bại, suite test đỏ (13 Passed, 1 Failed).
- **Giải pháp đề xuất:** Chuẩn hóa thông điệp exception hoặc điều chỉnh wildcard pattern `.WithMessage("*Benefit*Cost*")` để test vượt qua chính xác.

### Issue COD-02: Vi phạm quy chuẩn C# "Mỗi class một file" trên diện rộng
- **File:** 
  - `Services/AHPCalculationService.cs` (dòng 395–410: `AHPCalculationResult`)
  - `Services/TOPSISCalculationService.cs` (dòng 515–547: `TOPSISCalculationResult`, `TOPSISSupplierDistance`)
  - `Models/TOPSISViewModel.cs` (dòng 66–143: 4 classes phụ)
  - `Models/DecisionResultViewModel.cs` (dòng 52–67: `DecisionRankingViewModel`)
  - `Models/AHPComparisonViewModel.cs` (dòng 21–29: `AHPComparisonCriteriaViewModel`)
  - `Models/AHPComparisonInputViewModel.cs` (dòng 12–19: `AHPComparisonInputItemViewModel`)
  - `Models/AHPResultViewModel.cs` (dòng 29–41: `AHPWeightViewModel`)
  - `Models/AuditLogViewModels.cs` (dòng 15–30: nhiều ViewModels)
- **Vấn đề:** Nhiều class/DTO được gom chung vào cuối file cùng class chính.
- **Ảnh hưởng:** Khó tìm kiếm file, vi phạm Coding Convention mục 27 của dự án ("Mỗi class một file").
- **Giải pháp đề xuất:** Tách từng class/record ra file `.cs` riêng biệt với đúng tên class.

### Issue COD-03: Gọi `.ToString()` thừa trên hằng số kiểu string
- **File:** `Services/EvaluationService.cs` (dòng 161); `Services/TOPSISService.cs` (dòng 338)
- **Vị trí tương đối:** Điểm so sánh `batch.Status` với `BatchStatus.Evaluating.ToString()`
- **Vấn đề:** `BatchStatus.Evaluating` đã là `public const string Evaluating = "Evaluating"`. Việc gọi `.ToString()` là thừa thãi.
- **Ảnh hưởng:** Giảm độ trau chuốt và tính chuyên nghiệp của code.
- **Giải pháp đề xuất:** Bỏ `.ToString()`, so sánh trực tiếp với `BatchStatus.Evaluating`.

### Issue COD-04: File code-behind Razor Page mồ côi (`_Pagination.cshtml.cs`)
- **File:** `Views/Shared/_Pagination.cshtml.cs`
- **Vị trí tương đối:** Toàn bộ file
- **Vấn đề:** Tồn tại file `_PaginationModel : PageModel` bên cạnh partial view `_Pagination.cshtml` trong một project MVC thuần túy (không dùng Razor Pages).
- **Ảnh hưởng:** Code rác do scaffolding tạo nhầm, gây hiểu nhầm về kiến trúc.
- **Giải pháp đề xuất:** Xóa bỏ file `_Pagination.cshtml.cs`.

---

## 3. PHÁT HIỆN VỀ BẢO MẬT & PHÂN QUYỀN

### Issue SEC-01: Hardcode role string phân tán, thiếu tập trung Policy-based Authorization
- **File:** `Controllers/TOPSISController.cs` (dòng 11), `Controllers/DecisionResultController.cs` (dòng 117, 294, 436), `Views/Shared/_Sidebar.cshtml`
- **Vị trí tương đối:** Các attribute `[Authorize(Roles = "...")]` và kiểm tra `@User.IsInRole("...")`
- **Vấn đề:** 
  - `TOPSISController.cs`: `[Authorize(Roles = "Administrator,Expert")]` dùng chuỗi tĩnh tự gõ thay vì dùng hằng số `UserRole`.
  - `DecisionResultController.cs`: Nối chuỗi thủ công `UserRole.Administrator + "," + UserRole.Manager`.
  - Sidebar và Views kiểm tra `@User.IsInRole("...")` rải rác hơn 20 vị trí.
- **Ảnh hưởng:** Khó kiểm soát ma trận quyền hạn, dễ phát sinh lỗ hổng phân quyền (privilege escalation) khi thay đổi role.
- **Giải pháp đề xuất:** Định nghĩa `PolicyNames` (ví dụ: `PolicyNames.CanEvaluate`, `PolicyNames.CanManageBatch`, `PolicyNames.CanApproveDecision`) và đăng ký Authorization Policies trong `Program.cs`. Sử dụng `[Authorize(Policy = ...)]`.

### Issue SEC-02: Lặp logic trích xuất Claim `NameIdentifier` ở 11 action methods
- **File:** `UserController`, `TOPSISController`, `ReportController`, `EvaluationController`, `EvaluationBatchController`, `DecisionResultController`, `AHPController`
- **Vị trí tương đối:** Các method POST nhạy cảm
- **Vấn đề:** Code lặp 11 lần đoạn:
  ```csharp
  var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
  if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId) || userId <= 0)
      return Unauthorized();
  ```
- **Ảnh hưởng:** Dễ quên kiểm tra hoặc xử lý không đồng nhất; làm phình Controller.
- **Giải pháp đề xuất:** Viết extension method `User.GetUserId()` trong `Web/Extensions/ClaimsPrincipalExtensions.cs`.

### Issue SEC-03: Sử dụng Action Type không chuẩn trong Audit Log
- **File:** `Controllers/ReportController.cs`
- **Vị trí tương đối:** Dòng 171–173, dòng 209
- **Vấn đề:** Controller tự phát minh action string `"Download PDF"` và `"Download Excel"` không có trong `Constants/AuditActions.cs` hay `AuditLogService.Actions`.
- **Ảnh hưởng:** Bất nhất dữ liệu Audit Log, không thể filter chuẩn xác theo danh mục hành động.
- **Giải pháp đề xuất:** Bổ sung `AuditActions.DownloadReportPdf` và `AuditActions.DownloadReportExcel` vào `Constants/AuditActions.cs` và chuẩn hóa.

---

## 4. PHÁT HIỆN VỀ DATABASE & DATA ACCESS

### Issue DAT-01: Truy vấn Over-fetching quá lớn tại `EvaluationBatchService.GetDetailAsync`
- **File:** `Services/EvaluationBatchService.cs`
- **Vị trí tương đối:** Dòng 660–720
- **Vấn đề:** Truy vấn sử dụng 10 lệnh `.Include()` lồng nhau (`Suppliers`, `Criteria`, `EvaluationScores`, `AhppairwiseComparisons`, `Ahpresult`, `TopsisResults`, `Approvals`...) kèm `.AsSplitQuery()` để kéo toàn bộ entity graph về bộ nhớ chỉ để hiển thị trang chi tiết đợt đánh giá.
- **Ảnh hưởng:** Tạo ra 10 câu lệnh SQL truy vấn đồng thời, tiêu tốn băng thông database và RAM của web server khi dữ liệu so sánh và điểm đánh giá tăng lên.
- **Giải pháp đề xuất:** Viết query chiếu (projection) thẳng vào ViewModel chỉ lấy các trường cần thiết (thông tin Batch, danh sách tên/mã Supplier, Criteria, số lượng điểm hiện có, trạng thái AHP/TOPSIS/Approval).

### Issue DAT-02: Nhiều truy vấn tuần tự rời rạc tại `DashboardService`
- **File:** `Services/DashboardService.cs`
- **Vị trí tương đối:** Dòng 30–180
- **Vấn đề:** `GetDashboardAsync()` thực hiện 5 câu truy vấn database riêng biệt liên tiếp: Supplier count/active, Criteria count/active, Batch counts by status, RecentBatches (top 8), LatestTopsisResults.
- **Ảnh hưởng:** Làm tăng số lượng round-trip đến SQL Server.
- **Giải pháp đề xuất:** Giữ `AsNoTracking()`, tối ưu gom các chỉ số đếm hoặc thực hiện song song khi cần, đồng thời loại bỏ query lặp.

### Issue DAT-03: Mapping trực tiếp EF Entity trong Controller
- **File:** `Controllers/SupplierController.cs` (dòng 143–175), `Controllers/CriteriaController.cs` (dòng 63–70)
- **Vị trí tương đối:** Action POST Create và Edit
- **Vấn đề:** Controller tự khởi tạo `new Supplier`, `new Criterion` và gán từng thuộc tính từ ViewModel trước khi đẩy xuống Service.
- **Ảnh hưởng:** Phá vỡ ranh giới trách nhiệm; logic chuyển đổi model thuộc về Application Service/Mapping layer.
- **Giải pháp đề xuất:** Truyền trực tiếp Form ViewModel/DTO vào Service method `CreateAsync(SupplierFormViewModel model)`.

---

## 5. PHÁT HIỆN VỀ THUẬT TOÁN AHP

### Issue AHP-01: Giới hạn Random Index (RI) bị thắt chặt ở tối đa 10 tiêu chí
- **File:** `Services/AHPCalculationService.cs`
- **Vị trí tương đối:** Dòng 53–66, dòng 364–373
- **Vấn đề:** Bảng Saaty Random Index chỉ định nghĩa từ n=1 đến n=10. Khi n > 10, hệ thống ném exception `InvalidOperationException("AHP hiện hỗ trợ tối đa 10 Criteria...")`. Mặc dù bài toán lựa chọn nhà cung cấp ít khi vượt quá 10-15 tiêu chí, chuẩn Saaty cung cấp giá trị RI lên tới n=15 (1.59).
- **Ảnh hưởng:** Giới hạn khả năng mở rộng nếu đợt đánh giá có trên 10 tiêu chí con.
- **Giải pháp đề xuất:** Bổ sung giá trị chuẩn Saaty RI cho n từ 11 đến 15 (11: 1.51, 12: 1.54, 13: 1.56, 14: 1.57, 15: 1.59).

### Issue AHP-02: Khi AHP thay đổi hoặc tính lại, TOPSIS cũ không được cascade vô hiệu hóa
- **File:** `Services/AHPService.cs`
- **Vị trí tương đối:** Dòng 201–215, dòng 314–339, dòng 455–483
- **Vấn đề:** Khi chuyên gia lưu lại ma trận so sánh cặp hoặc bấm Tính toán lại AHP, hệ thống xóa `Ahpresults` và `AhpcriteriaWeights` cũ, nhưng **KHÔNG xóa hoặc vô hiệu hóa `TopsisResults` và `TopsisIdealSolutions` đã lưu trước đó**.
- **Ảnh hưởng:** Vi phạm tính toàn vẹn workflow nghiêm trọng (Phase 6 & Phase 14): Kết quả TOPSIS cũ (được tính trên trọng số cũ) vẫn tồn tại và hiển thị song song với trọng số AHP mới tính, dẫn đến sai lệch kết quả xếp hạng và quyết định phê duyệt.
- **Giải pháp đề xuất:** Khi tính lại hoặc cập nhật AHP comparisons/weights, phải cascade xóa `TopsisResults` và `TopsisIdealSolutions` của Batch đó để người dùng bắt buộc phải chạy lại TOPSIS với trọng số mới.

---

## 6. PHÁT HIỆN VỀ THUẬT TOÁN TOPSIS

### Issue TOP-01: Lưu điểm đánh giá mới không cascade vô hiệu hóa kết quả TOPSIS
- **File:** `Services/EvaluationService.cs`
- **Vị trí tương đối:** Dòng 139–343 (`SaveAsync`)
- **Vấn đề:** Khi nhân sự cập nhật điểm đánh giá của nhà cung cấp (`EvaluationScores`), hệ thống lưu điểm mới nhưng không xóa hoặc đánh dấu stale đối với `TopsisResults` và `TopsisIdealSolutions`.
- **Ảnh hưởng:** Bảng xếp hạng TOPSIS vẫn giữ nguyên kết quả của ma trận quyết định cũ, dẫn đến việc báo cáo và duyệt chọn nhà cung cấp dựa trên dữ liệu lỗi thời.
- **Giải pháp đề xuất:** Trong `EvaluationService.SaveAsync`, nếu có kết quả TOPSIS cũ cho Batch, tự động dọn dẹp các kết quả TOPSIS cũ và thông báo người dùng tính toán lại.

### Issue TOP-02: Engine TOPSIS gom nhiều cấu trúc kết quả trong cùng file
- **File:** `Services/TOPSISCalculationService.cs`
- **Vị trí tương đối:** Dòng 515–547
- **Vấn đề:** `TOPSISCalculationResult` và `TOPSISSupplierDistance` nằm chung file với thuật toán.
- **Ảnh hưởng:** Vi phạm chuẩn C# của dự án.
- **Giải pháp đề xuất:** Tách ra các file riêng trong thư mục `Application/Algorithms/TOPSIS/`.

---

## 7. PHÁT HIỆN VỀ TÍNH TOÀN VẸN WORKFLOW

### Issue WFL-01: Thiếu hoàn toàn tính năng và method "Cancel Batch"
- **File:** `Services/EvaluationBatchService.cs`, `Controllers/EvaluationBatchController.cs`, `Data/Entities/EvaluationBatch.cs`
- **Vị trí tương đối:** Nghiệp vụ Batch lifecycle
- **Vấn đề:** Entity `EvaluationBatch` có thuộc tính `CancelledAt`, `BatchStatus.Cancelled` đã được định nghĩa, UI có style `.status-cancelled`, nhưng trong `EvaluationBatchService` và `EvaluationBatchController` **hoàn toàn không có method nào để Cancel một Batch**.
- **Ảnh hưởng:** Không thực hiện được luồng nghiệp vụ `Draft -> Cancelled` và `Evaluating -> Cancelled` như mô tả trong Phase 7 và Phase 10 của tài liệu nghiệp vụ. Người dùng không thể hủy một đợt đánh giá bị đình trệ.
- **Giải pháp đề xuất:** Bổ sung `CancelAsync(int batchId, int cancelledBy, string? reason)` vào `EvaluationBatchService` và action `Cancel` vào `EvaluationBatchController` với confirmation modal.

### Issue WFL-02: Quy tắc chuyển trạng thái Batch bị phân tán, thiếu `EvaluationBatchStateRules.cs`
- **File:** Rải rác tại `EvaluationBatchController`, `EvaluationController`, `AHPController`, `TOPSISController`, `DecisionResultController`, `EvaluationBatch/Details.cshtml`
- **Vị trí tương đối:** Khắp các tầng
- **Vấn đề:** Mỗi nơi tự kiểm tra điều kiện chuyển trạng thái bằng các câu lệnh `if (batch.Status != ...)` riêng rẽ. Không có class `EvaluationBatchStateRules.cs` tập trung như tài liệu yêu cầu tại Mục 10.
- **Ảnh hưởng:** Dễ bỏ sót điều kiện chặn; logic bất nhất giữa frontend (Razor view) và backend (Controller/Service).
- **Giải pháp đề xuất:** Xây dựng `Domain/Rules/EvaluationBatchStateRules.cs` quản lý tập trung toàn bộ ma trận chuyển đổi trạng thái:
  - `CanStart(EvaluationBatch batch)`
  - `CanEvaluate(EvaluationBatch batch)`
  - `CanPerformAHP(EvaluationBatch batch, bool evaluationComplete)`
  - `CanPerformTOPSIS(EvaluationBatch batch, bool ahpConsistent)`
  - `CanApprove(EvaluationBatch batch, bool topsisComplete)`
  - `CanCancel(EvaluationBatch batch)`
  - `CanEditScope(EvaluationBatch batch)`

---

## 8. PHÁT HIỆN VỀ GIAO DIỆN & TRẢI NGHIỆM (UI/UX)

### Issue UI-01: Thiếu Breadcrumb và Footer trong Application Shell
- **File:** `Views/Shared/_Layout.cshtml`, `Views/Shared/_Topbar.cshtml`
- **Vị trí tương đối:** Shell cấu trúc
- **Vấn đề:** Theo yêu cầu tại Mục 17: Application shell bắt buộc phải có `Sidebar + Topbar + Breadcrumb + Content Area + Footer`. Hiện tại `_Layout.cshtml` chỉ có `Sidebar + Topbar + Main`, hoàn toàn thiếu Breadcrumb điều hướng phân cấp và Footer hệ thống DSS.
- **Ảnh hưởng:** Trải nghiệm người dùng giảm tính định vị ngữ cảnh (đặc biệt khi đang làm việc sâu trong workflow Batch -> AHP / TOPSIS -> Result).
- **Giải pháp đề xuất:** Thêm Breadcrumb động vào `_Layout.cshtml` / `_Topbar.cshtml` và Footer chuẩn Enterprise DSS.

### Issue UI-02: `Views/Home/Index.cshtml` và `Views/Dashboard/Index.cshtml` trùng lặp 100%
- **File:** `Views/Home/Index.cshtml` (449 dòng) và `Views/Dashboard/Index.cshtml` (450 dòng)
- **Vị trí tương đối:** Toàn bộ file
- **Vấn đề:** Hai file View này hoàn toàn giống nhau từng dòng HTML, CSS class, logic Razor. `HomeController.Index` và `DashboardController.Index` cùng gọi `_dashboardService.GetDashboardAsync()`.
- **Ảnh hưởng:** Lãng phí tài nguyên, dư thừa code, khi cần sửa đổi dashboard phải sửa 2 nơi, dễ lệch pha.
- **Giải pháp đề xuất:** Giữ `DashboardController.Index` và `Views/Dashboard/Index.cshtml` làm trang chính. `HomeController.Index` chỉ làm nhiệm vụ redirect hợp lệ sang `Dashboard` hoặc `Account/Login`. Xóa file view trùng lặp `Views/Home/Index.cshtml`.

### Issue UI-03: Trải nghiệm thông báo TempData bị phân tán và không có Toast System
- **File:** Hầu hết các file trong `Views/`
- **Vị trí tương đối:** Phần đầu mỗi view
- **Vấn đề:** 
  - `Views/AHP/Index.cshtml` kiểm tra cả 4 keys: `TempData["Success"]`, `TempData["SuccessMessage"]`, `TempData["Error"]`, `TempData["ErrorMessage"]`.
  - Mọi View đều tự nhúng markup HTML alert (`<div class="alert-success">...</div>`) gây lặp mã khắp nơi.
  - Chưa có hệ thống Toast notification tự động hiển thị góc màn hình theo chuẩn Enterprise DSS.
- **Ảnh hưởng:** Giao diện bị giật cục mỗi lần reload trang, mã nguồn View bị rác.
- **Giải pháp đề xuất:** Tạo Toast component tập trung tại `_Layout.cshtml`, chuẩn hóa 2 key duy nhất: `TempData["SuccessMessage"]` và `TempData["ErrorMessage"]`. Kèm JavaScript tự tắt sau 4s.

### Issue UI-04: Thiếu Confirmation Modals cho các thao tác nhạy cảm
- **File:** 
  - `Views/EvaluationBatch/Details.cshtml` (dòng 618: dùng `onclick="return confirm(...)""`)
  - `Views/DecisionResult/Index.cshtml` (dòng 755–818: Approve, Reject, Reevaluate bấm là submit ngay)
  - `Views/Supplier/Index.cshtml`, `Views/Criteria/Index.cshtml`, `Views/ProductGroup/Index.cshtml`: ToggleStatus submit không có xác nhận
- **Vấn đề:** Thao tác quan trọng (Phê duyệt nhà cung cấp, Từ chối, Yêu cầu đánh giá lại, Khóa/Mở khóa) không có popup xác nhận chuyên nghiệp, không có chỗ cho Manager nhập ghi chú (Notes/Reason).
- **Ảnh hưởng:** Người dùng dễ bấm nhầm gây hậu quả nghiêm trọng (ví dụ xóa sạch dữ liệu AHP/TOPSIS khi bấm nhầm Re-evaluate); vi phạm yêu cầu Phase 19.
- **Giải pháp đề xuất:** Xây dựng Modal component chuẩn Bootstrap/Vanilla CSS cho: Confirmation khi Approve (kèm input Notes), Reject (kèm input lý do), Reevaluate (cảnh báo xóa dữ liệu tính toán), Cancel Batch, ToggleStatus.

### Issue UI-05: Thiếu Loading State & Chống Duplicate Submit trên các nút xử lý tính toán
- **File:** Các view `Evaluation/Index.cshtml`, `AHP/Index.cshtml`, `TOPSIS/Index.cshtml`, `Report/Index.cshtml`
- **Vị trí tương đối:** Nút Save, Calculate AHP, Calculate TOPSIS, Generate Report
- **Vấn đề:** Khi bấm Calculate AHP hoặc TOPSIS hoặc Export PDF, nút bấm không hiển thị spinner loading, không disable để chống bấm liên tiếp (double click submit).
- **Ảnh hưởng:** Người dùng có thể click nhiều lần khiến server phải chạy thuật toán song song hoặc ném lỗi xung đột dữ liệu (concurrency conflict).
- **Giải pháp đề xuất:** Bổ sung script trong `site.js` tự động gắn spinner và disable button khi submit form tính toán.

---

## 9. PHÁT HIỆN VỀ HIỆU NĂNG

### Issue PRF-01: CSS tải tuần tự qua 15 lệnh `@import` trong `site.css`
- **File:** `wwwroot/css/site.css`
- **Vị trí tương đối:** Dòng 7–39
- **Vấn đề:** File `site.css` dùng 15 lệnh `@import url(...)` để load tokens, base, components và từng page stylesheet.
- **Ảnh hưởng:** Trình duyệt phải thực hiện các request tuần tự (CSS waterfall) làm chậm First Contentful Paint (FCP).
- **Giải pháp đề xuất:** Gom các file base/components cốt lõi vào bundle chung hoặc liên kết link stylesheet tối ưu.

---

## 10. NỢ KỸ THUẬT (TECHNICAL DEBT)

### Issue DEB-01: File scaffolding boilerplate chưa dọn dẹp
- **File:** `Views/Home/Privacy.cshtml`, `Views/Shared/_Layout.cshtml.css`, `Views/Shared/_Pagination.cshtml.cs`
- **Vị trí tương đối:** Thư mục Views
- **Vấn đề:** Các file mặc định được sinh ra từ template dotnet new không có giá trị nghiệp vụ hoặc xung đột với design system hiện tại.
- **Giải pháp đề xuất:** Loại bỏ sạch sẽ các file boilerplate không sử dụng.

### Issue DEB-02: Test Coverage cho AHP, TOPSIS và Workflow Rules còn rất mỏng
- **File:** Thư mục `SupplierSelectionDSS.Tests`
- **Vị trí tương đối:** Chỉ có 2 file test tính toán
- **Vấn đề:** 
  - Hoàn toàn chưa có unit test cho Batch State transitions (`EvaluationBatchStateRules`).
  - Chưa có test cho các trường hợp biên của TOPSIS (toàn bộ tiêu chí Cost, tiêu chí hỗn hợp, trường hợp điểm số bằng nhau dẫn đến mẫu số = 0).
  - Chưa có test cho AHP reciprocity validation và consistency check.
- **Giải pháp đề xuất:** Mở rộng bộ unit tests cho Calculation Engines và Workflow Rules.

---

## 11. MÃ NGUỒN LEGACY & TRÙNG LẶP

### Issue DUP-01: Trùng lặp phương thức `Normalize()` tại 4 Controllers
- **File:** `CriteriaController.cs`, `ProductGroupController.cs`, `SupplierController.cs`, `UserController.cs`
- **Vị trí tương đối:** Cuối mỗi file controller
- **Vấn đề:** Mỗi controller đều viết một private static method `Normalize(...)` để lặp qua từng property string và `.Trim()`.
- **Giải pháp đề xuất:** Viết helper hoặc model binder / extension method chuẩn hóa chuỗi đầu vào.

### Issue DUP-02: Trùng lặp khai báo Action Constants giữa `AuditActions` và `AuditLogService.Actions`
- **File:** `Constants/AuditActions.cs` và `Services/AuditLogService.cs`
- **Vị trí tương đối:** `AuditLogService.cs` dòng 18–42
- **Vấn đề:** `AuditLogService.cs` tạo subclass `Actions` gán lại từng hằng số của `AuditActions`. Nơi thì dùng `AuditLogService.Actions.SaveAHP`, nơi dùng `AuditActions.SaveAHP`.
- **Giải pháp đề xuất:** Dùng thống nhất `SupplierSelectionDSS.Constants.AuditActions`.

---

## 12. THỨ TỰ ƯU TIÊN XỬ LÝ

| Ưu tiên | Mã Issue | Mô tả ngắn gọn | Mục tiêu |
| :--- | :--- | :--- | :--- |
| **P0 (Khẩn cấp)** | `COD-01` | Sửa unit test bị fail trong `TOPSISCalculationServiceTests` | Khôi phục build/test xanh |
| **P0 (Khẩn cấp)** | `ARC-02` | Gỡ `FluentAssertions`, `Moq` khỏi production `.csproj` | Làm sạch dependency sản phẩm |
| **P1 (Cốt lõi)** | `AHP-02`, `TOP-01` | Cascade vô hiệu hóa TOPSIS khi AHP hoặc Evaluation scores thay đổi | Đảm bảo tính đúng đắn DSS |
| **P1 (Cốt lõi)** | `WFL-01`, `WFL-02` | Xây dựng `Domain/Rules/EvaluationBatchStateRules.cs` và bổ sung Cancel Batch | Hoàn thiện toàn vẹn workflow |
| **P1 (Cấu trúc)** | `ARC-01`, `ARC-04`, `ARC-05` | Tổ chức lại cấu trúc thư mục sạch, tách DTO/ViewModel, loại bỏ Entity Leakage | Kiến trúc phân tầng rõ ràng |
| **P2 (Bảo mật/Giao diện)** | `SEC-01`, `SEC-02` | Policy-based Authorization và ClaimsPrincipal Extension | Loại bỏ magic strings & code lặp |
| **P2 (Giao diện)** | `UI-01`, `UI-03`, `UI-04`, `UI-05` | Breadcrumb, Footer, Toast system, Confirmation Modal, Button Loading | Nâng cấp Enterprise DSS UX |
| **P2 (Báo cáo)** | `ARC-03` | Refactor `ReportService` tách Exporter sang Infrastructure | Giảm độ phức tạp God Class |
| **P3 (Dọn dẹp)** | `UI-02`, `COD-04`, `DEB-01`, `DUP-01`, `DUP-02` | Xóa `Home/Index` trùng lặp, xóa file rác, gom Normalize | Codebase sạch 100% |
| **P3 (Kiểm thử)** | `DEB-02` | Mở rộng Unit Test Suite (AHP, TOPSIS, Workflow Rules) | Đảm bảo không hồi quy (regression-free) |
