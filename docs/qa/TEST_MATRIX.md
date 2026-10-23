# MA TRẬN KIỂM THỬ CHI TIẾT (END-TO-END QA TEST MATRIX)
**Dự án:** Supplier Selection Decision Support System (AHP + TOPSIS)  
**Tập trung:** Kiểm thử toàn diện 14 nhóm chức năng, phòng chống lỗi Màn hình trắng (P0)  
**Ngày cập nhật:** 23/10/2026  

---

## BẢNG MA TRẬN CÁC TEST CASES

| ID | Nhóm | Tên Test Case | Điều kiện tiên quyết | Dữ liệu đầu vào / Thao tác | Kết quả mong đợi | Loại kiểm thử |
| :--- | :--- | :--- | :--- | :--- | :--- | :---: |
| **TC-A01** | Auth | Đăng nhập hợp lệ | Tài khoản Active trong DB | Nhập đúng Username & Password | Đăng nhập thành công, cấp cookie auth, chuyển hướng về Dashboard | E2E / Integration |
| **TC-A02** | Auth | Đăng nhập sai mật khẩu | Tài khoản tồn tại | Nhập sai Password | Báo lỗi tiếng Việt: "Sai tên đăng nhập hoặc mật khẩu.", tăng số lần sai | Integration |
| **TC-A03** | Auth | Khóa tài khoản sau 5 lần sai | Tài khoản tồn tại | Đăng nhập sai 5 lần liên tiếp | Báo lỗi khóa tài khoản tạm thời 15 phút, chặn đăng nhập tiếp theo | Unit / Integration |
| **TC-A04** | Auth | Đăng xuất người dùng | Đã đăng nhập | POST tới `/Account/Logout` | Xóa Auth cookie, chuyển hướng về `/Account/Login` | E2E |
| **TC-A05** | Auth | Đổi mật khẩu hủy cookie cũ | Người dùng có 2 phiên mở | Đổi mật khẩu ở phiên 1 | Phiên 2 khi gửi request tiếp theo bị từ chối do PasswordStamp không khớp | Integration |
| **TC-A06** | Auth | Rate limiting đăng nhập (HTTP 429) | Không cần login | Gửi > 15 requests trong 1 phút | Trả về mã HTTP 429 kèm trang lỗi tiếng Việt, không bị màn hình trắng | Integration |
| **TC-B01** | RBAC | Admin truy cập Quản trị User | Login vai trò Admin | Truy cập `/User` | Hiển thị danh sách người dùng, các nút Thêm, Sửa, Đổi trạng thái | E2E |
| **TC-B02** | RBAC | Procurement truy cập trái phép User | Login ProcurementOfficer | Truy cập `/User` | Chuyển hướng về `/Account/AccessDenied` hoặc trả về trang lỗi 403 tiếng Việt | Integration |
| **TC-B03** | RBAC | Anonymous truy cập trang nội bộ | Chưa đăng nhập | Truy cập `/Dashboard` | Tự động chuyển hướng về `/Account/Login?ReturnUrl=%2FDashboard` | Integration |
| **TC-B04** | RBAC | WarehouseStaff chỉ xem (Read-only) | Login WarehouseStaff | Truy cập `/Supplier` | Xem được danh sách nhà cung cấp nhưng không có nút "Thêm mới" / "Sửa" | E2E |
| **TC-C01** | Dashboard | Hiển thị 4 thẻ KPI chính | Đã đăng nhập | Xem trang chủ `/Dashboard` | Hiển thị đúng số lượng NCC, Đợt đánh giá, Đợt đang chạy, Quyết định duyệt | E2E |
| **TC-C02** | Dashboard | Biểu đồ phân bổ trạng thái | Có ít nhất 1 đợt ĐG | Kiểm tra thẻ biểu đồ Chart.js | Biểu đồ Donut hiển thị đúng số liệu, không bị lỗi console canvas | Frontend |
| **TC-C03** | Dashboard | Click nút Phê duyệt trên Dashboard | Login vai trò Manager | Click "Submit for Approval" | Chuyển hướng chính xác tới `/DecisionResult?batchId=...`, KHÔNG bị lỗi 404 trắng màn hình | E2E / Fix P0 |
| **TC-D01** | ProductGroup | Danh sách nhóm sản phẩm | Có dữ liệu nhóm SP | Truy cập `/ProductGroup` | Hiển thị bảng danh sách, phân trang, thanh tìm kiếm | E2E |
| **TC-D02** | ProductGroup | Thêm mới nhóm sản phẩm hợp lệ | Login Admin/Manager | Nhập Mã SP duy nhất, Tên nhóm SP | Thêm thành công vào DB, có thông báo tiếng Việt | E2E |
| **TC-D03** | ProductGroup | Thêm trùng mã nhóm sản phẩm | Nhóm SP đã tồn tại mã | Nhập trùng GroupCode | Báo lỗi validation tiếng Việt: "Mã nhóm sản phẩm đã tồn tại" | Unit / Integration |
| **TC-E01** | Criteria | Danh sách tiêu chí theo nhóm | Đã chọn nhóm SP | Truy cập `/Criteria?groupId=1` | Hiển thị danh sách tiêu chí Benefit/Cost tương ứng | E2E |
| **TC-E02** | Criteria | Tạo tiêu chí hợp lệ | Đã chọn nhóm SP | Nhập CriteriaCode, Tên, Loại (Benefit/Cost), Min, Max | Thêm thành công, hiển thị badge màu xanh (Benefit) hoặc cam (Cost) | E2E |
| **TC-F01** | Supplier | Danh sách nhà cung cấp | Có dữ liệu NCC | Truy cập `/Supplier` | Hiển thị danh sách NCC, thông tin liên hệ, trạng thái | E2E |
| **TC-F02** | Supplier | Thêm nhà cung cấp mới | Đăng nhập hợp lệ | Nhập Mã NCC, Tên, MST, Email, SĐT | Lưu thành công vào DB, hiển thị thông báo thành công | E2E |
| **TC-F03** | Supplier | Chuyển trạng thái sang Blacklisted | NCC tồn tại | Cập nhật Status = 'Blacklisted' | Không thể chọn NCC này vào các đợt đánh giá mới | Integration |
| **TC-G01** | Batch | Tạo đợt đánh giá mới (Draft) | Có ít nhất 1 nhóm SP, 2 NCC, 2 Tiêu chí | Chọn nhóm SP, chọn NCCs, chọn Tiêu chí | Tạo thành công ở trạng thái Draft | E2E |
| **TC-G02** | Batch | Chuyển trạng thái sang Evaluating | Đợt đang ở Draft | Bấm "Bắt đầu đánh giá" | Trạng thái chuyển sang Evaluating, kích hoạt trang chấm điểm | E2E |
| **TC-G03** | Batch | Chặn xóa đợt đã chốt quyết định | Đợt đã Approved | Thực hiện xóa đợt | Trigger DB `TR_EvaluationBatches_PreventDeleteCompleted` chặn lại | Database / Trigger |
| **TC-H01** | Scoring | Hiển thị ma trận điểm đầy đủ | Đợt ở trạng thái Evaluating | Truy cập `/Evaluation?batchId=...` | Hiển thị lưới nhập điểm cho tất cả NCCs và Tiêu chí | E2E |
| **TC-H02** | Scoring | Nhập điểm số thập phân hợp lệ | Đang trong form điểm | Nhập giá trị có số thập phân (`8.5` hoặc `8,5`) | Hệ thống lưu chính xác giá trị số, không bị lỗi văn hóa địa phương | Integration |
| **TC-H03** | Scoring | Bỏ sót ô điểm chưa nhập | Đang trong form điểm | Để trống 1 ô điểm và lưu | Cảnh báo yêu cầu nhập đầy đủ điểm trước khi chuyển sang bước tiếp theo | Frontend / Validation |
| **TC-I01** | AHP | Hiển thị ma trận so sánh cặp | Đợt có $\ge 2$ tiêu chí | Truy cập `/EvaluationBatch/{id}/AHP` | Ma trận vuông $n \times n$, đường chéo bằng 1.0, các ô nghịch đảo tự động cập nhật | E2E |
| **TC-I02** | AHP | Nhập giá trị nghịch đảo (phân số) | Đang so sánh cặp C1 và C2 | Chọn tỉ lệ 3 (Quan trọng hơn gấp 3) | Ô nghịch đảo C2-C1 tự động hiển thị 1/3 (0.3333) | Frontend / E2E |
| **TC-I03** | AHP | Tính toán trọng số và kiểm tra CR | Đã nhập đầy đủ ma trận | Bấm "Tính toán trọng số" | Tính ra $\lambda_{\max}$, $CI$, $CR$. Nếu $CR \le 0.1$ báo xanh nhất quán, lưu trọng số | Unit / MCDM Parity |
| **TC-I04** | AHP | Cảnh báo ma trận không nhất quán | Nhập ma trận mâu thuẫn lớn | Bấm tính toán khi $CR > 0.1$ | Cảnh báo đỏ bằng tiếng Việt, yêu cầu xem xét lại các đánh giá mâu thuẫn | E2E |
| **TC-J01** | TOPSIS | Tính toán ma trận quyết định chuẩn hóa | Điểm và trọng số AHP hợp lệ | Truy cập `/TOPSIS?batchId=...` | Hiển thị ma trận chuẩn hóa và ma trận nhân trọng số | Unit / MCDM Parity |
| **TC-J02** | TOPSIS | Xác định PIS và NIS | Có ma trận chuẩn hóa nhân trọng số | Chạy thuật toán TOPSIS | Phân biệt chính xác cực đại/cực tiểu cho tiêu chí Benefit và Cost | Unit / Test |
| **TC-J03** | TOPSIS | Xếp hạng nhà cung cấp | Đã tính khoảng cách Euclid | Xem bảng xếp hạng | Xếp hạng theo thứ tự giảm dần của hệ số $C_i$, đánh dấu nhà cung cấp tốt nhất | E2E |
| **TC-K01** | Sensitivity | Biến thiên trọng số tiêu chí | Đã có kết quả TOPSIS | Kéo thanh trượt thay đổi trọng số $\pm 20\%$ | Hệ số $C_i$ các nhà cung cấp tự động cập nhật theo thời gian thực | Frontend / E2E |
| **TC-K02** | Sensitivity | Phát hiện đảo vị thế (Rank Reversal) | Đổi trọng số vượt ngưỡng nhạy | Đổi trọng số tiêu chí nhạy cảm | Hiển thị cảnh báo điểm giao cắt và thay đổi vị trí thứ hạng | Logic / E2E |
| **TC-L01** | Decision | Trình phê duyệt kết quả | Vai trò ProcurementOfficer | Bấm "Gửi phê duyệt" | Ghi nhận trạng thái chờ duyệt, thông báo tới Quản lý | E2E |
| **TC-L02** | Decision | Quản lý phê duyệt quyết định | Login Manager | Bấm "Phê duyệt" kèm nhận xét | Đợt chuyển sang Completed, chốt nhà cung cấp được lựa chọn | E2E |
| **TC-L03** | Decision | Tính bất biến sau khi phê duyệt | Đợt đã Approved | Thử sửa điểm hoặc sửa AHP | DB Trigger và Service chặn đứng thao tác, bảo toàn lịch sử | Trigger / Integration |
| **TC-M01** | Report | Xuất báo cáo hồ sơ PDF | Đợt đã hoàn tất | Bấm "Tải Báo cáo PDF" | Sinh file PDF chuẩn QuestPDF đầy đủ bảng số liệu và chữ ký | Integration |
| **TC-M02** | Report | Xuất bảng tính Excel | Đợt đã hoàn tất | Bấm "Tải Báo cáo Excel" | Sinh file `.xlsx` chuẩn ClosedXML có định dạng công thức | Integration |
| **TC-M03** | Report | Chặn truy cập trực tiếp file tĩnh | Bất kỳ ai | Truy cập trực tiếp `/reports/secret.pdf` | Bị middleware chặn, trả về HTTP 404 có giao diện lỗi, KHÔNG trắng màn hình | Security / Integration |
| **TC-N01** | Non-func | Phòng chống Màn hình trắng trên 404 | Bất kỳ ai | Truy cập URL sai: `/NonExistentRoute` | Trả về trang lỗi 404 tiếng Việt đẹp mắt, logo DSS, nút về trang chủ ($\ge 200$ bytes) | Integration / Fix P0 |
| **TC-N02** | Non-func | Truy cập MCDM không truyền batchId | Đã đăng nhập | Truy cập `/AHP`, `/TOPSIS`, `/DecisionResult` | Tự động chuyển hướng về `/EvaluationBatch` kèm cảnh báo thân thiện, KHÔNG màn hình trắng | Integration / Fix P0 |
| **TC-N03** | Non-func | Chống tấn công CSRF trên Form | Form bất kỳ | Gửi POST thiếu AntiForgeryToken | Trả về HTTP 400 kèm trang lỗi thân thiện, KHÔNG màn hình trắng | Security / Integration |
| **TC-N04** | Non-func | Hiển thị tương thích Dark Mode | Trình duyệt bật dark theme | Bật `[data-theme="dark"]` | Không bị lỗi chữ trắng trên nền trắng, màu sắc tương phản cao | UI / CSS |
| **TC-N05** | Non-func | Đầy đủ biến CSS Design Tokens | Tải trang bất kỳ | Kiểm tra console và computed styles | Không còn thiếu bất kỳ biến token nào (`--brand-primary`, `--primary`, `--muted-dark`, v.v.) | UI / CSS |
