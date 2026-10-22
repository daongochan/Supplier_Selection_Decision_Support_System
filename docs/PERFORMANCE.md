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
3. Xem kết quả chi tiết xuất ra tại `tools/perf/baseline_results.json`.

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

## 4. BẢNG ĐỐI CHIẾU TRƯỚC / SAU (SẼ ĐƯỢC ĐIỀN SAU CÁC PHASE TỐI ƯU)

| STT | Màn hình | Số lệnh SQL (Trước) | Số lệnh SQL (Sau) | p50 Trước (ms) | p50 Sau (ms) | Giảm thời gian | Dung lượng Trước | Dung lượng Sau |
|:---:|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | Dashboard | 17 | *(Đang tối ưu)* | 25.649,8 ms | *(Đang tối ưu)* | - | 52.923 B | - |
| 2 | Danh sách Batch | 7 | *(Đang tối ưu)* | 77,5 ms | *(Đang tối ưu)* | - | 102.408 B | - |
| 3 | Nhập điểm đánh giá | 2 | *(Đang tối ưu)* | 44,2 ms | *(Đang tối ưu)* | - | 178.149 B | - |
| 4 | Ma trận AHP | 4 | *(Đang tối ưu)* | 14,3 ms | *(Đang tối ưu)* | - | 94.702 B | - |
| 5 | Kết quả TOPSIS | 6 | *(Đang tối ưu)* | 47,6 ms | *(Đang tối ưu)* | - | 243.276 B | - |
| 6 | Kết quả quyết định | 10 | *(Đang tối ưu)* | 37,6 ms | *(Đang tối ưu)* | - | 59.776 B | - |
| 7 | Lịch sử báo cáo | 2 | *(Đang tối ưu)* | 12,2 ms | *(Đang tối ưu)* | - | 36.490 B | - |
| 8 | Nhật ký hệ thống | 7 | *(Đang tối ưu)* | 580,4 ms | *(Đang tối ưu)* | - | 67.752 B | - |
