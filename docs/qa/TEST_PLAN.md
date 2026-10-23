# KẾ HOẠCH KIỂM THỬ TOÀN DIỆN (END-TO-END QA TEST PLAN)
**Dự án:** Hệ thống Hỗ trợ Ra Quyết định Chọn Nhà Cung Cấp (Supplier Selection Decision Support System - AHP + TOPSIS)  
**Phiên bản nền tảng:** .NET 10 (C# 14), ASP.NET Core MVC, Entity Framework Core 10, SQL Server 2022  
**Thư viện nghiệp vụ:** QuestPDF 2025.7, ClosedXML 0.105, Chart.js 4.4.7 UMD  
**Cơ sở dữ liệu QA biệt lập:** `SupplierSelectionDSS_QA` (Tránh mọi tác động phá hủy lên DB chính)  
**Nhánh Git:** `fix/qa-full-sweep`  
**Ngày lập:** 23/10/2026  

---

## 1. MỤC TIÊU & NGUYÊN TẮC KIỂM THỬ

### 1.1. Mục tiêu tối thượng
1. **Tiệt trừ triệt để 100% lỗi Màn hình trắng (Blank Screen - P0):** Mọi URL, mọi trạng thái lỗi (404, 403, 400, 429, 500), mọi thao tác người dùng đều phải trả về giao diện HTML hoàn chỉnh, tối thiểu 200 bytes, có header/footer/nội dung rõ ràng, thông báo bằng tiếng Việt và có hướng dẫn điều hướng quay về an toàn.
2. **Kiểm thử xuyên suốt 14 nhóm chức năng (Nhóm A $\to$ Nhóm N):** Bao phủ toàn bộ luồng nghiệp vụ từ quản lý danh mục, thiết lập tiêu chí, nhập bảng điểm, tính toán ma trận AHP, xếp hạng TOPSIS, phân tích độ nhạy, phê duyệt quyết định, xuất báo cáo PDF/Excel đến các yêu cầu phi chức năng (bảo mật, trigger toàn vẹn, hiệu năng).
3. **Tuân thủ nguyên tắc "Không phỏng đoán - Chứng minh bằng kiểm thử":** Mọi kết luận đều phải dựa trên log thực thi, kết quả xUnit và crawler tự động.

### 1.2. Ràng buộc bất di bất dịch (Guiding Rules)
- **UI & Thông báo:** 100% tiếng Việt chuẩn mực, chuyên nghiệp cho hệ thống DSS doanh nghiệp.
- **Tên biến, hàm, cơ sở dữ liệu, commit:** 100% tiếng Anh chuẩn mực, tuân theo Conventional Commits.
- **Không thay đổi công thức toán học:** Giữ nguyên logic tính toán trong `AHPCalculationService`, `TOPSISCalculationService`, `SensitivityAnalysisService` trừ khi có bằng chứng sai lệch từ `tools/verify/verify_mcdm.py` hoặc xUnit.
- **Không chỉnh sửa thư viện bên thứ ba:** Giữ nguyên vẹn mã nguồn trong `wwwroot/lib/`.
- **An toàn dữ liệu:** Nghiêm cấm chạy câu lệnh DDL/DML phá hủy (DROP, TRUNCATE, DELETE bừa bãi) trên cơ sở dữ liệu sản xuất.

---

## 2. PHẠM VI 14 NHÓM KIỂM THỬ (TEST GROUPS A $\to$ N)

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           14 NHÓM KIỂM THỬ TOÀN DIỆN                           │
├─────────────────────┬───────────────────────────┬───────────────────────────────┤
│ Nhóm A: Auth & Ses  │ Nhóm B: RBAC & Quyền hạn  │ Nhóm C: Dashboard & KPIs      │
├─────────────────────┼───────────────────────────┼───────────────────────────────┤
│ Nhóm D: Nhóm SP     │ Nhóm E: Tiêu chí (AHP)    │ Nhóm F: Nhà cung cấp          │
├─────────────────────┼───────────────────────────┼───────────────────────────────┤
│ Nhóm G: Đợt ĐG      │ Nhóm H: Bảng điểm ĐG      │ Nhóm I: Thuật toán AHP        │
├─────────────────────┼───────────────────────────┼───────────────────────────────┤
│ Nhóm J: TOPSIS      │ Nhóm K: Độ nhạy           │ Nhóm L: Luồng Quyết định      │
├─────────────────────┼───────────────────────────┼───────────────────────────────┤
│ Nhóm M: Báo cáo PDF │ Nhóm N: Bảo mật & An toàn │                               │
└─────────────────────┴───────────────────────────┴───────────────────────────────┘
```

### Nhóm A: Xác thực & Quản lý Phiên (Authentication & Session)
- **Mục tiêu:** Kiểm tra quy trình đăng nhập, đăng xuất, lưu cookie, xử lý hết hạn phiên và chống brute-force.
- **Nội dung kiểm thử:**
  - Đăng nhập đúng thông tin tài khoản (Admin, Manager, ProcurementOfficer, WarehouseStaff).
  - Đăng nhập sai mật khẩu $\to$ cảnh báo tiếng Việt, tăng bộ đếm thất bại.
  - Khóa tài khoản sau 5 lần đăng nhập sai liên tiếp trong 15 phút (`LoginAttemptService`).
  - Rate limiting tại endpoint login (`LoginLimiter` 15 req/phút) $\to$ HTTP 429 kèm trang thông báo thân thiện.
  - Thay đổi mật khẩu người dùng $\to$ hủy hiệu lực cookie của phiên cũ thông qua `PasswordStamp` validator.
  - Phiên làm việc hết hạn / Cookie bị xóa $\to$ tự động chuyển hướng về `/Account/Login`.

### Nhóm B: Phân quyền & Kiểm soát Truy cập (Authorization & RBAC)
- **Mục tiêu:** Đảm bảo nguyên tắc đặc quyền tối thiểu (Least Privilege) trên 4 vai trò nghiệp vụ.
- **Nội dung kiểm thử:**
  - `Admin`: Toàn quyền quản trị tài khoản người dùng, cấu hình nhóm sản phẩm, tiêu chí.
  - `Manager`: Phê duyệt đợt đánh giá, xem dashboard, báo cáo, thực hiện AHP/TOPSIS.
  - `ProcurementOfficer`: Tạo đợt đánh giá, nhập bảng điểm, khởi chạy AHP/TOPSIS, tạo báo cáo.
  - `WarehouseStaff`: Chỉ có quyền xem danh sách nhà cung cấp, thông tin sản phẩm và lịch sử đánh giá (Read-only).
  - Người dùng ẩn danh (Anonymous): Không thể truy cập các trang nội bộ, tự động redirect về trang đăng nhập với `ReturnUrl`.
  - Truy cập trái phép (403 Forbidden): Không trả về trang trắng; hiển thị trang lỗi 403 giải thích lý do không có quyền.

### Nhóm C: Dashboard & Thẻ Chỉ Số KPI (Dashboard & Metrics)
- **Mục tiêu:** Hiển thị chính xác các chỉ số tổng hợp, biểu đồ tiến độ và liên kết thao tác nhanh.
- **Nội dung kiểm thử:**
  - Tổng số nhà cung cấp, tổng số đợt đánh giá, số đợt đang thực hiện, số quyết định đã duyệt.
  - Biểu đồ phân bổ trạng thái đợt đánh giá qua Chart.js 4.4.7 local UMD.
  - Trạng thái đợt đánh giá gần nhất (Banner tiến độ 5 bước).
  - Khối thông tin phê duyệt: Sửa triệt để liên kết chết `ApprovalController` $\to$ chuyển hướng đúng sang `DecisionResultController.Index`.

### Nhóm D: Quản lý Nhóm Sản phẩm (Product Groups Management)
- **Mục tiêu:** Quản lý vòng đời nhóm sản phẩm và tính toàn vẹn khóa ngoại.
- **Nội dung kiểm thử:**
  - Thêm mới nhóm sản phẩm với mã không trùng lặp, tên hợp lệ.
  - Chỉnh sửa thông tin nhóm sản phẩm.
  - Vô hiệu hóa (Deactivate) / Xóa logic: Đảm bảo không xóa vật lý các nhóm sản phẩm đang có tiêu chí hoặc đợt đánh giá gắn liền (chặn bởi trigger/khóa ngoại).

### Nhóm E: Quản lý Tiêu chí Đánh giá (Criteria Management)
- **Mục tiêu:** Cấu hình hệ thống tiêu chí Benefit/Cost cho bài toán AHP và TOPSIS.
- **Nội dung kiểm thử:**
  - Tạo tiêu chí với mã `CriteriaCode` duy nhất, loại tiêu chí (`Benefit` hoặc `Cost`).
  - Kiểm tra đơn vị đo lường, phạm vi giá trị tối thiểu/tối đa.
  - Ràng buộc sửa đổi: Tiêu chí đã nằm trong đợt đánh giá đã chốt không được phép chỉnh sửa loại tiêu chí.

### Nhóm F: Quản lý Nhà Cung Cấp (Suppliers Management)
- **Mục tiêu:** Quản lý hồ sơ nhà cung cấp, thông tin liên lạc, mã số thuế và trạng thái.
- **Nội dung kiểm thử:**
  - Thêm mới nhà cung cấp với mã số thuế, email, số điện thoại hợp lệ.
  - Cập nhật thông tin nhà cung cấp.
  - Chuyển trạng thái nhà cung cấp (`Active`, `Inactive`, `Blacklisted`).
  - Bộ lọc tìm kiếm nhà cung cấp theo từ khóa, trạng thái, nhóm sản phẩm.

### Nhóm G: Quản lý Đợt Đánh Giá (Evaluation Batches & State Machine)
- **Mục tiêu:** Kiểm tra máy trạng thái của đợt đánh giá qua 4 trạng thái: `Draft` $\to$ `Evaluating` $\to$ `Completed` (hoặc `Cancelled`).
- **Nội dung kiểm thử:**
  - Tạo mới đợt đánh giá ở trạng thái `Draft`, gán nhóm sản phẩm, chọn danh sách nhà cung cấp và danh sách tiêu chí.
  - Chuyển trạng thái sang `Evaluating` để mở khóa chức năng nhập điểm.
  - Kiểm soát trigger cơ sở dữ liệu: Ngăn chặn xóa hoặc sửa đợt đánh giá khi đã hoàn tất quyết định.
  - Xử lý khi đợt đánh giá bị hủy (`Cancelled`).

### Nhóm H: Nhập Bảng Điểm Đánh Giá (Evaluation Scoring Matrix)
- **Mục tiêu:** Ghi nhận và thẩm định bảng điểm cho từng nhà cung cấp theo từng tiêu chí.
- **Nội dung kiểm thử:**
  - Hiển thị ma trận $m$ nhà cung cấp $\times$ $n$ tiêu chí.
  - Kiểm tra ràng buộc giá trị: Không được để trống, không được nhập số âm cho tiêu chí chi phí, giá trị trong giới hạn quy định.
  - Hỗ trợ nhập liệu số thập phân linh hoạt (chấp nhận cả dấu chấm `.` và dấu phẩy `,` trong môi trường tiếng Việt).
  - Tự động kiểm tra tính đầy đủ của ma trận điểm trước khi cho phép chạy thuật toán TOPSIS.

### Nhóm I: Phân Tích Ma Trận So Sánh Cặp AHP (AHP Module)
- **Mục tiêu:** Tính toán trọng số tiêu chí bằng phương pháp Phân tích Thứ bậc (AHP).
- **Nội dung kiểm thử:**
  - Ma trận so sánh cặp $n \times n$ với đường chéo chính luôn bằng 1.
  - Tính đối xứng nghịch đảo: $a_{ji} = 1 / a_{ij}$.
  - Giao diện nhập liệu trực quan: cho phép nhập cả giá trị nguyên (1-9) và phân số nghịch đảo (1/2, 1/3, ..., 1/9) một cách dễ dàng, không bắt người dùng tính nhẩm số lẻ.
  - Tính toán $\lambda_{\max}$, Chỉ số Nhất quán $CI = \frac{\lambda_{\max} - n}{n - 1}$, và Tỷ số Nhất quán $CR = \frac{CI}{RI}$.
  - Kiểm tra điều kiện nhất quán: Nếu $CR \le 0.1$, đánh dấu ma trận hợp lệ và lưu trọng số; nếu $CR > 0.1$, cảnh báo đỏ bằng tiếng Việt và yêu cầu người dùng đánh giá lại các cặp tiêu chí mâu thuẫn.

### Nhóm J: Xếp Hạng Đa Tiêu Chí TOPSIS (TOPSIS Ranking Module)
- **Mục tiêu:** Chuẩn hóa ma trận quyết định, xác định giải pháp lý tưởng và xếp hạng nhà cung cấp.
- **Nội dung kiểm thử:**
  - Chuẩn hóa ma trận điểm theo phương pháp Vector Normalization: $r_{ij} = \frac{x_{ij}}{\sqrt{\sum x_{kj}^2}}$.
  - Nhân trọng số từ AHP: $v_{ij} = w_j \times r_{ij}$.
  - Xác định Giải pháp Lý tưởng Dương (PIS / $A^+$) và Giải pháp Lý tưởng Âm (NIS / $A^-$) riêng biệt cho tiêu chí Benefit và Cost.
  - Tính khoảng cách Euclid $D_i^+$ và $D_i^-$.
  - Tính hệ số tương tự gần gũi $C_i = \frac{D_i^-}{D_i^+ + D_i^-}$ nằm trong khoảng $[0, 1]$.
  - Xếp hạng giảm dần theo $C_i$ (nhà cung cấp có $C_i$ cao nhất xếp hạng 1).

### Nhóm K: Phân Tích Độ Nhạy (Sensitivity Analysis Module)
- **Mục tiêu:** Kiểm tra độ bền vững của kết quả xếp hạng khi trọng số các tiêu chí biến thiên.
- **Nội dung kiểm thử:**
  - Thay đổi trọng số của từng tiêu chí trong khoảng $[-50\%, +50\%]$ và chuẩn hóa lại các trọng số còn lại.
  - Vẽ biểu đồ đường biến thiên hệ số $C_i$ theo sự thay đổi trọng số.
  - Xác định điểm đảo vị thế (Rank Reversal) nếu có và đưa ra cảnh báo độ nhạy cho người ra quyết định.

### Nhóm L: Quy Trình Phê Duyệt & Chốt Quyết Định (Decision Workflow)
- **Mục tiêu:** Quy trình phê duyệt minh bạch, phân cấp thẩm quyền và ghi nhận lịch sử.
- **Nội dung kiểm thử:**
  - Chuyên viên mua sắm (Procurement Officer) gửi kết quả đề xuất nhà cung cấp lên Quản lý (Manager).
  - Quản lý duyệt chấp thuận (`Approve`) kèm ghi chú lý do $\to$ Trạng thái đợt đánh giá chuyển sang `Completed`.
  - Quản lý từ chối (`Reject`) hoặc yêu cầu đánh giá lại (`Reevaluate`) kèm phản hồi cụ thể $\to$ Đợt chuyển về trạng thái cần điều chỉnh.
  - Kích hoạt trigger SQL Server: Sau khi đã duyệt (`Approved`), toàn bộ dữ liệu điểm số, so sánh AHP, kết quả TOPSIS và quyết định bị khóa vĩnh viễn (Read-only, không thể UPDATE/DELETE).

### Nhóm M: Xuất Báo Cáo Chuyên Nghiệp (Reporting Engine)
- **Mục tiêu:** Xuất bản tài liệu hồ sơ lựa chọn nhà cung cấp dưới định dạng PDF và Excel.
- **Nội dung kiểm thử:**
  - Báo cáo PDF (QuestPDF): Thiết kế trang trọng, đầy đủ bảng tiêu chí, trọng số AHP, bảng điểm gốc, kết quả TOPSIS, kết luận đề xuất và khu vực chữ ký các bên.
  - Báo cáo Excel (ClosedXML): Bảng tính đầy đủ công thức, định dạng số, màu sắc rõ ràng phục vụ lưu trữ kế toán và kiểm toán.
  - Bảo mật tài nguyên: Chặn tuyệt đối hành vi duyệt file tĩnh trực tiếp từ thư mục `/reports/` trên web server; bắt buộc phải qua endpoint nghiệp vụ kiểm tra quyền.

### Nhóm N: Kiểm Soát Toàn Vẹn & Phi Chức Năng (Cross-cutting & Non-functional)
- **Mục tiêu:** Độ tin cậy, an toàn thông tin và tính bền bỉ của hệ thống.
- **Nội dung kiểm thử:**
  - Kiểm tra tính toàn vẹn 7 trigger SQL Server kiểm soát quy tắc nghiệp vụ.
  - Kiểm tra cơ chế chống tấn công CSRF trên toàn bộ form POST (`AutoValidateAntiforgeryToken`).
  - Kiểm tra mã hóa HTML chống XSS trên các trường nhập văn bản của người dùng.
  - Kiểm tra thời gian phản hồi: các trang hiển thị dữ liệu $< 300\text{ms}$.
  - Xác minh hiển thị không lỗi font, không vỡ layout trên các độ phân giải màn hình từ Desktop (1920x1080) đến Laptop (1366x768).

---

## 3. LỊCH TRÌNH VÀ PHƯƠNG PHÁP TRIỂN KHAI

| Giai đoạn | Nhiệm vụ chính | Sản phẩm bàn giao | Phương tiện nghiệm thu |
| :--- | :--- | :--- | :--- |
| **Giai đoạn 1** | Khảo sát màn hình, cấu trúc route & role | `docs/qa/SCREEN_INVENTORY.md` | Hoàn thành |
| **Giai đoạn 2** | Quét tự động crawler, cô lập lỗi màn hình trắng, lập kế hoạch & ma trận test | `docs/qa/BLANK_SCREEN_REPORT.md`<br>`docs/qa/TEST_PLAN.md`<br>`docs/qa/TEST_MATRIX.md` | Hoàn thành |
| **Giai đoạn 3** | Sửa triệt để mã nguồn, sửa views, cấu hình pipeline, bổ sung CSS tokens, viết test tự động | Mã nguồn đã sửa<br>`RouteSmokeAndBlankScreenSweepTests.cs` | `dotnet build`<br>`dotnet test`<br>`crawler_test.py` |
| **Giai đoạn 4** | Kiểm thử hồi quy, xác nhận 0 blank screen, đối chiếu AHP/TOPSIS | `docs/qa/EVIDENCE.md`<br>`CHANGELOG.md` | 100% tests Passed, MCDM parity verified |
