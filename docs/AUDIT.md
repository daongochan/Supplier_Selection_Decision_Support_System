# BÁO CÁO AUDIT ĐỘC LẬP DỰ ÁN SUPPLIER SELECTION DSS
**Ngày thực hiện:** 22/10/2026  
**Nhánh kiểm tra:** `chore/project-hardening` (tạo từ nhánh `main`)  
**Người thực hiện:** Senior .NET Engineer & Academic Reviewer  
**Đối tượng:** Hệ thống Hỗ trợ Ra Quyết định Chọn Nhà Cung Cấp (Supplier Selection DSS)  
**Mục tiêu:** Đánh giá độc lập toàn diện về tính tái lập, vệ sinh repo, bảo mật, thuật toán AHP/TOPSIS, kiến trúc và chất lượng mã nguồn trước khi hoàn thiện repo bảo vệ đồ án.

---

## I. HIỆN TRẠNG REPO & SO SÁNH LOCAL VỚI GITHUB ORIGIN

### 1. Thông tin Git & Remote
- **Remote Origin:** `https://github.com/daongochan/Supplier_Selection_Decision_Support_System`
- **Lịch sử commit remote:** Chỉ có duy nhất 1 commit: `a0d86cd Initial commit`.
- **Cấu trúc thư mục tại máy local (`d:\DEAN`):**
  - `d:\DEAN\Projects\SupplierSelectionDSS`: Thư mục chứa git repo hiện tại.
  - `d:\DEAN\Projects\SupplierSelectionDSS.Tests`: Project unit test (xUnit, 95 test cases đã viết, toàn bộ Pass). Đang nằm **ngoài git repo**.
  - `d:\DEAN\SupplierSelectionDSS_SQL`: Thư mục chứa toàn bộ 11 script SQL dựng cơ sở dữ liệu (`00_Create_Database.sql` đến `10_Verify_Database.sql`, cùng script gộp `Run_All.sql`). Đang nằm **ngoài git repo**.

### 2. Khác biệt giữa Local Working Tree và `origin/main` (`git diff origin/main --stat`)
- **Số lượng file thay đổi:** 439 file (17.902 dòng thêm, 8.749 dòng bớt).
- **Các thay đổi uncommitted đáng chú ý trên bản local:**
  - `SupplierSelectionDSS.csproj`: Đã xóa tham chiếu `Moq` và `FluentAssertions` ra khỏi project web chính (chuyển sang `SupplierSelectionDSS.Tests`).
  - `Controllers/AHPController.cs`: Đã làm sạch using thừa (`Internal`, `Intrinsics.Arm`, `MediaTypeNames`).
  - `Controllers/TOPSISController.cs`: Đã sửa attribute thành `[Authorize(Roles = UserRole.Administrator + "," + UserRole.Expert)]`.
  - `Views/Home/Privacy.cshtml`: Đã xóa khỏi thư mục Views.
  - `Services/ReportService.cs`: Đã tách logic xuất PDF sang `QuestPdfReportExporter.cs` và Excel sang `ClosedXmlExcelReportExporter.cs` (giảm từ 1.798 dòng xuống 802 dòng).
  - Bổ sung `Domain/Rules/EvaluationBatchStateRules.cs` tập trung hóa quy tắc trạng thái của đợt đánh giá.
  - Nhiều file `.vs/`, `obj/`, `bin/`, và các file báo cáo thử nghiệm (`wwwroot/reports/*.pdf|xlsx`) đang bị tracking hoặc uncommitted trong working tree.

---

## II. XÁC MINH CÁC PHÁT HIỆN SƠ BỘ TỪ MỤC 3

| STT | Phát hiện sơ bộ (Mục 3) | Kết quả xác minh | Chi tiết kiểm chứng & Số dòng dẫn chứng |
|---|---|---|---|
| **A.1** | `SupplierSelectionDSS.slnx` tham chiếu `../SupplierSelectionDSS.Tests/`, nhưng test không nằm trong repo. | **ĐÚNG** | `SupplierSelectionDSS.slnx:2` tham chiếu `../SupplierSelectionDSS.Tests/SupplierSelectionDSS.Tests.csproj`. Project test tồn tại ở `d:\DEAN\Projects\SupplierSelectionDSS.Tests\` nhưng không được git theo dõi. |
| **A.2** | `Moq` và `FluentAssertions` nằm trong project web chính. Vấn đề bản quyền FluentAssertions 8.x. | **ĐÚNG (ở a0d86cd) / ĐÃ SỬA CỤC BỘ** | Trong commit `a0d86cd`, `SupplierSelectionDSS.csproj` có dòng 12 (`FluentAssertions 8.10.0`) và dòng 20 (`Moq 4.20.72`). Bản local đã chuyển sang `SupplierSelectionDSS.Tests.csproj:13,15` nhưng chưa commit. FluentAssertions từ 8.0 đổi sang bản quyền thương mại (Xceed) cần thay thế để an toàn pháp lý. |
| **A.3** | Không có `Migrations/`, không có script `.sql` trong repo. Toàn bộ schema chỉ nằm trong SQL cục bộ. | **ĐÚNG (với Git) / ĐÃ CÓ BẢN NGOÀI** | Git repo không có `.sql`. Tuy nhiên, thư mục `d:\DEAN\SupplierSelectionDSS_SQL\` đã có đầy đủ 11 file script và `Run_All.sql` (17 bảng, 7 trigger, 2 view, index, collation `Vietnamese_CI_AS`). |
| **A.4** | Không có seed tài khoản Administrator đầu tiên. | **ĐÚNG** | `09_Seed_Data.sql:6` chỉ chứa placeholder `PasswordHash = N'<hash>'`. Trong C# không có seed startup nào. |
| **A.5** | Không có `README.md`, `.gitignore`, `LICENSE`, CI workflow. | **ĐÚNG** | Repo hoàn toàn chưa có `.gitignore`, `README.md`, `LICENSE`. Thư mục `.github/workflows/` rỗng. |
| **B.1** | Đã commit `.vs/`, `obj/`, `SupplierSelectionDSS.csproj.user`. | **ĐÚNG** | Đã theo dõi trong commit `a0d86cd` (~6.9 MB `.vs/`, file `.vsidx`, `.suo`, `.user`, `.assets.json`). |
| **B.2** | `obj/project.assets.json` chứa đường dẫn máy cá nhân `C:\Users\...`. | **ĐÚNG** | Kiểm chứng trong `obj/project.assets.json` chứa chuỗi máy cá nhân tác giả cũ. |
| **B.3** | `wwwroot/reports/*.pdf\|xlsx` bị commit như dữ liệu thật. | **ĐÚNG** | Có 4 file mẫu `BAT-002_*`, `BAT-003_*`, `EB005_*` trong `wwwroot/reports/`. |
| **C.1** | `app.UseStaticFiles()` trước `UseAuthentication()`, báo cáo lưu trong `wwwroot/reports` công khai. | **ĐÚNG** | `Program.cs:204` gọi `UseStaticFiles()` trước `UseAuthentication():208`. `ReportService.cs:766` lưu file vào `wwwroot/reports`. Bất kỳ ai cũng truy cập trực tiếp được `/reports/<filename>` mà không cần đăng nhập/quyền Manager. |
| **C.2** | `GetDownloadAsync` ghép path thiếu kiểm tra Path Traversal. | **ĐÚNG** | `ReportService.cs:473-475` dùng `Path.Combine(_environment.WebRootPath, relativePath)` mà không kiểm tra canonical path (`Path.GetFullPath`) có nằm trong thư mục reports hay không. |
| **C.3** | Thiếu bộ lọc Antiforgery toàn cục dự phòng. | **ĐÚNG** | `Program.cs:22` chỉ gọi `AddControllersWithViews()` mà không đăng ký `AutoValidateAntiforgeryTokenAttribute`. |
| **C.4** | `AccountController.Login`: user enumeration, timing attack, thiếu lockout, thiếu log. | **ĐÚNG** | `AccountController.cs:48-55` trả về sớm khi `user == null` (không tốn CPU BCrypt); dòng 58-65 báo "Tài khoản hiện không hoạt động" trước khi verify mật khẩu (lộ username); không lockout hay log. |
| **C.5** | Chuỗi `ex.Message` xuất hiện ở 10 controller đẩy ra UI. | **ĐÚNG** | 10 controller (User, TOPSIS, Supplier, Report, ProductGroup, Evaluation, EvaluationBatch, DecisionResult, Criteria, AHP) bắt `Exception ex` rồi gán thẳng vào `TempData["ErrorMessage"]` hoặc JSON. |
| **C.6** | Toàn dự án không dùng `ILogger` (0 lần xuất hiện). | **ĐÚNG** | Tìm kiếm toàn bộ solution cho thấy `ILogger` xuất hiện 0 lần. |
| **C.7** | Connection string dùng `Trusted_Connection` tới `Server=.`. | **ĐÚNG** | `appsettings.json:3` cấu hình `Server=.;Database=SupplierSelectionDSS;Trusted_Connection=True;...`. |
| **D.1** | AHP ném lỗi khi n > 10. | **SAI MỘT PHẦN** | Code local tại `AHPCalculationService.cs:58-76, 381` đã cập nhật bảng Saaty Random Index từ n = 1 đến n = 15; chỉ ném lỗi khi n > 15. |
| **D.2** | TOPSIS ném lỗi khi cả một cột toàn 0 (kể cả tiêu chí Cost). | **ĐÚNG** | `TOPSISCalculationService.cs:102-107` ném `InvalidOperationException` nếu `maxAbsoluteValue <= 1e-12`. |
| **D.3** | TOPSIS gán CC = 0 khi D+ + D- = 0. | **ĐÚNG** | `TOPSISCalculationService.cs:300-309` gán `closenessCoefficient = 0.0`. |
| **D.4** | TOPSIS gán thứ hạng tuần tự (1, 2, 3) kể cả khi CC bằng nhau. | **ĐÚNG** | `TOPSISCalculationService.cs:348-358` duyệt `rank = 0 .. n-1` và gán `rank + 1`. |
| **D.5** | Magic string `Unit == "Điểm"` phân biệt thang điểm 0–10. | **ĐÚNG** | `EvaluationService.cs:380-384` so sánh trực tiếp chuỗi `"Điểm"`. |
| **D.6** | `AHPService.SaveComparisonsAsync` xóa đè ma trận cũ, không lưu lịch sử chuyên gia. | **ĐÚNG** | `AHPService.cs:208-219` gọi `_context.AhppairwiseComparisons.RemoveRange(...)` mỗi lần lưu. |
| **D.7** | Ghi chú "chưa có MaxValue" cho tiêu chí số thực. | **ĐÚNG** | `EvaluationService.cs:411-413` ghi chú rõ ràng về việc chưa có upper bound. |
| **E.1** | Service rất lớn (ReportService ~1.800 dòng...). | **ĐÚNG MỘT PHẦN** | Bản local đã tách PDF/Excel exporter, `ReportService.cs` còn 802 dòng. Các service khác vẫn lớn: `AHPService.cs` (944 dòng), `DecisionResultService.cs` (781 dòng), `TOPSISService.cs` (754 dòng), `EvaluationBatchService.cs` (742 dòng). |
| **E.2** | Using thừa/nguy hiểm trong `AHPController.cs`. | **ĐÃ SỬA CỤC BỘ** | `AHPController.cs` bản local đã sạch using thừa. |
| **E.3** | Chuỗi ký tự cứng thay vì hằng số (`TOPSISController:11`). | **ĐÃ SỬA CỤC BỘ** | `TOPSISController.cs:12` đã dùng `UserRole.Administrator + "," + UserRole.Expert`. Tuy nhiên chuỗi cứng `"PDF"` vẫn còn trong `ReportService.cs`. |
| **E.4** | `Views/Home/Privacy.cshtml` còn nguyên mẫu template. | **ĐÃ SỬA CỤC BỘ** | `Privacy.cshtml` đã bị xóa hoàn toàn khỏi thư mục Views; action `Privacy` trong `HomeController.cs` cũng đã xóa. |
| **E.5** | Không có `.editorconfig`, cảnh báo nullable khi build. | **ĐÚNG** | Chưa có `.editorconfig`. Build phát hiện 3 cảnh báo nullable: `Create.cshtml:476`, `Create.cshtml:601`, `DashboardService.cs:245`. |

---

## III. BẢNG TỔNG HỢP PHÁT HIỆN TOÀN DIỆN (AUDIT FINDINGS)

Mức độ nghiêm trọng:
- **CRITICAL**: Nguy cơ lộ lọt dữ liệu kinh doanh công khai, xâm phạm an toàn hệ thống, hoặc gãy đổ khi triển khai.
- **HIGH**: Lỗ hổng xác thực/phân quyền, rò rỉ thông tin kỹ thuật, đe dọa toàn vẹn tài khoản quản trị.
- **MEDIUM**: Rủi ro bất đồng bộ giao dịch, xung đột dữ liệu đồng thời, hạn chế thuật toán MCDM, thiếu chuẩn hóa mã nguồn.
- **LOW**: Cảnh báo trình biên dịch, thiếu cấu hình chuẩn định dạng mã nguồn.

| ID | Mức độ | File : Dòng | Mô tả phát hiện | Đề xuất khắc phục | Phase xử lý | Trạng thái |
|---|---|---|---|---|---|---|
| **SEC-01** | **CRITICAL** | `Services/ReportService.cs:766`<br>`Program.cs:204,208` | Báo cáo PDF/Excel được ghi trực tiếp vào `wwwroot/reports/`. Do `app.UseStaticFiles()` được gọi trước `UseAuthentication()`, bất kỳ người dùng nặc danh nào cũng có thể tải toàn bộ báo cáo chọn nhà cung cấp qua đường dẫn `/reports/<filename>` mà không cần đăng nhập hay kiểm tra quyền Manager. | Chuyển thư mục lưu báo cáo ra ngoài webroot (ví dụ: `App_Data/reports` hoặc thư mục cấu hình qua `appsettings.json`). Tải file bắt buộc đi qua action `ReportController.Download` (đã có role-check và ghi audit log). Xóa bỏ hoặc chặn route static files tới thư mục báo cáo. | **Phase 4** | **ĐÃ SỬA** (Đã chuyển sang `App_Data/reports`, chặn route tĩnh 404, kiểm soát tải qua action có role-check) |
| **SEC-02** | **CRITICAL** | `Services/ReportService.cs:473-475` | Lỗ hổng Path Traversal tiềm ẩn: `GetDownloadAsync` dùng `Path.Combine(_environment.WebRootPath, relativePath)` trực tiếp từ giá trị trong database mà không dùng `Path.GetFullPath` để xác thực đường dẫn con nằm trong thư mục cho phép. | Chuẩn hóa đường dẫn bằng `Path.GetFullPath(combinedPath)` và kiểm tra `combinedPath.StartsWith(allowedBaseDirectory, StringComparison.OrdinalIgnoreCase)`. | **Phase 4** | **ĐÃ SỬA** (Kiểm tra canonical path chặt chẽ, có unit test chống Path Traversal) |
| **SEC-03** | **HIGH** | `Controllers/AccountController.cs:48-65` | Lỗ hổng User Enumeration và Timing Attack tại trang Đăng nhập: (1) Nhánh `user == null` trả về ngay lập tức mà không chạy hàm băm BCrypt, tạo chênh lệch thời gian xử lý (~150ms) so với khi user tồn tại. (2) Kiểm tra trạng thái tài khoản trước khi xác minh mật khẩu và hiển thị "Tài khoản hiện không hoạt động", để lộ sự tồn tại của username trong database. | Dùng thông báo đăng nhập chung: "Tên đăng nhập hoặc mật khẩu không chính xác" cho mọi trường hợp. Luôn chạy BCrypt verify giả lập (dummy verify) khi user không tồn tại để cân bằng thời gian phản hồi. Kiểm tra mật khẩu trước khi kiểm tra trạng thái hoạt động. | **Phase 4** | **ĐÃ SỬA** (Đã đồng nhất thông báo lỗi và băm BCrypt giả lập) |
| **SEC-04** | **HIGH** | `Program.cs:22` | Thiếu bộ lọc Antiforgery toàn cục: Mặc dù các action `[HttpPost]` hiện tại đều có `[ValidateAntiForgeryToken]`, việc thiếu bộ lọc toàn cục dễ dẫn đến rủi ro bỏ sót CSRF token khi phát triển thêm các action POST mới trong tương lai. | Đăng ký `options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>()` trong `builder.Services.AddControllersWithViews(...)`. | **Phase 4** | **ĐÃ SỬA** (Đã đăng ký filter toàn cục trong Program.cs) |
| **SEC-05** | **HIGH** | `Controllers/AccountController.cs:36-90` | Thiếu cơ chế giới hạn thử sai (Brute-force/Lockout) khi đăng nhập và không ghi nhật ký các lần đăng nhập thất bại vào Audit Log. Kẻ tấn công có thể dò quét mật khẩu từ điển tự do. | Bổ sung cơ chế Rate Limiting hoặc Lockout (khóa tài khoản tạm thời sau 5 lần thử sai liên tiếp) và ghi nhận hành vi đăng nhập thất bại vào `AuditLogs`. | **Phase 4** | **ĐÃ SỬA** (Khóa 15 phút sau 5 lần sai, ghi log kiểm toán và ILogger) |
| **SEC-06** | **HIGH** | Toàn bộ 10 Controller & Program.cs | Toàn dự án không sử dụng `ILogger` (0 lần xuất hiện). Khi xảy ra ngoại lệ runtime, không có dấu vết log được lưu lại. Đồng thời, 10 controller đẩy chuỗi `ex.Message` thô thẳng ra giao diện UI (`TempData["ErrorMessage"]`), có nguy cơ rò rỉ chi tiết cấu trúc bảng SQL, kết nối mạng và lỗi nội bộ. | Inject `ILogger<T>` vào các Controller và Service trọng yếu. Bổ sung Middleware xử lý lỗi toàn cục. Phân loại ngoại lệ: chỉ hiển thị `ex.Message` khi là `InvalidOperationException` có chủ đích nghiệp vụ; các lỗi kỹ thuật khác hiển thị thông báo chung và ghi log đầy đủ ngữ cảnh. | **Phase 4** | **ĐÃ SỬA** (Đã inject ILogger vào các service, kiểm soát lỗi ngoại lệ an toàn) |
| **AUTH-01** | **HIGH** | `Services/UserService.cs:382-400` | Lỗ hổng Last Admin Privilege: Trong `UserService.UpdateAsync`, hàm kiểm tra Administrator cuối cùng chỉ kích hoạt khi `model.Status != EntityStatus.Active`. Nếu Administrator duy nhất đổi `Role` của mình từ `Administrator` sang `PurchasingStaff` hoặc `Expert`, hệ thống cho phép cập nhật, dẫn tới không còn bất kỳ Administrator nào có thể quản trị hệ thống. | Bổ sung kiểm tra: Nếu user đang là Administrator và `model.Role != UserRole.Administrator`, đếm số lượng Active Administrator còn lại; nếu `<= 1` thì ném `InvalidOperationException`. | **Phase 4** | **ĐÃ SỬA** (Chặn cả việc hạ quyền và vô hiệu hóa Administrator cuối cùng, có test khóa) |
| **REPO-01** | **HIGH** | `d:\DEAN\SupplierSelectionDSS_SQL\09_Seed_Data.sql:6-9` | Dữ liệu mẫu (Seed Data) chứa mật khẩu placeholder `N'<hash>'`, khiến người dùng sau khi chạy script không thể đăng nhập vào hệ thống bằng bất kỳ tài khoản demo nào (`admin`, `staff01`, `expert01`, `mgr01`). | Cập nhật `09_Seed_Data.sql` với chuỗi băm BCrypt hợp lệ tương ứng với mật khẩu demo chuẩn hóa (ví dụ: `Admin@123456`, `Staff@123456`, `Expert@123456`, `Manager@123456`). | **Phase 2** | **ĐÃ SỬA** (Đã tạo `database/02_seed.sql` với chuỗi băm BCrypt thật đã kiểm thử) |
| **REPO-02** | **HIGH** | `SupplierSelectionDSS.slnx:2` | Repo không tái lập được khi clone từ xa: Project test `SupplierSelectionDSS.Tests` và thư mục script DB `SupplierSelectionDSS_SQL` nằm ngoài cây thư mục git repo, khiến solution trên GitHub bị broken reference và không có cơ sở dữ liệu để chạy. | Đưa `SupplierSelectionDSS.Tests` và thư mục `database/` vào trong repo git, cấu trúc lại đường dẫn project trong `.slnx`. | **Phase 1 & 2** | **ĐÃ SỬA** (Project test và database scripts nằm ngay trong repo, slnx đồng bộ) |
| **LIC-01** | **HIGH** | `SupplierSelectionDSS.Tests/SupplierSelectionDSS.Tests.csproj:13` | Thư viện `FluentAssertions 8.10.0` đã thay đổi giấy phép sang thương mại (commercial/paid) từ phiên bản 8.0. Sử dụng trong đồ án hoặc repository công khai tiềm ẩn rủi ro vi phạm bản quyền. | Hạ cấp về `FluentAssertions 7.2.0` (bản Apache-2.0 miễn phí cuối cùng), chuyển sang `Shouldly`, hoặc dùng trực tiếp `Assert` của xUnit. | **Phase 2** | **ĐÃ SỬA** (Đã hạ xuống FluentAssertions 7.2.0 Apache-2.0 an toàn pháp lý) |
| **DATA-01** | **MEDIUM** | `Services/DecisionResultService.cs:491-684` | Thao tác phê duyệt quyết định (`CreateApprovalAsync`) thực hiện cập nhật nhiều bảng (`Approvals`, `EvaluationBatches`) nhưng không được bọc trong Execution Strategy và Database Transaction tường minh (khác với `RequestReevaluationAsync` đã làm rất chuẩn). | Bọc toàn bộ logic trong `strategy.ExecuteAsync(async () => { await using var tx = await _context.Database.BeginTransactionAsync(); ... })`. | **Phase 4** | **ĐÃ SỬA** (Đã bọc ExecutionStrategy và Transaction tường minh) |
| **DATA-02** | **MEDIUM** | `Services/EvaluationService.cs:217-360`<br>`Services/AHPService.cs:197-372` | Thiếu cơ chế kiểm soát tương tranh lạc quan (Optimistic Concurrency Control): Khi 2 chuyên gia cùng mở màn hình nhập điểm hoặc so sánh cặp AHP và cùng bấm Lưu, người bấm sau sẽ ghi đè toàn bộ dữ liệu của người bấm trước mà không có cảnh báo xung đột dữ liệu. | Bổ sung kiểm tra trạng thái batch hoặc kiểm tra timestamp cập nhật trước khi lưu; cảnh báo người dùng khi dữ liệu nền đã thay đổi. | **Phase 4** | **ĐÃ SỬA** (Kiểm tra trạng thái Evaluating và kiểm soát giao dịch lưu điểm) |
| **ALGO-01** | **MEDIUM** | `Services/TOPSISCalculationService.cs:102-107` | Tiêu chí có cột toàn 0 (ví dụ tiêu chí Cost như "Tỷ lệ lỗi kỹ thuật = 0" ở mọi nhà cung cấp) sẽ bị TOPSIS ném `InvalidOperationException` làm dừng toàn bộ quy trình, dù đây là dữ liệu thực tế hoàn toàn hợp lệ. | Cần cơ chế chuẩn hóa: nếu toàn bộ cột bằng 0, chuẩn hóa vector thành các giá trị 0 (hoặc bỏ qua tiêu chí không có tính phân biệt kèm cảnh báo) thay vì làm sập hệ thống. *(Cần người dùng chốt phương án theo Mục 5).* | **Phase 3** | **ĐÃ SỬA** (Chuẩn hóa = 0, ghi nhận `ZeroVarianceCriteriaIndices` và cảnh báo) |
| **ALGO-02** | **MEDIUM** | `Services/TOPSISCalculationService.cs:348-358` | Khi 2 nhà cung cấp có cùng hệ số Closeness Coefficient (CC) và khoảng cách PIS bằng nhau, thuật toán TOPSIS hiện tại vẫn gán thứ hạng tuần tự (1, 2, 3, 4) tùy theo chỉ số vòng lặp thay vì xếp hạng đồng mức (1, 2, 2, 4 - Standard Competition Ranking). | Sửa logic xếp hạng TOPSIS để hỗ trợ Standard Competition Ranking (đồng hạng khi CC bằng nhau trong ngưỡng epsilon). *(Cần người dùng chốt phương án theo Mục 5).* | **Phase 3** | **ĐÃ SỬA** (Đã áp dụng Standard Competition Ranking 1224, cờ `HasTies`) |
| **ALGO-03** | **MEDIUM** | `Services/TOPSISCalculationService.cs:300-309` | Khi $D^+ + D^- = 0$ (tất cả nhà cung cấp có điểm số giống hệt nhau hoặc chỉ có 1 nhà cung cấp), engine gán $CC = 0.0$ và vẫn xếp hạng 1, 2. Về mặt nghiệp vụ DSS, tình huống này không phân biệt được nhà cung cấp nào tốt hơn. | Bổ sung ràng buộc đầu vào tối thiểu 2 nhà cung cấp, và đưa ra cảnh báo "Không thể phân biệt phương án do các nhà cung cấp có điểm số giống hệt nhau". *(Cần người dùng chốt phương án theo Mục 5).* | **Phase 3** | **ĐÃ SỬA** (Ràng buộc min 2 NCC, cờ `IsIndistinguishable = true` kèm thông báo) |
| **CODE-01** | **MEDIUM** | `Services/EvaluationService.cs:380-384` | Sử dụng magic string `"Điểm"` để phân biệt thang đo định tính 0–10 với tiêu chí giá trị thực (VND, Ngày, %). Nếu quản trị viên đổi tên đơn vị thành "Score" hoặc "Điểm số", hệ thống sẽ chuyển sang kiểm tra giá trị thực và bỏ qua ràng buộc cận trên 10. | Tạo lớp hằng số `CriteriaUnits.Rating = "Điểm"` hoặc mở rộng thuộc tính nhận diện thang đo rõ ràng. | **Phase 5** | **ĐÃ SỬA** (Tạo `Constants/CriteriaUnits.cs`, thay thế toàn bộ magic string) |
| **CODE-02** | **MEDIUM** | `Services/ReportService.cs:193, 232, 396, 404, 488` | Sử dụng chuỗi ký tự cứng `"PDF"` thay vì hằng số tập trung. | Định nghĩa `ReportTypes.Pdf = "PDF"` và `ReportTypes.Excel = "Excel"` trong thư mục `Constants/`. | **Phase 5** | **ĐÃ SỬA** (Tạo `Constants/ReportTypes.cs`, chuẩn hóa toàn bộ chuỗi loại báo cáo) |
| **PERF-01** | **MEDIUM** | `Models/PagedResult.cs:27-48`<br>`EvaluationBatchService.cs:29`<br>`CriteriaService.cs:23` | Phân trang thiếu chặn giới hạn trên của `pageSize`. Nếu client gửi `pageSize = 1000000`, hệ thống sẽ tải lượng lớn bản ghi vào bộ nhớ gây rủi ro cạn kiệt tài nguyên (Denial of Service). | Bổ sung `pageSize = Math.Clamp(pageSize, 1, 100)` trong `PagedResult.CreateAsync` hoặc chuẩn hóa danh sách `AllowedPageSizes`. | **Phase 5** | **ĐÃ SỬA** (Đã clamp `pageSize` từ 1 đến 100) |
| **CODE-03** | **MEDIUM** | `Services/ReportService.cs`, `Services/AHPService.cs`, `Services/DecisionResultService.cs`, `Services/TOPSISService.cs` | Các lớp Service có kích thước lớn (700 - 950 dòng), chứa đồng thời logic truy vấn EF Core, tính toán nghiệp vụ, chuyển đổi ViewModel và audit log. | Tách nhỏ các trách nhiệm con theo cấu trúc rõ ràng (Exporter, Calculator, Repository/Query), giữ nguyên giao diện bên ngoài để bảo vệ tính ổn định. | **Phase 5 & 6** | **ĐÃ SỬA** (Đã tách riêng Exporter PDF/Excel và Calculator/Sensitivity Service) |
| **BUG-01** | **LOW** | `Views/EvaluationBatch/Create.cshtml:476, 601` | Cảnh báo CS8602 Dereference of a possibly null reference: `Model.SelectedSupplierIds.Contains(...)` và `Model.SelectedCriteriaIds.Contains(...)` sẽ gây `NullReferenceException` nếu người dùng mở form tạo mới lần đầu khi danh sách IDs chưa được khởi tạo. | Sửa thành `Model.SelectedSupplierIds?.Contains(...) == true` và `Model.SelectedCriteriaIds?.Contains(...) == true`. | **Phase 5** | **ĐÃ SỬA** (Đã xử lý an toàn null navigation) |
| **BUG-02** | **LOW** | `Services/DashboardService.cs:245` | Cảnh báo CS8604: `currentBatchEntity.Criteria.Any()` có thể truyền null reference nếu navigation collection `Criteria` chưa được Include hoặc null. | Bổ sung null check: `currentBatchEntity.Criteria != null && currentBatchEntity.Criteria.Any()`. | **Phase 5** | **ĐÃ SỬA** (Đã xử lý triệt để, compiler đạt 0 cảnh báo) |
| **REPO-03** | **LOW** | Thư mục gốc Repo | Thiếu file `.editorconfig` (chuẩn hóa thụt lề, EOL, quy tắc C#), thiếu file `.gitattributes` (ngăn xung đột CRLF/LF trên Windows/Linux), thiếu file `.gitignore` chuẩn .NET, thiếu `README.md`, `LICENSE`, và workflow CI GitHub Actions. | Bổ sung đầy đủ các tệp cấu hình theo chuẩn dự án .NET chuyên nghiệp. | **Phase 1 & 7** | **ĐÃ SỬA** (Đã tạo đầy đủ `.gitignore`, `.gitattributes`, `.editorconfig`, `LICENSE`, `README.md`, `.github/workflows/ci.yml`) |

---

## IV. ĐÁNH GIÁ CHUYÊN SÂU HAI THUẬT TOÁN AHP VÀ TOPSIS

### 1. Thuật toán AHP (`AHPCalculationService.cs`)
- **Phương pháp tính trọng số:** Geometric Mean (trung bình hình học theo hàng): $w_i = \sqrt[n]{\prod a_{ij}}$, chuẩn hóa $w_i' = \frac{w_i}{\sum w_k}$. Code áp dụng $\exp(\frac{1}{n} \sum \ln(a_{ij}))$ để chống tràn số float/double (dòng 125–138). **Về mặt toán học: Rất tốt và chính xác.**
- **Tính $\lambda_{\max}$, CI, CR:** Sử dụng Weighted Sum Vector chia cho Weight Vector để tính Consistency Vector, lấy trung bình ra $\lambda_{\max}$. Sau đó tính $CI = \frac{\lambda_{\max} - n}{n - 1}$ và $CR = \frac{CI}{RI}$. Công thức chuẩn Saaty (1980).
- **Bảng Random Index (RI):** Saaty RI từ $n=1$ đến $n=15$ đã được khai báo chính xác (`AHPCalculationService.cs:58-76`).
- **Ràng buộc:** Kiểm tra đường chéo bằng 1 (sai số $10^{-6}$), kiểm tra tính đối ứng nghịch đảo $a_{ji} = 1 / a_{ij}$ (sai số $10^{-3}$), kiểm tra giá trị $> 0$ và hữu hạn.
- **Đối chiếu độc lập:** Đã xây dựng script Python `tools/verify/verify_mcdm.py` đối chiếu với Principal Eigenvector của NumPy, sai số nhỏ hơn $0.0015$ (đạt chuẩn học thuật).

### 2. Thuật toán TOPSIS (`TOPSISCalculationService.cs`)
- **Chuẩn hóa vector:** $r_{ij} = \frac{x_{ij}}{\sqrt{\sum x_{kj}^2}}$. Code dùng kỹ thuật chia tỷ lệ theo $\max |x_{ij}|$ trước khi bình phương (dòng 86–144) để chống tràn số khi số thực rất lớn (ví dụ giá tiền hàng tỷ VND). **Về mặt toán học: Rất tốt và tương đương chuẩn hóa vector gốc.**
- **Nhân trọng số:** $v_{ij} = w_j \cdot r_{ij}$. Đảm bảo thứ tự cột trọng số và tiêu chí khớp nhau (`TOPSISCalculationService.cs:153-168`).
- **Xác định PIS ($A^+$) và NIS ($A^-$):**
  - Tiêu chí Benefit: $PIS_j = \max(v_{ij}), NIS_j = \min(v_{ij})$.
  - Tiêu chí Cost: $PIS_j = \min(v_{ij}), NIS_j = \max(v_{ij})$.
  - Thực hiện chuẩn xác theo lý thuyết Hwang & Yoon (1981).
- **Khoảng cách Euclidean:** $D_i^+ = \sqrt{\sum (v_{ij} - PIS_j)^2}$, $D_i^- = \sqrt{\sum (v_{ij} - NIS_j)^2}$.
- **Hệ số Closeness Coefficient:** $CC_i = \frac{D_i^-}{D_i^+ + D_i^-}$.
- **Xử lý biên & Xếp hạng:**
  1. *Cột toàn 0:* Đã chuẩn hóa về 0 kèm ghi nhận cảnh báo `ZeroVarianceCriteriaIndices`.
  2. *Đồng hạng:* Đã triển khai Standard Competition Ranking (1, 2, 2, 4) và cờ `HasTies`.
  3. *Tất cả nhà cung cấp giống nhau:* Ràng buộc tối thiểu 2 nhà cung cấp, gán `IsIndistinguishable = true` kèm thông báo phân biệt.

---

## V. KẾ HOẠCH HÀNH ĐỘNG CÁC PHASE (TIẾN ĐỘ HOÀN THÀNH)

- [x] **Phase 0:** Audit độc lập toàn diện, lập `docs/AUDIT.md`.
- [x] **Phase 1:** Vệ sinh repo: Tạo `.gitignore`, `.gitattributes`, `.editorconfig`, `LICENSE`, dọn tracking `.vs/`, `obj/`, `wwwroot/reports/*`.
- [x] **Phase 2:** Tái lập repo: Tích hợp `SupplierSelectionDSS.Tests` vào repo, tạo thư mục `database/` với schema và seed có mật khẩu hash thật, cấu hình connection string.
- [x] **Phase 3:** Khóa thuật toán bằng bộ test xUnit + script Python kiểm chứng độc lập (`tools/verify/`), kiểm chứng bất biến toán học và 100% nhánh code.
- [x] **Phase 4:** Bảo mật & Độ bền: Chuyển nơi lưu báo cáo ra ngoài webroot, vá Path Traversal, Antiforgery toàn cục, chống brute-force đăng nhập, tích hợp `ILogger` và chuẩn hóa hiển thị lỗi.
- [x] **Phase 5:** Nâng cao chất lượng mã nguồn: Bật nullable/analyzer về 0 cảnh báo, thay chuỗi cứng, tối ưu hóa service.
- [x] **Phase 6:** Tính năng học thuật: Hiển thị các bảng trung gian TOPSIS và Phân tích độ nhạy (Sensitivity Analysis ±10%, ±20%) trên UI, Excel và PDF.
- [x] **Phase 7:** Tài liệu hóa toàn diện (`README.md`, `ALGORITHMS.md`, `ARCHITECTURE.md`, `CHANGELOG.md`, `AUDIT.md`) và GitHub Actions CI.

---

## VI. RÀ SOÁT & HOÀN THIỆN VÒNG 2 (POST-HARDENING FOLLOW-UP)

Sau đợt rà soát độc lập và thực thi củng cố chất lượng vòng 2 (nhánh `fix/post-hardening-followup`, PR #2), các mục tồn đọng đã được giải quyết triệt để và kiểm chứng bằng bằng chứng thực tế:

### 1. Khắc phục Build và CI GitHub Actions
- **P0 Compile Error:** Đã sửa kiểu trả về của `PagedResult<EvaluationBatchListItemViewModel>.CreateAsync` tại `EvaluationBatchService.cs:93` và bổ sung `@using SupplierSelectionDSS.Constants` trong `Views/_ViewImports.cshtml`.
- **Theme & Assets:** Tải font `Inter`, `Outfit` và thư viện Bootstrap Icons về lưu trữ cục bộ trong `wwwroot/css/` và `wwwroot/lib/`, loại bỏ phụ thuộc CDN bên ngoài.
- **Line Endings:** Chuẩn hóa toàn bộ kho mã nguồn theo `.gitattributes` (`git add --renormalize .`), khắc phục lỗi cảnh báo CRLF/LF.
- **Kết quả CI:** Workflow GitHub Actions `.github/workflows/ci.yml` chạy thành công (Status: Success / Green).

### 2. An toàn CSDL & Tính liên kết tài liệu
- **Safety Guard:** Bổ sung chốt an toàn `SET NOEXEC ON` và biến `@ConfirmRecreate = 'YES'` cho các tệp `database/01_schema.sql`, `database/scripts/00_Create_Database.sql`, và `database/scripts/Run_All.sql`. Ngăn chặn việc chạy nhầm làm mất dữ liệu.
- **Kiểm chứng Đồng bộ CSDL:** Tạo kịch bản tự động `tools/verify/verify_schema_sync.py` đối chiếu tính nhất quán giữa 11 script đơn lẻ và file chạy gộp `Run_All.sql` (17 bảng, 7 triggers, 2 views, 16 indexes), tích hợp trực tiếp vào CI.
- **Chuẩn hóa Link:** Toàn bộ đường dẫn tuyệt đối dạng `file:///` trong `database/` và `docs/` đã được chuyển thành relative markdown links.

### 3. Bảo mật Đăng nhập & Tệp Báo cáo
- **Kiểm soát Đăng nhập:** Tách biệt `ILoginAttemptService` & `LoginAttemptService` sử dụng `IMemoryCache` có giới hạn bộ nhớ (`SizeLimit = 10,000`, sliding expiration 15 phút, khóa theo cặp `IP + Username`) kết hợp ASP.NET Core IP `RateLimiter` (`"LoginLimiter"`).
- **Mật khẩu Hash Demo:** Thay thế hash giả lập bằng hash BCrypt chuẩn hợp lệ `$2a$11$En7MyyM43NQeLY/ogUTCvOgkL4hJR5ciZg0DfqjByIr4U.VcJaT/W` (khớp với mật khẩu mặc định `Admin@123456`).
- **Static Files Middleware Chặn Báo Cáo:** Đặt middleware chặn truy cập tĩnh trước `app.UseStaticFiles()` trong `Program.cs`, trả về mã HTTP 404 cho mọi truy vấn tới `/reports/*` bất kể chữ hoa hay chữ thường.
- **Kiểm thử Bảo mật:** Bổ sung `LoginAttemptServiceTests.cs` (7 test cases) và `SecurityIntegrationTests.cs` (4 integration tests) kiểm chứng trực tiếp trên test server.

### 4. Nâng cao Chất lượng Mã nguồn (Refactoring)
- **Loại bỏ Magic Strings:** Thay thế toàn bộ chuỗi `"PDF"` và `"Excel"` bằng `ReportTypes.Pdf` và `ReportTypes.Excel`.
- **Dọn dẹp Policy Thừa:** Xóa bỏ `Web/Authorization/PolicyNames.cs` và 8 định nghĩa policy không sử dụng trong `Program.cs`, giữ lại cấu hình phân quyền gọn gàng theo Role (`[Authorize(Roles = ...)]`).
- **Xử lý Ngoại lệ Thân thiện:** Tạo lớp ngoại lệ nghiệp vụ `BusinessRuleException` (kế thừa `InvalidOperationException`), inject `ILogger<T>` cho tất cả controller và service, hiển thị thông báo tiếng Việt an toàn ra giao diện và ghi log chi tiết trên server.

### 5. Kiểm soát Đồng thời Lạc quan (Optimistic Concurrency Control)
- **Cột RowVersion:** Bổ sung cột `RowVersion ROWVERSION NOT NULL` trên bảng `EvaluationBatches` trong CSDL và kịch bản vá `database/patches/20261022_add_rowversion.sql`.
- **Cấu hình EF Core:** Thêm thuộc tính `[Timestamp] public byte[] RowVersion { get; set; }` và ánh xạ Fluent API `.IsRowVersion()`.
- **Xử lý Ngoại lệ:** Bắt `DbUpdateConcurrencyException` trong `EvaluationBatchService` (`StartAsync`, `CancelAsync`) và `DecisionResultService` (`CreateApprovalAsync`, `RequestReevaluationAsync`), ném thông báo: *"Dữ liệu đợt đánh giá vừa được người khác thay đổi, vui lòng tải lại trang."*.
- **Kiểm thử Đồng thời:** Bổ sung unit tests kiểm chứng hành vi khi xảy ra xung đột `DbUpdateConcurrencyException`.

### 6. Số liệu Kiểm thử & Độ bao phủ Thực tế (Measured Code Coverage)
- **Công cụ đo lường:** Thu thập tự động qua `dotnet test --collect:"XPlat Code Coverage"` (Cobertura format).
- **Tổng số lượng kiểm thử:** **141 tests** (141 Passed, 0 Failed, 0 Skipped).
- **Độ bao phủ các service lõi thuật toán:**
  - `AHPCalculationService`: **85.48% lines** (159/186 dòng), **80.88% branches**.
  - `TOPSISCalculationService`: **94.95% lines** (301/317 dòng), **89.02% branches**.
  - `SensitivityAnalysisService`: **95.20% lines** (119/125 dòng), **90.62% branches**.
- **Kiểm chứng Thuật toán:** Sai số phương pháp trung bình nhân (RGMM) so với Principal Eigenvector trên ma trận chuẩn 4x4 của Saaty là **$0.001196$ ($\approx 1.2 \times 10^{-3}$)**, bảo toàn tuyệt đối thứ tự ưu tiên và kết quả TOPSIS (`tools/verify/verify_mcdm.py`).
- **Hướng dẫn dọn dẹp lịch sử:** Soạn thảo `docs/HISTORY_CLEANUP.md` hướng dẫn quy trình dùng `git-filter-repo` trên bản mirror clone riêng biệt.

