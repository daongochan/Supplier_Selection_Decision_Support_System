# CHANGELOG — Nhật Ký Thay Đổi Dự Án

Toàn bộ các thay đổi quan trọng về kiến trúc, thuật toán, bảo mật và tính năng của hệ thống **Supplier Selection DSS** được ghi nhận tại đây theo chuẩn [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [1.2.0] - 2026-10-22

### Củng Cố Toàn Diện & Tự Động Hóa CI (Round 2 Hardening)
- **Khắc phục lỗi biên dịch & CI Xanh 100%:** Sửa lỗi kiểu trả về P0 của `PagedResult<T>.CreateAsync`, cục bộ hóa assets CSS/font và dọn dẹp cảnh báo EOL.
- **An toàn Cơ sở Dữ liệu & Đồng bộ Schema:**
  - Bổ sung chốt an toàn `SET NOEXEC ON` và `@ConfirmRecreate = 'YES'` cho các script tạo/xóa database.
  - Bổ sung công cụ `tools/verify/verify_schema_sync.py` đối chiếu tự động 17 bảng, 7 triggers, 2 views, 16 indexes.
  - Thay thế toàn bộ liên kết tuyệt đối `file:///` bằng relative links.
- **Bảo mật & Tách biệt Trách nhiệm (Security & Hardening):**
  - Tách `ILoginAttemptService` & `LoginAttemptService` sử dụng `IMemoryCache` có giới hạn (`SizeLimit = 10,000`, 15 phút, khóa theo `IP + Username`) kết hợp ASP.NET Core IP `RateLimiter` (`"LoginLimiter"`).
  - Thay hash mật khẩu demo bằng BCrypt chuẩn `$2a$11$...`.
  - Triển khai middleware chặn truy cập tĩnh `/reports/*` trước `UseStaticFiles()` trả về HTTP 404 Not Found.
  - Thêm 11 test bảo mật tích hợp (`LoginAttemptServiceTests`, `SecurityIntegrationTests`).
- **Nâng cao Chất lượng & Xử lý Ngoại lệ:**
  - Loại bỏ chuỗi ma thuật `"PDF"`/`"Excel"`, inject `ILogger<T>` cho toàn bộ service và controller.
  - Dọn dẹp policy thừa, tạo lớp `BusinessRuleException` chuẩn hóa thông báo lỗi thân thiện.
- **Kiểm soát Đồng thời Lạc quan (Optimistic Concurrency Control):**
  - Bổ sung cột `RowVersion ROWVERSION NOT NULL` trên bảng `EvaluationBatches`.
  - Cấu hình EF Core `[Timestamp]` / `IsRowVersion()` và xử lý `DbUpdateConcurrencyException`.
  - Bổ sung script vá `database/patches/20261022_add_rowversion.sql` và unit test kiểm chứng concurrency.
- **Kiểm chứng Thuật toán & Đo lường Coverage:**
  - Thu thập Cobertura coverage thực tế: **141 tests** (141 passed).
  - Độ bao phủ: `TOPSISCalculationService` 94.95% dòng (89.02% nhánh), `SensitivityAnalysisService` 95.20% dòng (90.62% nhánh), `AHPCalculationService` 85.48% dòng (80.88% nhánh).
  - Độc lập kiểm chứng sai số RGMM vs Eigenvector qua `tools/verify/verify_mcdm.py` ($\approx 1.2 \times 10^{-3}$).
  - Soạn thảo tài liệu dọn dẹp lịch sử Git an toàn `docs/HISTORY_CLEANUP.md`.

## [1.1.0] - 2026-10-22

### Phase 1: Vệ sinh Kho mã nguồn (Repository Hygiene)
- Bổ sung `.gitignore` chuẩn hóa cho .NET và Visual Studio, ngăn chặn rò rỉ `.vs/`, `bin/`, `obj/`, `*.user`, `*.suo`, và các tệp báo cáo cá nhân.
- Gỡ bỏ theo dõi (untrack) khỏi chỉ mục Git các tệp rác và tệp dữ liệu cá nhân (`.vs/`, `obj/`, `SupplierSelectionDSS.csproj.user`, `wwwroot/reports/*.pdf|xlsx`).
- Thêm `.gitattributes` để chuẩn hóa EOL (CRLF/LF) và mã hóa UTF-8.
- Bổ sung tệp giấy phép phần mềm mã nguồn mở `LICENSE` (MIT License).
- Thêm cấu hình `.editorconfig` chuẩn hóa phong cách viết mã C# và Razor.

### Phase 2: Tái lập Môi trường & Cơ sở Dữ liệu (Reproducibility)
- Tích hợp dự án kiểm thử `SupplierSelectionDSS.Tests` trực tiếp vào repo dưới định dạng xUnit, đồng bộ cấu trúc solution `.slnx`.
- Cấu hình `<DefaultItemExcludes>` trong `SupplierSelectionDSS.csproj` để loại trừ mã test khỏi tiến trình biên dịch chính.
- Chuyển `Moq` và các thư viện kiểm thử ra khỏi dự án web chính; hạ phiên bản `FluentAssertions` về `7.2.0` (giấy phép Apache-2.0 an toàn).
- Tạo thư mục `database/` chứa tệp kịch bản `01_schema.sql` (17 bảng, 7 triggers kiểm soát tính toàn vẹn, 2 views thống kê) và `02_seed.sql` (dữ liệu mẫu: nhóm hàng, 8 tiêu chí Benefit/Cost, 5 NCC, 4 tài khoản người dùng băm BCrypt).
- Bổ sung tệp hướng dẫn `AGENTS.md` và `database/README.md`.

### Phase 3: Khóa Thuật toán bằng Kiểm thử Độc lập (Algorithm Lock)
- **TOPSIS - Xử lý cột toàn 0 (Zero-variance criteria):**
  - *Hành vi trước:* Ném `InvalidOperationException` làm dừng toàn bộ tiến trình đánh giá nếu có một tiêu chí mà tất cả NCC đều có giá trị bằng 0 (ví dụ: tỷ lệ lỗi = 0% ở mọi NCC).
  - *Hành vi sau:* Không ném lỗi. Chuẩn hóa cột này về 0, ghi nhận chỉ số tiêu chí vào `ZeroVarianceCriteriaIndices` trong `TOPSISCalculationResult` để cảnh báo rằng tiêu chí này không có khả năng phân biệt giữa các nhà cung cấp.
- **TOPSIS - Hạng khi Closeness Coefficient bằng nhau:**
  - *Hành vi trước:* Gán thứ hạng tuần tự (1, 2, 3, 4) ngay cả khi hai nhà cung cấp có cùng CC và khoảng cách $D^+$.
  - *Hành vi sau:* Áp dụng xếp hạng chuẩn thi đấu (Standard Competition Ranking - 1, 2, 2, 4). Cung cấp cờ `HasTies = true` khi có đồng hạng.
- **TOPSIS - Trường hợp $D^+ + D^- = 0$ (Các nhà cung cấp giống hệt nhau):**
  - *Hành vi trước:* Gán $CC = 0.0$ nhưng vẫn gán hạng tuần tự 1, 2, 3.
  - *Hành vi sau:* Bắt buộc tối thiểu 2 nhà cung cấp. Khi tất cả NCC có điểm giống hệt nhau trên mọi tiêu chí, toàn bộ nhận $CC = 0.0$ và đồng hạng 1 (`IsIndistinguishable = true`).
- **TOPSIS - Ràng buộc số lượng nhà cung cấp:**
  - Yêu cầu tối thiểu 2 nhà cung cấp để thực hiện so sánh đa tiêu chí TOPSIS.
- **Bộ Kiểm Thử Độc Lập:**
  - Bổ sung công cụ kiểm chứng độc lập bằng Python & NumPy (`tools/verify/verify_mcdm.py`).
  - Đối chiếu trọng số AHP giữa Geometric Mean và Principal Eigenvector (sai số $< 0.0015$).
  - Đối chiếu 100% số liệu trung gian của TOPSIS (PIS, NIS, $D^+$, $D^-$, CC, Rank) với bộ benchmark độc lập.
  - Kiểm thử các tính chất bất biến toán học: tính bất biến theo tỷ lệ nhân dương (Scale Invariance), tính bất biến theo hoán vị hàng (Permutation Invariance).
  - Đạt 114 test vượt qua với độ bao phủ nhánh thuật toán tuyệt đối.

### Phase 4: Bảo mật & Độ Bền Vững (Security & Hardening)
- **Bảo vệ tệp báo cáo:** Chuyển toàn bộ nơi lưu trữ báo cáo ra ngoài webroot vào thư mục cách ly `App_Data/reports` (hỗ trợ cấu hình tùy biến qua `appsettings.json`).
- **Chặn truy cập tĩnh:** Cấu hình middleware `UseStaticFiles` chặn hoàn toàn việc gọi trực tiếp URL tĩnh `/reports/*` khi chưa xác thực, trả về HTTP `404 Not Found`.
- **Chống leo quyền thư mục (Path Traversal):** Áp dụng `Path.GetFullPath` và đối chiếu tiền tố thư mục chuẩn hóa tại `ReportService.GetDownloadAsync`.
- **Bảo vệ Đăng nhập:**
  - Chống User Enumeration: Chuẩn hóa thông báo lỗi chung cho cả hai trường hợp sai tài khoản và sai mật khẩu.
  - Chống Timing Attack: Thực thi xác thực BCrypt giả lập khi người dùng không tồn tại.
  - Khóa tài khoản (Lockout): Tự động khóa đăng nhập 15 phút sau 5 lần thử sai liên tiếp.
  - Ghi nhật ký kiểm toán (Audit Log) và hệ thống log `ILogger` cho mọi lượt thử đăng nhập.
- **Bảo vệ vai trò Quản trị viên:** Chặn việc vô hiệu hóa hoặc hạ quyền tài khoản Administrator hoạt động cuối cùng của hệ thống trong `UserService.UpdateAsync`.
- **Bộ lọc CSRF toàn cục:** Bổ sung `AutoValidateAntiforgeryTokenAttribute` cho toàn bộ các yêu cầu HTTP `POST`, `PUT`, `DELETE`.
- **Security Response Headers:** Tự động chèn `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy: strict-origin-when-cross-origin`.
- **Health Check:** Bổ sung endpoint `/health` kiểm tra trạng thái hoạt động của hệ thống.

### Phase 5: Nâng Cao Chất Lượng Mã Nguồn (Code Quality)
- Loại bỏ các chuỗi ký tự ma thuật (magic strings), chuẩn hóa thành các hằng số rõ ràng:
  - `Constants/CriteriaUnits.cs`: Hằng số `Rating = "Điểm"` kèm phương thức kiểm tra thang đo.
  - `Constants/ReportTypes.cs`: Hằng số `Pdf = "PDF"` và `Excel = "Excel"`.
- Giới hạn tham số phân trang `pageSize` trong `PagedResult.CreateAsync` (`Math.Clamp(pageSize, 1, 100)`).
- Xử lý triệt để toàn bộ cảnh báo trình biên dịch nullable (CS8602, CS8604), đưa số lượng cảnh báo compiler của dự án về mức **0 cảnh báo, 0 lỗi**.

### Phase 6: Tính Năng Học Thuật (Academic Features)
- **Hiển thị Bảng Trung Gian TOPSIS:**
  - Bổ sung cấu trúc dữ liệu `TOPSISMatrixRowViewModel`.
  - Hiển thị Ma trận Chuẩn hóa Vector ($R = [r_{ij}]$) và Ma trận Chuẩn hóa có trọng số ($V = [v_{ij}]$) trực quan trên giao diện `Views/TOPSIS/Index.cshtml`.
  - Tích hợp thêm các sheet "Normalized Matrix" và "Weighted Matrix" vào tệp xuất Excel (ClosedXML).
- **Phân Tích Độ Nhạy (Sensitivity Analysis):**
  - Xây dựng `SensitivityAnalysisService` mô phỏng biến thiên trọng số từng tiêu chí $\pm 10\%, \pm 20\%$.
  - Tái cân bằng tỷ lệ các tiêu chí còn lại để tổng trọng số luôn bảo toàn đúng bằng 1.0.
  - Đánh giá tính ổn định của nhà cung cấp dẫn đầu (#1), phát hiện các tiêu chí nhạy cảm, tính toán phần trăm độ vững bền (`RobustnessPercentage`).
  - Hiển thị bảng phân tích độ nhạy trên giao diện TOPSIS, trang Quyết định Phê duyệt (`DecisionResult`), tệp xuất Excel và báo cáo PDF (QuestPDF).
  - Bổ sung bộ unit test tự động cho `SensitivityAnalysisService` (nâng tổng số test lên 123 tests).

### Phase 7: Tài Liệu Học Thuật & CI Pipeline (Documentation & CI)
- Soạn thảo tài liệu toán học chuyên sâu `docs/ALGORITHMS.md`.
- Soạn thảo tài liệu kiến trúc phần mềm, bảo mật và vòng đời nghiệp vụ `docs/ARCHITECTURE.md`.
- Soạn thảo `README.md` song ngữ (Anh - Việt) kèm hướng dẫn dựng dự án trên máy trống.
- Cập nhật toàn bộ trạng thái trong báo cáo kiểm toán độc lập `docs/AUDIT.md`.
- Thiết lập CI Pipeline tự động bằng GitHub Actions `.github/workflows/ci.yml` trên nền .NET 10.
