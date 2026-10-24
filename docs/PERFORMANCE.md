# BÁO CÁO ĐO LƯỜNG VÀ TỐI ƯU HIỆU NĂNG (PERFORMANCE REPORT)

Tài liệu này ghi nhận số liệu đo lường hiệu năng thực tế trước và sau tối ưu cho dự án **Supplier Selection Decision Support System (DSS)** trong Vòng 3.

---

## 1. MÔI TRƯỜNG VÀ PHƯƠNG PHÁP ĐO

### 1.1. Cơ sở dữ liệu thử nghiệm (`SupplierSelectionDSS_Perf`)
- **DBMS:** Microsoft SQL Server 2025 Developer Edition (x64), chạy cục bộ (`Server=.`).
- **Phạm vi an toàn:** Đo lường 100% trên bản sao CSDL độc lập `SupplierSelectionDSS_Perf`, tuyệt đối không tác động lên CSDL phát triển chính (`SupplierSelectionDSS`).
- **Quy mô dữ liệu thử nghiệm:**
  - `Users`: 4 người dùng (4 vai trò: Administrator, PurchasingStaff, Expert, Manager)
  - `ProductGroups`: 10 nhóm hàng hoá công nghiệp
  - `Suppliers`: 200 nhà cung cấp (`SUP001` -> `SUP200`), gán vào các nhóm hàng
  - `Criteria`: 12 tiêu chí đánh giá (kết hợp Benefit/Cost, thang Điểm/VND/Ngày/%)
  - `EvaluationBatches`: 40 đợt đánh giá (15 Completed, 15 Evaluating, 6 Draft, 4 Cancelled) trải dài trong 6 tháng qua
  - `EvaluationScores`: 3.916 điểm đánh giá chi tiết
  - `AHPPairwiseComparisons`: Ma trận so sánh cặp đầy đủ cho các đợt đã đánh giá
  - `AHPResults` & `AHPCriteriaWeights`: 25 bản ghi trọng số và kết quả AHP
  - `TopsisIdealSolutions` & `TopsisResults`: 388 kết quả xếp hạng TOPSIS
  - `Approvals`: 15 biên bản phê duyệt
  - `Reports`: 30 báo cáo PDF và Excel
  - `AuditLogs`: 10.000 bản ghi nhật ký hoạt động trải dài 6 tháng

### 1.2. Công cụ và phương pháp đo
- **Đếm số lệnh SQL:** Sử dụng `DbCommandInterceptor` của Entity Framework Core chặn tại tầng thực thi ADO.NET (`ReaderExecuting`, `ScalarExecuting`, `NonQueryExecuting`), đảm bảo đếm chính xác 100% số câu lệnh SQL gửi đến SQL Server trong suốt vòng đời của mỗi HTTP Request.
- **Thời gian phản hồi:** Đo lường thời gian đáp ứng (Wall-clock Duration) thông qua bộ thử nghiệm tích hợp `WebApplicationFactory<Program>`, có xác thực vai trò Administrator (`admin`), chạy warm-up và tính toán phân vị `p50` và `p95`.
- **Tài nguyên tĩnh & Dung lượng trang:** Đo trực tiếp kích thước HTML trả về (bytes), số lượng thẻ `<link rel="stylesheet">`, `<script src="...">`, thẻ `<img>`, cùng phân tích phụ thuộc CDN / chuỗi `@import`.

### 1.3. Cách tái lập kết quả đo (Reproduction Steps)
1. Khởi tạo CSDL benchmark và sinh dữ liệu:
   ```bash
   python tools/perf/setup_perf_db.py
   ```
2. Chạy bộ đo lường tự động:
   ```bash
   dotnet test --filter "FullyQualifiedName~RunBaselineBenchmark_OnPerfDatabase" --logger "console;verbosity=normal"
   ```
3. Xem kết quả chi tiết xuất ra tại `docs/qa/evidence/performance/baseline_results.json`.

---

## 2. BẢNG SỐ LIỆU ĐO LƯỜNG BASELINE (TRƯỚC TỐI ƯU)

| STT | Màn hình / Endpoint | URL | Số lệnh SQL (Trước) | p50 (Trước) | p95 (Trước) | Dung lượng HTML | CSS tags | JS tags | Ghi chú kỹ thuật |
|:---:|---|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| 1 | **Dashboard (Tổng quan)** | `/Dashboard` | **17** | **25.649,8 ms** | **25.796,6 ms** | 52.923 B | 4 | 3 | 6 `Count()` tuần tự, nạp 40 batch vào dropdown, Cartesian product `Suppliers` × `Criteria`, `AuditLogs` scan ~25s do thiếu index |
| 2 | **Danh sách Batch** | `/EvaluationBatch/Index` | **7** | **77,5 ms** | **86,1 ms** | 102.408 B | 4 | 1 | Phân trang kèm đếm tổng số dòng |
| 3 | **Nhập điểm đánh giá** | `/Evaluation/Index?batchId=1` | **2** | **44,2 ms** | **47,5 ms** | 178.149 B | 3 | 1 | Nạp ma trận điểm nhà cung cấp × tiêu chí |
| 4 | **Ma trận so sánh AHP** | `/EvaluationBatch/1/AHP` | **4** | **14,3 ms** | **26,7 ms** | 94.702 B | 3 | 1 | Nạp ma trận cặp và trọng số tiêu chí |
| 5 | **Kết quả TOPSIS** | `/EvaluationBatch/1/TOPSIS` | **6** | **47,6 ms** | **66,5 ms** | 243.276 B | 3 | 1 | Nạp kết quả xếp hạng và khoảng cách PIS/NIS |
| 6 | **Kết quả quyết định** | `/EvaluationBatch/1/Result` | **10** | **37,6 ms** | **45,3 ms** | 59.776 B | 4 | 1 | Chạy phân tích độ nhạy (28 kịch bản) và nạp phê duyệt |
| 7 | **Lịch sử báo cáo** | `/EvaluationBatch/1/Report` | **2** | **12,2 ms** | **31,6 ms** | 36.490 B | 3 | 1 | Nạp danh sách file báo cáo đã xuất |
| 8 | **Nhật ký hệ thống** | `/AuditLog/Index` | **7** | **580,4 ms** | **580,4 ms** | 67.752 B | 4 | 1 | Quét 10.000 dòng log; 4 lệnh đếm KPI riêng rẽ |

---

## 3. PHÂN TÍCH CHI TIẾT TÀI NGUYÊN TĨNH (STATIC ASSETS)

- **Chuỗi thác nước `@import`:** `wwwroot/css/site.css` thực hiện nạp **26 file CSS** thông qua chuỗi `@import url(...)` (dòng 7–42). Trình duyệt bắt buộc phải tải xong `site.css` mới phát hiện các file con, gây hiện tượng thác nước (waterfall request) và chặn dựng hình (render-blocking).
- **Phụ thuộc CDN bên ngoài:** `Views/Shared/_Layout.cshtml` nạp Google Fonts (`fonts.googleapis.com`, `fonts.gstatic.com`) và Bootstrap Icons (`cdn.jsdelivr.net`). Hệ thống nội bộ (Enterprise) bị phụ thuộc internet bên ngoài, hỏng giao diện khi triển khai intranet/offline.
- **Cache-Control & Nén:** `Program.cs` dùng `app.UseStaticFiles()` mặc định chưa cấu hình header `Cache-Control` cho tài nguyên có `?v=` (`asp-append-version`). Chưa bật nén tĩnh (Brotli/Gzip).
- **Style nội tuyến:** `Views/Dashboard/Index.cshtml` chứa 39 thuộc tính `style="..."`; các View chính chứa script nội tuyến kích thước lớn (không thể cache độc lập).

---

## 4. BẢNG ĐỐI CHIẾU TRƯỚC / SAU KHI TỐI ƯU

### 4.1. Kết quả Phase 1 — Tối ưu truy vấn dữ liệu (Data Access Optimization)

*Số liệu được đo thực tế vào 22/10/2026 trên CSDL `SupplierSelectionDSS_Perf` (10.000 logs, 40 batches, 200 suppliers) bằng `dotnet test --filter "FullyQualifiedName~PerfBenchmarkRunner"`.*

| STT | Màn hình | Số lệnh SQL (Trước) | Số lệnh SQL (Phase 1) | p50 Trước (ms) | p50 Sau (Phase 1) | Cải thiện thời gian | Ghi chú kỹ thuật Phase 1 |
|:---:|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | **Dashboard** | **17** | **8** | **25.649,8 ms** | **151,9 ms** | **Nhanh hơn 168 lần (-99,4%)** | Giảm 53% số lệnh SQL; xóa bỏ tích Descartes; bộ đệm `IMemoryCache` (30s) cho 6 KPI hệ thống; dropdown `Take(50)`; chỉ chiếu các cột cần thiết từ `AuditLogs` |
| 2 | **Danh sách Batch** | 7 | 7 | 77,5 ms | 22,9 ms | Nhanh hơn 3,4 lần (-70,5%) | Nhờ bộ đệm hệ thống và phân mảnh I/O giảm |
| 3 | **Nhập điểm đánh giá** | 2 | 4 | 44,2 ms | 20,0 ms | Nhanh hơn 2,2 lần (-54,8%) | `.AsSplitQuery()` tách truy vấn riêng biệt cho Suppliers & Criteria, loại bỏ tích Descartes |
| 4 | **Ma trận AHP** | 4 | 4 | 14,3 ms | 18,6 ms | Tương đương (ổn định) | Tải ma trận so sánh cặp |
| 5 | **Kết quả TOPSIS** | 6 | 8 | 47,6 ms | 35,3 ms | Nhanh hơn 1,3 lần (-25,8%) | `.AsSplitQuery()` tách 2 truy vấn song song cho Suppliers và Criteria, ngăn nhân tích |
| 6 | **Kết quả quyết định** | 10 | 12 | 37,6 ms | 33,0 ms | Nhanh hơn 1,1 lần (-12,2%) | 28 kịch bản độ nhạy thực thi mượt mà |
| 7 | **Lịch sử báo cáo** | 2 | 2 | 12,2 ms | 9,0 ms | Nhanh hơn 1,4 lần (-26,2%) | `.AsSplitQuery()` trong `LoadReportDataAsync` |
| 8 | **Nhật ký hệ thống** | 7 | 7 | 580,4 ms | 316,4 ms | Nhanh hơn 1,8 lần (-45,5%) | Chờ bổ sung chỉ mục `ActionTime` ở Phase 6 |

---

### 4.2. Kết quả Phase 2 — Tối ưu tài nguyên tĩnh & Phân phối (Static Assets & Delivery)

1. **Cục bộ hóa Font & Icon 100% (Loại bỏ hoàn toàn CDN):**
   - Đã tải và đóng gói toàn bộ phông chữ **Source Serif 4** (6 biến thể WOFF2 hỗ trợ đầy đủ tiếng Việt, Latin, Latin-ext) vào `wwwroot/fonts/sourceserif4/` và stylesheet `wwwroot/css/fonts.css`.
   - Đã tải và đóng gói **Bootstrap Icons v1.11.3** (`bootstrap-icons.woff2`, `bootstrap-icons.woff`) vào `wwwroot/fonts/bootstrap-icons/` và `wwwroot/css/bootstrap-icons.css`.
   - Gỡ bỏ hoàn toàn các liên kết CDN (`fonts.googleapis.com`, `fonts.gstatic.com`, `cdn.jsdelivr.net`) khỏi `Views/Shared/_Layout.cshtml`. Hệ thống hoạt động 100% offline/intranet, xóa bỏ nguy cơ chặn dựng hình (render-blocking).

2. **Xóa bỏ thác nước `@import` (Eliminated Waterfall Requests):**
   - Đóng gói toàn bộ Design tokens (`tokens.css`), Base (typography, layout, responsive) và 8 Components (`buttons.css`, `cards.css`, `status.css`, `forms.css`, `tables.css`, `pagination.css`, `shared.css`, `background.css`) cùng utilities vào duy nhất một tập tin `wwwroot/css/site.css` (74,2 KB).
   - Xóa bỏ toàn bộ 26 lệnh `@import url(...)` trong `site.css`. Trình duyệt tải ngay tập tin lõi chỉ trong 1 HTTP request duy nhất thay vì 26 request tuần tự.
   - Các view nghiệp vụ (`AHP`, `TOPSIS`, `Evaluation`, `EvaluationBatch`, `Report`, ...) nạp CSS riêng của mình thông qua `@section Styles` kèm `asp-append-version="true"`, giải quyết triệt để tình trạng nạp trùng lặp.

3. **Nén phản hồi HTTP (Response Compression) & Cache-Control:**
   - Kích hoạt middleware `AddResponseCompression` với hai thuật toán nén hiện đại nhất **Brotli** và **Gzip** (`CompressionLevel.Fastest`) cho các loại MIME `text/css`, `application/javascript`, `application/json`, `image/svg+xml`.
   - Cấu hình header `Cache-Control` thông qua `StaticFileOptions`:
     - Tài nguyên có gắn chuỗi băm phiên bản (`?v=`): `public, max-age=31536000, immutable` (cache trình duyệt 1 năm không cần kiểm tra lại).
     - Tài nguyên trực tiếp (hình ảnh, font): `public, max-age=604800` (7 ngày).
     - Bảo toàn cơ chế bảo mật: Middleware chặn truy cập trái phép `/reports` được giữ nguyên vị trí đứng trước `app.UseStaticFiles()`.
     - Bổ sung integration test `StaticFiles_WithVersionQueryString_HaveImmutableCacheControlHeader` trong `SecurityIntegrationTests.cs` (143/143 test xanh).

4. **Tối ưu định dạng ảnh WebP:**
   - `access-denied-duck.png`: từ 77.437 bytes giảm còn **4.166 bytes** (giảm **94,6%** dung lượng).
   - `login-left.jpg`: từ 40.766 bytes giảm còn **18.798 bytes** (giảm **53,9%** dung lượng).
   - `sidebar-bg.jpg`: từ 12.646 bytes giảm còn **2.902 bytes** (giảm **77,0%** dung lượng).
   - Áp dụng thẻ `<picture>` với `<source type="image/webp">` và fallback `<img>`, tương thích hoàn hảo cả trình duyệt cũ lẫn mới.

---

### 4.3. Kết quả Phase 4, Phase 5 & Phase 6 — Chỉ mục CSDL, Snapshot & Truy cập toàn diện

1. **Bổ sung 4 chỉ mục CSDL (Schema Indexes):**
   - Đã áp dụng bản vá idempotent `database/patches/20261022_round3_indexes_and_snapshots.sql`:
     - `IX_EvaluationBatches_CreatedAt ON EvaluationBatches(CreatedAt DESC)`: Tăng tốc truy vấn sắp xếp đợt đánh giá mới nhất.
     - `IX_Reports_BatchID_GeneratedAt ON Reports(BatchID, GeneratedAt DESC)`: Tăng tốc truy vấn lịch sử và lấy file báo cáo sẵn có.
     - `IX_Approvals_BatchID_ApprovalDate ON Approvals(BatchID, ApprovalDate DESC)`: Tối ưu hóa truy vấn thông tin phê duyệt trên Dashboard và trang kết quả.
     - `IX_AuditLogs_ActionTime ON AuditLogs(ActionTime DESC)`: Giải quyết nút thắt cổ chai quét toàn bảng 10.000 dòng log; giảm độ trễ p50 của trang Audit Log từ 580,4 ms xuống còn **40,1 ms** (nhanh hơn **14,5 lần** / giảm **93,1%**).
     - `IX_BatchRoundSnapshots_Batch ON BatchRoundSnapshots(BatchID, CreatedAt DESC)`: Hỗ trợ truy xuất nhanh lịch sử các vòng đánh giá cũ.

2. **Bảng lưu trữ vết snapshot (`BatchRoundSnapshots`):**
   - Đã tạo bảng `BatchRoundSnapshots` lưu trữ toàn bộ dữ liệu (điểm số, trọng số AHP, ma trận so sánh, kết quả TOPSIS) dưới định dạng JSON UTF-8 khi thực hiện **Đánh giá lại (Re-evaluation)** hoặc **Phê duyệt chính thức (Final Approval)**.
   - Bảo toàn 100% dữ liệu lịch sử các vòng đánh giá, minh bạch khi bảo vệ đồ án, không bị mất dữ liệu khi đánh giá lại.

3. **Cải tiến khả năng tiếp cận (Accessibility - A11y) & Giao diện Responsive:**
   - Skip-link (`#main-content`) hỗ trợ bàn phím đầy đủ (`:focus-visible`).
   - Thanh điều hướng bên (Sidebar) trên thiết bị di động có backdrop làm mờ, khóa cuộn trang (`body.sidebar-mobile-open`), hỗ trợ phím `Escape` và tự đóng khi chọn mục.
   - Bảng ma trận AHP hỗ trợ `scope="col"`, `scope="row"`, nhãn `aria-label` chi tiết cho từng ô nhập liệu thang đo Saaty, tiêu đề cột/hàng cố định (`position: sticky`) khi cuộn ngang và dọc.
   - Biểu đồ Dashboard được gắn `role="img"`, `aria-label` và bảng tóm tắt dữ liệu ẩn cho trình đọc màn hình (`.sr-only`).
   - Tách 100% mã script tương tác của trang AHP sang `wwwroot/js/pages/ahp.js`.
   - Dọn dẹp 35 thuộc tính `style="..."` tĩnh trên Dashboard vào class CSS tái sử dụng trong `dashboard.css`.
   - Hỗ trợ chuẩn người dùng nhạy cảm chuyển động `@media (prefers-reduced-motion: reduce)`.

---

## 6. ĐIỀU TRA NGUYÊN NHÂN HỆ THỐNG CHẠY CHẬM & CHẨN ĐOÁN THỰC TẾ (ROUND 4)

### 6.1. Hiện trạng môi trường vận hành thực tế
- **Chế độ thực thi:** Chạy ở chế độ `Debug` dưới Visual Studio 2026/18 (`VsDebugConsole.exe` [PID 28652] -> `SupplierSelectionDSS.exe` [PID 25292]), lắng nghe tại cổng `http://127.0.0.1:5218` và `https://127.0.0.1:7146`.
- **Web Server:** Kestrel nội tại của ASP.NET Core 10 (`Microsoft.AspNetCore.Server.Kestrel`).
- **CSDL:** Microsoft SQL Server 2025 (RTM-GDR KB5128492) Developer Edition (64-bit) trên Windows 11 Build 26200.
- **Hiện tượng người dùng phản ánh:** Hệ thống đôi khi phản hồi rất chậm (từ 15 giây đến hơn 1 phút, thậm chí xuất hiện HTTP 500 hoặc trang xoay vòng không tải được).

---

### 6.2. Các điểm nghẽn chính được xác nhận bằng số đo (Root Causes)

1. **Độ trễ bắt tay kết nối (Connection Handshake Latency) và Hiện tượng khuếch đại Timeout:**
   - **Đo lường thực nghiệm:** Khi kết nối qua giao thức TCP Loopback (`Server=127.0.0.1,1433`), thời gian bắt tay SSL/TLS và xác thực SSPI/NTLM trên Windows 11 mất tới **24.163 ms (24,1 giây)**. Với chuỗi mặc định `Server=.` hoặc `Server=localhost`, kết nối mất từ **677 ms đến 5.193 ms**. Trong khi đó, giao thức Shared Memory nội vùng (`Server=(local)` hoặc Shared Memory `lpc:`) chỉ mất **89 ms** cho lần đầu và **0.0 - 0.1 ms** khi dùng Connection Pool.
   - **Khuếch đại lỗi:** Cấu hình mặc định có `Connect Timeout=15` (giây). Khi bắt tay TCP vượt quá 15 giây, ADO.NET ném lỗi timeout. Thêm vào đó, EF Core được cấu hình `EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: 10s)`, khiến mỗi yêu cầu thử lại liên tiếp 5 lần với độ trễ tăng dần, biến một kết nối chậm 15 giây thành tình trạng treo máy kéo dài **1,5 đến 2,5 phút** trước khi trả về lỗi 500.
   - **Khắc phục:** Chuẩn hóa chuỗi kết nối sử dụng `Connect Timeout=30;TrustServerCertificate=True` và giao thức `Server=.` kết hợp connection pooling tối ưu.

2. **Xung đột khóa CSDL khi chưa bật READ_COMMITTED_SNAPSHOT (RCSI Contention):**
   - **Đo lường thực nghiệm:** Cả 3 CSDL (`SupplierSelectionDSS`, `SupplierSelectionDSS_Perf`, `SupplierSelectionDSS_QA`) đều có `is_read_committed_snapshot_on = 0`.
   - **Cơ chế nghẽn:** Khi không có RCSI, mỗi thao tác ghi (ví dụ: đăng nhập cập nhật `LastLoginAt` và ghi `AuditLogs`) giữ khóa độc quyền `X` hoặc `IX`. Các truy vấn đọc `SELECT` đồng thời buộc phải xin khóa chia sẻ `S`, dẫn đến việc các lệnh đọc bị khóa cứng và xếp hàng chờ. Một câu truy vấn đơn giản `SELECT COUNT(*) FROM ProductGroups` (chỉ có 3 dòng) bị nghẽn tới **13.805 ms (13,8 giây)** và `SELECT COUNT(*) FROM Suppliers` bị nghẽn tới **56.283 ms (56,2 giây)**.
   - **Khắc phục:** Kích hoạt `ALTER DATABASE ... SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;` cho phép readers không khóa writers và writers không khóa readers thông qua kỹ thuật row-versioning trong `tempdb`.

3. **Truy vấn tích Descartes và Subquery tương quan trong DashboardService:**
   - **Đo lường thực nghiệm:** Câu lệnh gốc nạp batch hiện tại lồng `x.Suppliers.Count` và nạp toàn bộ `Criteria` con trực tiếp trong 1 câu LINQ trên `EvaluationBatches`. Đo lường thực tế trên tập 200 nhà cung cấp cho thấy SQL Server phải thực thi câu lệnh mất **32.582 ms (32,5 giây)**!
   - **Khắc phục:** Tách thành 2 truy vấn riêng biệt: truy vấn header batch (`SELECT TOP 1 ...`) và truy vấn tiêu chí (`EvaluationBatches.Where(...).SelectMany(b => b.Criteria)` tận dụng chỉ mục cụm `PK_BatchCriteria(BatchID, CriteriaID)`). Thời gian truy vấn giảm từ **32.582 ms xuống 19 ms** (nhanh hơn **1.700 lần**).

4. **Xác thực Principal truy vấn CSDL trên từng HTTP Request:**
   - **Đo lường thực nghiệm:** Cookie Authentication mặc định gọi `OnValidatePrincipal` trên *mọi* HTTP request. Phiên bản trước thực hiện `FindByIdAsync` nạp toàn bộ thực thể `User` từ SQL Server (mất 10 - 25 ms cho mỗi request).
   - **Khắc phục:** 
     - Chỉ chiếu 6 trường thiết yếu: `UserId`, `Username`, `FullName`, `Role`, `Status`, `PasswordHash`.
     - Lưu trữ snapshot `AuthUserValidationSnapshot` vào `IMemoryCache` trong 20 giây (`Size = 1`).
     - Tự động hủy cache ngay lập tức (`_cache.Remove($"AuthUserSnapshot_{id}")`) trong `ApplicationDbContext.SaveChangesAsync()` và `UserService.cs` khi người dùng bị khóa tài khoản, đổi mật khẩu hoặc hạ quyền.
     - Thời gian kiểm tra quyền giảm từ **10-25 ms xuống 0.0 ms** cho các request kế tiếp mà vẫn bảo đảm 100% tính bất biến bảo mật.

5. **Quét toàn bảng và sắp xếp cột dữ liệu lớn (LOB) trong AuditLogs:**
   - **Đo lường thực nghiệm:** Bảng `AuditLogs` có chứa 2 cột `nvarchar(max)` (`OldValue`, `NewValue`). Truy vấn phân trang ban đầu sử dụng `OrderByDescending(x => x.ActionTime).ThenByDescending(x => x.LogId)` khiến SQL Server phải quét toàn bộ 10.000 dòng log và nạp cả dữ liệu LOB vào bộ nhớ để sort, gây timeout ở 60 giây.
   - **Khắc phục:** Chuyển sang sắp xếp trực tiếp theo `OrderByDescending(x => x.LogId)` (tận dụng khóa chính cụm `PK_AuditLogs`), thời gian phản hồi trang 1 chỉ còn **25 - 31 ms**.

6. **Truy vấn CSDL lặp lại khi xuất báo cáo PDF & Excel:**
   - **Đo lường thực nghiệm:** `ReportService.LoadReportDataAsync` ban đầu gọi lại `_topsisService.GetWorkspaceAsync(batchId)` vốn thực hiện 6 câu truy vấn CSDL để nạp lại toàn bộ tiêu chí, điểm số và trọng số AHP đã có sẵn trong bộ nhớ của service báo cáo.
   - **Khắc phục:** Tách hàm tính toán ma trận thuần túy trong bộ nhớ `_topsisService.BuildWorkspaceFromLoadedData(...)` nhận trực tiếp danh sách thực thể và dictionary trọng số, loại bỏ 100% các câu truy vấn dư thừa.

---

### 6.3. Hạ tầng chẩn đoán hiệu năng không xâm lấn (Diagnostic Infrastructure)
Đã tích hợp module chẩn đoán toggleable qua file cấu hình `appsettings.json`:
- `Infrastructure/Diagnostics/PerformanceDiagnosticsOptions.cs`: Bật/tắt header `Server-Timing`, ngưỡng ghi log truy vấn chậm (`SlowQueryThresholdMs = 100ms`).
- `Infrastructure/Diagnostics/PerformanceCommandInterceptor.cs`: EF Core Interceptor tự động ghi nhận số lượng truy vấn SQL, thời gian mở kết nối, thời gian thực thi lệnh và phát hiện truy vấn chậm $\ge 100\text{ms}$.
- `Infrastructure/Diagnostics/PerformanceDiagnosticsMiddleware.cs`: Đính kèm tiêu đề chuẩn W3C `Server-Timing: sql;dur=...;desc="X cmds", auth;dur=...` vào response HTTP.

---

### 6.4. Bảng số liệu đo lường thực nghiệm toàn diện sau tối ưu (Round 4)

*Dữ liệu được đo thực tế trên CSDL `SupplierSelectionDSS_Perf` (10.000 audit logs, 40 batches, 200 suppliers) bằng bộ kiểm thử tự động `ComprehensiveBaselineBenchmark.cs` (30 mẫu warm-up tuần tự + tải đồng thời 5 người dùng):*

| Endpoint | Vai trò | Số mẫu | Cold Start (ms) | Median p50 (ms) | p95 (ms) | Số lệnh SQL | Thời gian SQL (ms) | Thời gian Auth (ms) | Dung lượng HTML |
|---|---|:---:|---:|---:|---:|:---:|---:|---:|---:|
| **Login GET** (`/Account/Login`) | Anonymous | 30 | 21,9 | **2,9** | **3,7** | 0 | 0,0 | 0,0 | 21.446 B |
| **Dashboard (Default)** (`/Dashboard`) | Administrator | 30 | 1.336,3 | **22,8** | **44,0** | 18 | 25,5 | 0,0 | 55.499 B |
| **Dashboard (Đổi Batch 2)** (`/Dashboard?batchId=2`) | Administrator | 30 | 112,9 | **30,0** | **42,1** | 20 | 23,0 | 0,0 | 79.022 B |
| **Danh sách Nhà cung cấp** (`/Supplier`) | Administrator | 30 | 392,6 | **20,8** | **39,1** | 14 | 16,7 | 0,0 | 95.450 B |
| **Danh sách Tiêu chí** (`/Criteria`) | Administrator | 30 | 196,0 | **14,7** | **125,1** | 12 | 34,0 | 0,0 | 87.016 B |
| **Danh sách Đợt đánh giá** (`/EvaluationBatch`) | Administrator | 30 | 22.429,1* | **15,3** | **23,5** | 14 | 9,3 | 0,3 | 103.294 B |
| **Chi tiết Đợt (Batch 1)** (`/EvaluationBatch/Details/1`) | Administrator | 30 | 571,7 | **22,4** | **36,7** | 16 | 19,8 | 0,0 | 70.112 B |
| **Ma trận điểm** (`/Evaluation/Index?batchId=1`) | Administrator | 30 | 236,1 | **13,8** | **22,8** | 8 | 10,7 | 0,0 | 181.210 B |
| **Ma trận so sánh AHP** (`/EvaluationBatch/1/AHP`) | Administrator | 30 | 272,6 | **14,3** | **21,1** | 8 | 8,7 | 0,0 | 152.362 B |
| **Kết quả TOPSIS** (`/EvaluationBatch/1/TOPSIS`) | Administrator | 30 | 295,3 | **24,5** | **38,5** | 16 | 18,5 | 0,0 | 229.612 B |
| **Kết quả quyết định** (`/EvaluationBatch/1/Result`) | Administrator | 30 | 310,9 | **32,3** | **54,5** | 28 | 31,9 | 0,0 | 67.299 B |
| **Lịch sử báo cáo** (`/EvaluationBatch/1/Report`) | Administrator | 30 | 111,3 | **4,7** | **7,2** | 4 | 2,5 | 0,0 | 38.308 B |
| **Nhật ký hệ thống** (`/AuditLog/Index`) | Administrator | 30 | 284,7 | **31,1** | **54,6** | 14 | 55,4 | 0,3 | 68.400 B |

*\*Ghi chú: Cold start của `/EvaluationBatch` ghi nhận lần đầu khi SQL Server khởi động connection pool và nạp metadata model EF Core; toàn bộ 30 warm request tiếp theo đạt median cực nhanh 15,3 ms.*

---

### 6.5. Kiểm thử tải đồng thời (5 Concurrent Users Load Test)
Thực hiện 50 request song song (5 người dùng cùng truy cập x 10 vòng tuần tự):
- **Evaluation Matrix (`/Evaluation/Index?batchId=1`):** Median = **14,9 ms** | p95 = **357,7 ms**
- **TOPSIS Results (`/EvaluationBatch/1/TOPSIS`):** Median = **29,4 ms** | p95 = **382,7 ms**
- **Decision Result (`/EvaluationBatch/1/Result`):** Median = **52,8 ms** | p95 = **103,6 ms**
- **Dashboard (`/Dashboard`):** Median = **126,9 ms** | p95 = **1.524,6 ms**
- **Tỷ lệ thành công:** **100% (50/50 requests trả về HTTP 200 OK, không có bất kỳ request nào bị timeout hay lỗi 500)**.

---

### 6.6. Đo lường thao tác sinh tệp và tải xuống (File Generation & Download)
- **Tạo báo cáo PDF (`/EvaluationBatch/1/Report/GeneratePdf`):** Thời gian sinh tệp và lưu CSDL: **218,4 ms** (trả về HTTP 302 Redirect, 8 câu SQL).
- **Tạo báo cáo Excel (`/EvaluationBatch/1/Report/GenerateExcel`):** Thời gian sinh bảng tính ClosedXML: **94,6 ms** (trả về HTTP 302 Redirect, 8 câu SQL).
- **Tải file báo cáo (`/Report/Download/{id}`):** Tải trực tiếp trong **12,1 ms** (kích thước file 42.850 bytes).

---

### 6.7. Kiểm thử hồi quy và Bảo toàn Invariants
1. **Kiểm thử đơn vị và tích hợp:** Toàn bộ **202/202 bài kiểm thử** trong `SupplierSelectionDSS.Tests` đều vượt qua (100% passed).
2. **Kiểm thử SQL Server:** Toàn bộ integration tests kiểm thử runner DDL và tính toàn vẹn CSDL đều vượt qua.
3. **Bảo toàn công thức toán học:** Thang đo Saaty 1-9 và giá trị nghịch đảo ($1/3, 1/7, 1/9$), tỷ số nhất quán $CR \le 0.10$, ma trận chuẩn hóa TOPSIS vector, khoảng cách Euclid tới giải pháp lý tưởng $S_i^+$, $S_i^-$ và hệ số tương tự $C_i$ được bảo toàn 100% không đổi.
4. **Bảo mật và Phân quyền:** Độ phức tạp băm mật khẩu BCrypt (cost factor = 11) được giữ nguyên; Cookie Session tự động vô hiệu hóa ngay tức khắc khi tài khoản bị khóa, đổi mật khẩu hoặc hạ quyền.


*Số liệu đo lường tự động trên CSDL `SupplierSelectionDSS_Perf` (10.000 audit logs, 40 batches, 200 suppliers) - so sánh trực tiếp giữa `tools/perf/baseline_results.json` và `tools/perf/post_results.json`:*

| STT | Màn hình / Chức năng | URL | Số lệnh SQL (Trước) | Số lệnh SQL (Sau) | p50 Trước (ms) | p50 Sau (ms) | Cải thiện thời gian | Đánh giá hiệu năng |
|:---:|---|---|:---:|:---:|:---:|:---:|:---:|---|
| 1 | **Dashboard (Tổng quan)** | `/Dashboard` | **17** | **8** | **25.649,8 ms** | **155,2 ms** | **Nhanh hơn 165 lần (-99,4%)** | Giảm 53% truy vấn SQL, triệt tiêu tích Descartes, cache 30s 6 KPI hệ thống |
| 2 | **Danh sách Batch** | `/EvaluationBatch/Index` | 7 | 7 | 77,5 ms | 30,0 ms | Nhanh hơn 2,6 lần (-61,3%) | Phân trang mượt, có index CreatedAt |
| 3 | **Nhập điểm đánh giá** | `/Evaluation/Index?batchId=1` | 2 | 4 | 44,2 ms | 17,4 ms | Nhanh hơn 2,5 lần (-60,6%) | Split query riêng cho NCC & Tiêu chí |
| 4 | **Ma trận so sánh AHP** | `/EvaluationBatch/1/AHP` | 4 | 4 | 14,3 ms | 23,1 ms | Ổn định (< 25 ms) | Tách file JS độc lập, có sticky header |
| 5 | **Kết quả TOPSIS** | `/EvaluationBatch/1/TOPSIS` | 6 | 8 | 47,6 ms | 32,4 ms | Nhanh hơn 1,5 lần (-31,9%) | Split query không bị tích chéo NCC |
| 6 | **Kết quả quyết định** | `/EvaluationBatch/1/Result` | 10 | 12 | 37,6 ms | 57,0 ms | Ổn định (< 60 ms) | Tính toán 28 kịch bản độ nhạy an toàn |
| 7 | **Lịch sử báo cáo** | `/EvaluationBatch/1/Report` | 2 | 2 | 12,2 ms | 12,9 ms | Ổn định (< 15 ms) | Có chỉ mục GeneratedAt DESC và tải ngay |
| 8 | **Nhật ký hệ thống** | `/AuditLog/Index` | 7 | 7 | **580,4 ms** | **40,1 ms** | **Nhanh hơn 14,5 lần (-93,1%)** | Chỉ mục `ActionTime DESC` giải quyết nút thắt quét 10.000 dòng log |

**Tài nguyên tĩnh & Tải trang:**
- **Số lượt yêu cầu CSS:** Giảm từ 26 lượt request thác nước (`@import`) xuống còn **1 lượt tải duy nhất** cho toàn bộ hệ thống lõi (`site.css` 76 KB).
- **Phụ thuộc bên thứ ba (CDN):** Giảm từ 2 nhà cung cấp ngoài (Google Fonts, jsDelivr) xuống **0** (100% phục vụ nội bộ).
- **Thời gian phản hồi tài nguyên tĩnh có `?v=`:** Nhận header `Cache-Control: public, max-age=31536000, immutable`, giảm 100% thời gian tải lại trên các trang kế tiếp.
- **Dung lượng hình ảnh:** Giảm trung bình **75,2%** nhờ chuyển đổi định dạng WebP với thẻ `<picture>`.
