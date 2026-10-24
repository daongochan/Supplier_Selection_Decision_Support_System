<!-- ARCHIVE_HEADER -->
> [!NOTE]
> **ARCHIVED DOCUMENT — SUPERSEDED**
> **Archived Date:** 2026-10-24  
> **Base Commit:** \d5b98db2b8b732bf0ce5491793aa73ba9136b678\  
> **Status:** Historical point-in-time document preserved for traceability. The active canonical documentation is located in \docs/\ (\DESIGN-SYSTEM.md\, \ARCHITECTURE.md\, \ALGORITHMS.md\, \WORKFLOW-AND-PERMISSIONS.md\, \docs/qa/\).

---

# BẢNG ÁNH XẠ DỮ LIỆU DASHBOARD (DATA MAPPING SPECIFICATION)

**Hệ thống:** SupplierSelectionDSS  
**Mục đích:** Xác định nguồn dữ liệu thật 100% từ Database cho tất cả các thành phần giao diện Dashboard.  
**Nguyên tắc cốt lõi:**  
- Database là Source of Truth.  
- Tuyệt đối không bịa số liệu, không hardcode giá trị hiển thị.  
- Mọi widget đều có nguồn truy vấn rõ ràng, sử dụng Entity Framework Core 10 `AsNoTracking()`.

---

## BẢNG ÁNH XẠ CHI TIẾT TỪNG WIDGET

| STT | Widget Giao Diện | Data Source | Table / View | Trường Dữ Liệu (Field) | Phương Thức Truy Vấn / Logic Service |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **1** | **Batch Selector Dropdown** (`[Đợt đánh giá ▼]`) | Danh sách đợt đánh giá | `EvaluationBatches` | `BatchID`, `BatchCode`, `BatchName`, `Status`, `CreatedAt` | `_context.EvaluationBatches.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync()` |
| **2** | **Thẻ ngữ cảnh đợt hiện tại** (Current Batch Card) | Chi tiết đợt được chọn | `EvaluationBatches`, `ProductGroups`, `BatchSuppliers`, `EvaluationScores` | `b.BatchCode`, `b.BatchName`, `g.GroupName`, `b.Status`, `b.Suppliers.Count`, `ScoreSupplierCount` | Truy vấn `EvaluationBatch` với `.Include(x => x.Group).Include(x => x.Suppliers)`. Đếm số supplier có điểm trong `EvaluationScores` của đợt để tính `% hoàn thành = (ScoredCount / TotalSuppliers) * 100`. |
| **3** | **KPI 1: Tổng nhà cung cấp** | Thống kê NCC hệ thống | `Suppliers` | `SupplierID`, `Status` | `Total = _context.Suppliers.Count()`, `Active = _context.Suppliers.Count(x => x.Status == EntityStatus.Active)` |
| **4** | **KPI 2: Nhóm hàng** | Danh mục nhóm hàng | `ProductGroups` | `GroupID`, `Status` | `Total = _context.ProductGroups.Count()`, `Active = _context.ProductGroups.Count(x => x.Status == EntityStatus.Active)` |
| **5** | **KPI 3: Đợt đánh giá** | Thống kê các đợt | `EvaluationBatches` | `BatchID`, `Status` | `Total = _context.EvaluationBatches.Count()`, `Completed = _context.EvaluationBatches.Count(x => x.Status == BatchStatus.Completed)` |
| **6** | **KPI 4: Tiêu chí** | Danh mục tiêu chí DSS | `Criteria` | `CriteriaID`, `CriteriaType`, `Status` | `Total = _context.Criteria.Count()`, `BenefitCount`, `CostCount` |
| **7** | **KPI 5: CR (AHP)** | Kết quả nhất quán AHP đợt hiện tại | `AHPResults` | `CRValue`, `IsConsistent`, `LambdaMax`, `CIValue`, `RIValue` | `_context.Ahpresults.AsNoTracking().FirstOrDefaultAsync(x => x.BatchId == batchId)`. Đánh giá: $CR \le 0.10 \implies$ Đạt yêu cầu. |
| **8** | **KPI 6: Top 1 (TOPSIS)** | Nhà cung cấp hạng 1 đợt hiện tại | `TopsisResults`, `Suppliers` (hoặc `vw_BatchRanking`) | `s.SupplierName`, `s.SupplierCode`, `t.ClosenessCoefficient`, `t.RankNo` | `_context.TopsisResults.AsNoTracking().Where(x => x.BatchId == batchId && x.RankNo == 1).Include(x => x.Supplier).FirstOrDefaultAsync()` |
| **9** | **Biểu đồ Donut: Tình trạng đợt đánh giá** | Cơ cấu trạng thái đợt | `EvaluationBatches` | `Status`, `Count()` | `_context.EvaluationBatches.GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() })` (Draft, Evaluating, Completed, Cancelled). |
| **10** | **Biểu đồ Line: Xu hướng đánh giá theo thời gian** | Lịch sử đợt và số NCC đánh giá | `EvaluationBatches`, `BatchSuppliers` | `CreatedAt`, `Month`, `Year`, số đợt, số NCC | Group dữ liệu thực tế theo 6 tháng gần nhất: Đếm số đợt tạo trong tháng và tổng số NCC trong các đợt đó. Nếu chưa đủ tháng, hiển thị các tháng có dữ liệu thực. |
| **11** | **Biểu đồ Donut: Phân bố nhà cung cấp** | Trạng thái hoạt động NCC | `Suppliers` | `Status`, `Count()` | `_context.Suppliers.GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() })` (Active / Inactive). |
| **12** | **Bảng Top 5 Nhà Cung Cấp theo TOPSIS** | Bảng xếp hạng đợt hiện tại | `TopsisResults`, `Suppliers`, `ProductGroups` | `t.RankNo`, `s.SupplierName`, `s.SupplierCode`, `g.GroupName`, `t.ClosenessCoefficient`, `s.Status` | `_context.TopsisResults.AsNoTracking().Where(x => x.BatchId == batchId).OrderBy(x => x.RankNo).Take(5).Include(x => x.Supplier)...` |
| **13** | **Biểu đồ Bar: Xếp hạng TOPSIS** | Trực quan hóa hệ số $C_i$ Top 5 | `TopsisResults`, `Suppliers` | `s.SupplierName`, `t.ClosenessCoefficient` | Lấy trực tiếp từ tập Top 5 TOPSIS của đợt hiện tại, ánh xạ trục Y: Tên nhà cung cấp, trục X: $C_i \in [0, 1]$. |
| **14** | **Quy trình đánh giá (Workflow Stepper 5 bước)** | Kiểm tra trạng thái thực thi thực tế của đợt | `EvaluationBatches`, `EvaluationScores`, `AHPResults`, `TopsisResults`, `Approvals` | `b.Status`, `HasScores`, `HasAhpResult`, `HasTopsisResult`, `HasApproval` | Kiểm tra điều kiện: <br>1. *Nhập liệu*: `EvaluationScores.Any(x => x.BatchId == batchId)`<br>2. *AHP*: `Ahpresults.Any(x => x.BatchId == batchId)`<br>3. *TOPSIS*: `TopsisResults.Any(x => x.BatchId == batchId)`<br>4. *So sánh*: Bước 3 hoàn thành<br>5. *Phê duyệt*: `Approvals.Any(x => x.BatchId == batchId && x.Status == "Approved")` |
| **15** | **Thẻ tiến độ giai đoạn hiện tại (Circular Progress)** | Tiến độ phần trăm thực tế | Tính toán từ Stepper | Bước hiện tại (1 đến 5) | Tỷ lệ phần trăm tính theo giai đoạn: Bước 1 = 20%, Bước 2 = 40%, Bước 3 = 60%, Bước 4 = 80%, Bước 5 = 100%. |
| **16** | **Thanh tiến độ: Phân bố trọng số AHP** | Trọng số tiêu chí đợt hiện tại | `AHPCriteriaWeights`, `Criteria` | `c.CriteriaName`, `w.WeightValue` | `_context.AhpcriteriaWeights.AsNoTracking().Where(x => x.BatchId == batchId).Include(x => x.Criteria).OrderByDescending(x => x.WeightValue).ToListAsync()`. Phần trăm hiển thị $= w_j \times 100\%$. |
| **17** | **Biểu đồ Radar: So sánh Top 3 NCC** | Điểm số các tiêu chí của Top 3 | `EvaluationScores`, `Criteria`, `Suppliers` | `c.CriteriaName`, `s.SupplierName`, `AVG(ScoreValue)` | Lấy Top 3 `SupplierId` từ TOPSIS; truy vấn bảng `EvaluationScores` cho đợt hiện tại theo các `Criteria` của đợt. Chuẩn hóa thang điểm hiển thị 0 - 10. |
| **18** | **Thông tin hệ thống** (System Information) | Thông tin vận hành | `Users`, `Roles`, Metadata | `UserCount`, `RoleCount = 4`, `Database = "SQL Server"`, `Version = "v1.0.0"` | `_context.Users.CountAsync()`, kết hợp các thông số phiên bản phần mềm. |
| **19** | **Thẻ trạng thái phê duyệt** (Approval Card) | Quyết định phê duyệt đợt hiện tại | `Approvals`, `Suppliers`, `Users` | `a.Status`, `s.SupplierName`, `u.FullName`, `a.ApprovalDate`, `a.Notes` | `_context.Approvals.AsNoTracking().Where(x => x.BatchId == batchId).OrderByDescending(x => x.ApprovalDate).Include(x => x.SelectedSupplier).Include(x => x.ApprovedByNavigation).FirstOrDefaultAsync()` |
| **20** | **Tác vụ báo cáo nhanh** (Report Actions) | Báo cáo đã xuất của đợt | `Reports` | `r.ReportId`, `r.ReportType`, `r.FilePath` | `_context.Reports.AsNoTracking().Where(x => x.BatchId == batchId).OrderByDescending(x => x.GeneratedAt).FirstOrDefaultAsync()` |
| **21** | **Nhật ký hoạt động gần đây** (Recent Activities) | Lịch sử thao tác hệ thống | `AuditLogs`, `Users` | `l.ActionType`, `l.Description`, `l.ActionTime`, `u.FullName` | `_context.AuditLogs.AsNoTracking().OrderByDescending(x => x.ActionTime).Take(5).Include(x => x.User).ToListAsync()` |

---

## CƠ CHẾ XỬ LÝ EMPTY STATE KHI THIẾU DỮ LIỆU
1. **Trường hợp hệ thống chưa có đợt đánh giá nào:**
   - Thẻ ngữ cảnh hiển thị thông điệp mời khởi tạo đợt đánh giá đầu tiên.
   - Các biểu đồ theo đợt (TOPSIS, AHP, Radar) hiển thị container rỗng có biểu tượng gợi ý thao tác và nút điều hướng tới `/EvaluationBatch/Create`.
2. **Trường hợp đợt đánh giá chưa hoàn thành AHP / TOPSIS:**
   - Card CR (AHP) hiển thị badge `Chưa tính toán` kèm link dẫn tới màn hình tính AHP.
   - Bảng TOPSIS và Bar chart hiển thị trạng thái `Đang chờ tính toán TOPSIS` kèm link dẫn tới màn hình TOPSIS.
   - Card Phê duyệt hiển thị `Chưa gửi phê duyệt`.
