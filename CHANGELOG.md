# CHANGELOG — Nhật Ký Thay Đổi Dự Án

Toàn bộ các thay đổi quan trọng về kiến trúc, thuật toán, bảo mật và tính năng của hệ thống **Supplier Selection DSS** được ghi nhận tại đây theo chuẩn [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [1.4.0] - 2026-10-23

### Comprehensive System Audit Remediation (Security, Concurrency, Auditability & UX)

- **Security & Session Integrity (P0 — F01, F04):**
  - Replaced all `innerHTML` string interpolation in `Views/EvaluationBatch/Create.cshtml` with safe DOM creation (`createElement`, `textContent`, `replaceChildren`) to eliminate stored DOM XSS sinks.
  - Added `UserCookieSecurityStamp` (`dss:security_stamp`) and `CookieAuthenticationOptions.Events.OnValidatePrincipal` in `Program.cs` to validate active user status, password hash stamp, and live role on every request; added safe local `ReturnUrl` redirection on login.
- **Aggregate Concurrency & Data Integrity (P0/P1 — F02, F03, F10):**
  - Updated `EvaluationController.Save` and `EvaluationService.SaveAsync` to inspect `ModelState.IsValid`, preserve raw invalid inputs without deleting existing scores, require explicit `ClearedCells` flags to delete cells, and treat unchanged score saves as no-ops.
  - Added `BatchConcurrencyHelper` to verify and atomically bump `EvaluationBatch.RowVersion` (`WHERE BatchID = @id AND RowVersion = @rv`) inside transactions across all batch, score, AHP, TOPSIS, and decision mutations.
  - Updated SQL Server triggers (`01_schema.sql`, `Run_All.sql`, `07_Triggers.sql`, `20261023_trigger_batch_state_locks.sql`) to block child table mutations when a batch is `Completed` or `Cancelled`.
  - Wrapped administrator role/status updates in `IsolationLevel.Serializable` transactions in `UserService` to prevent concurrent demotions from leaving zero active administrators.
- **Immutable Decision History & Frozen Snapshots (P1 — F05, F06):**
  - Removed `RemoveRange` on non-final `Approvals`; decision events remain append-only and are marked as `[Superseded ...]` when scores or AHP weights change.
  - Upgraded `BatchRoundSnapshotData` (`SchemaVersion = 2`) to store full supplier/criterion metadata, raw scores, AHP weights, TOPSIS matrices, ranking, and sensitivity stability; updated `DecisionResultService` and `ReportService` to serve completed batches directly from frozen snapshots.
- **Workflow Readiness, UX & Accessibility (P1/P2 — F07, F08, F09, F11, F12):**
  - Introduced `BatchWorkflowReadinessService` shared across `EvaluationBatch/Details` and `DashboardService`.
  - Rendered `_Topbar.cshtml` in `_Layout.cshtml` with `#sidebarToggle` (`aria-controls="appSidebar"`, `aria-expanded`) for viewports `<= 900px`.
  - Added `AbortController`, request sequence counter, and supplier selection preservation in `Views/EvaluationBatch/Create.cshtml`.
  - Added Shared Batch Matrix disclosure banner in `Views/AHP/Index.cshtml`, progressive disclosure (`<details>`) in `Views/TOPSIS/Index.cshtml`, and native `checkValidity()`/`requestSubmit()` + keyboard focus trap in `wwwroot/js/site.js`.
- **Performance, Pagination & Automated Verification (P2 — F13, F14, F15):**
  - Eliminated duplicate `TOPSISService.GetWorkspaceAsync` calls in `DecisionResultService` and `ReportService`, gated `PerfBenchmarkRunner` behind `DSS_RUN_PERF=1`, and protected pagination against `int` overflow.
  - Expanded automated test suite to **164 passing xUnit tests**, including `McdmBenchmarkParityTests`, `AuditFindingsRegressionTests`, and disposable SQL Server `SqlServerConcurrencyAndTriggerTests`.

## [1.3.0] - 2026-10-22

### Tối Ưu Hiệu Năng, Giao Diện & Toàn Vẹn Luồng Nghiệp Vụ (Round 3 Optimization)

- **Đo lường & Tối ưu truy vấn dữ liệu (Data Access Optimization):**
  - **Dashboard:** Giảm 53% số lệnh SQL (từ 17 xuống 8 câu lệnh), loại bỏ tích Descartes (`Suppliers` × `Criteria`), bổ sung bộ đệm `IMemoryCache` (30 giây) cho 6 KPI hệ thống, giới hạn dropdown `Take(50)`. Thời gian phản hồi p50 giảm từ **25.649,8 ms** xuống **155,2 ms** (nhanh hơn **165 lần / giảm 99,4%**).
  - **Audit Log:** Bổ sung chỉ mục `IX_AuditLogs_ActionTime` trên 10.000 dòng nhật ký. Thời gian phản hồi p50 giảm từ **580,4 ms** xuống **40,1 ms** (nhanh hơn **14,5 lần / giảm 93,1%**).
  - **Split Queries:** Áp dụng `.AsSplitQuery()` cho các truy vấn nạp đồng thời `Suppliers` và `Criteria` tại `EvaluationService`, `ReportService`, và `TOPSISService`, ngăn ngừa bùng nổ dữ liệu truyền tải.
- **Tối ưu Tài nguyên tĩnh & Phân phối (Static Asset Delivery):**
  - **Cục bộ hóa Font & Icon 100%:** Tải và nhúng toàn bộ font Source Serif 4 (WOFF2) và Bootstrap Icons v1.11.3 vào kho cục bộ (`wwwroot/fonts/`), loại bỏ hoàn toàn các liên kết CDN (`fonts.googleapis.com`, `cdn.jsdelivr.net`), giúp hệ thống chạy độc lập 100% trong môi trường mạng nội bộ (intranet/offline).
  - **Xóa bỏ thác nước `@import`:** Đóng gói toàn bộ token, kiểu chữ, bố cục và component vào `site.css` (76 KB), xóa bỏ chuỗi 26 file con nạp tuần tự. Các view nạp CSS đặc thù qua `@section Styles` kèm `asp-append-version="true"`.
  - **Nén phản hồi & Bộ đệm:** Kích hoạt nén Brotli + Gzip (`CompressionLevel.Fastest`) và cấu hình `Cache-Control: public, max-age=31536000, immutable` cho tài nguyên tĩnh gắn mã phiên bản.
  - **Chuyển đổi hình ảnh WebP:** Giảm 75,2% dung lượng ảnh hệ thống (`access-denied-duck.webp` 4,1 KB, `login-left.webp` 18,7 KB, `sidebar-bg.webp` 2,9 KB) kèm thẻ `<picture>` fallback.
- **Khả năng tiếp cận (Accessibility - A11y) & Trải nghiệm người dùng (UX):**
  - Bổ sung **Skip-link** (`#main-content`) và chuẩn hóa `:focus-visible` phục vụ điều hướng hoàn toàn bằng bàn phím.
  - Thanh bên di động (Mobile Sidebar) được trang bị backdrop làm mờ, khóa cuộn thân trang (`overflow: hidden`), hỗ trợ đóng bằng phím `Escape` hoặc khi bấm ngoài.
  - Ma trận so sánh AHP được bổ sung `scope="col"`, `scope="row"`, nhãn `aria-label` chi tiết cho từng ô thang đo Saaty, tiêu đề hàng/cột cố định (`position: sticky`) khi cuộn ngang/dọc.
  - Các biểu đồ Dashboard được gắn `role="img"`, `aria-label` và bảng tóm tắt số liệu ẩn cho trình đọc màn hình (`.sr-only`).
  - Tách toàn bộ script nội tuyến của trang AHP sang `wwwroot/js/pages/ahp.js` và dọn dẹp 35 thuộc tính `style="..."` tĩnh trên Dashboard vào class CSS trong `dashboard.css`.
  - Hỗ trợ chuẩn người dùng nhạy cảm chuyển động qua `@media (prefers-reduced-motion: reduce)`.
- **Toàn vẹn Nghiệp vụ & Dữ liệu (Business Logic Hardening):**
  - **Lưu vết Snapshot vòng đánh giá (C1):** Bổ sung bảng `BatchRoundSnapshots` lưu trữ toàn vẹn JSON UTF-8 của đợt đánh giá khi yêu cầu **Đánh giá lại (Re-evaluation)** hoặc **Phê duyệt chính thức (Final Approval)**, không làm mất dữ liệu lịch sử các vòng trước.
  - **Tự động hủy phê duyệt chờ khi sửa điểm/trọng số (C2):** Tự động vô hiệu hóa các bản ghi `Approvals` chưa hoàn tất khi lưu điểm đánh giá hoặc cập nhật ma trận AHP.
  - **Bắt buộc giải trình khi chọn NCC ngoài Top 1 (C4):** Yêu cầu nhập lý do giải trình khi phê duyệt nhà cung cấp không xếp hạng 1 TOPSIS (`RankNo != 1`), bổ sung badge cảnh báo trực quan trên giao diện phê duyệt.
  - **Kiểm soát đồng thời lạc quan (C5):** Bắt `DbUpdateConcurrencyException` và dịch thành thông báo tiếng Việt thân thiện trên toàn bộ các tác vụ lưu điểm, lưu AHP và phê duyệt.
  - **Khóa đơn vị đo tiêu chí đang sử dụng (C6):** Ngăn chặn chỉnh sửa thuộc tính `Unit` trên các tiêu chí đã có điểm đánh giá hoặc dữ liệu AHP/TOPSIS.
  - **Tái sử dụng báo cáo sẵn có (A5):** Cung cấp nút tải tức thì báo cáo PDF/Excel đã sinh kèm nút "Tạo lại" khi cần cập nhật.
- **Đề xuất Kiến trúc Hội đồng Đa Chuyên gia (Group AHP Proposal):**
  - Soạn thảo tài liệu thiết kế học thuật `docs/GROUP-AHP-PROPOSAL.md` theo phương pháp Trung bình nhân (Geometric Mean - AIJ của Thomas L. Saaty), gồm thiết kế DDL 2 bảng `AHPEvaluatorAssignments` và `AHPEvaluatorComparisons`, công thức toán học và chỉ số đồng thuận nhóm.
- **Đồng bộ Cơ sở dữ liệu & Kiểm thử:**
  - Đồng bộ 100% CSDL qua bản vá `database/patches/20261022_round3_indexes_and_snapshots.sql`, cập nhật `01_schema.sql` và `database/scripts/*`.
  - Đạt 100% qua công cụ kiểm tra `python tools/verify/verify_schema_sync.py` (18 bảng, 21 chỉ mục, 7 triggers, 2 views).
  - Bộ kiểm thử tự động tăng lên **151 ca kiểm thử** (`151 passed, 0 failed`).

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
