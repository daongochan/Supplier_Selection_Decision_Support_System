/* =====================================================================
   SUPPLIER SELECTION DSS - BENCHMARK SEED DATA (PERFORMANCE TESTING ONLY)
   Database target: SupplierSelectionDSS_Perf
   ===================================================================== */

USE SupplierSelectionDSS_Perf;
GO

SET NOCOUNT ON;

-- 1. Thêm các nhóm hàng hoá (ProductGroups) để đủ 10 nhóm
IF NOT EXISTS (SELECT 1 FROM ProductGroups WHERE GroupCode = N'PG02')
BEGIN
    INSERT INTO ProductGroups (GroupCode, GroupName, Description) VALUES
    (N'PG02', N'Linh kiện cơ khí chính xác', N'Gia công cơ khí, tiện, phay CNC và phụ kiện kim khí'),
    (N'PG03', N'Bao bì và thùng carton công nghiệp', N'Vật tư đóng gói tiêu chuẩn xuất khẩu'),
    (N'PG04', N'Hóa chất phụ trợ sản xuất', N'Dung môi, chất tẩy rửa công nghiệp và phụ gia tạo màu'),
    (N'PG05', N'Linh kiện điện tử bán dẫn', N'IC, vi điều khiển, tụ điện và bảng mạch PCB'),
    (N'PG06', N'Dịch vụ vận tải & logistics', N'Giao vận nội địa và logistics đường biển'),
    (N'PG07', N'Dụng cụ bảo hộ lao động', N'Trang thiết bị bảo hộ, nón, giày, găng tay chống tĩnh điện'),
    (N'PG08', N'Nhựa kỹ thuật & hạt nhựa PVC', N'Nguyên liệu ép phun và đùn ống kỹ thuật'),
    (N'PG09', N'Vật tư văn phòng phẩm & IT', N'Thiết bị mạng, máy in, mực in và văn phòng phẩm'),
    (N'PG10', N'Thiết bị đo lường & hiệu chuẩn', N'Thước kẹp, máy đo quang học, cảm biến kiểm tra KCS');
END
GO

-- 2. Thêm các tiêu chí mẫu (Criteria) để đủ 12 tiêu chí
IF NOT EXISTS (SELECT 1 FROM Criteria WHERE CriteriaCode = N'C07')
BEGIN
    INSERT INTO Criteria (CriteriaCode, CriteriaName, CriteriaType, Unit, Description) VALUES
    (N'C07', N'Tỷ lệ giao hàng đúng hẹn (OTIF)', N'Benefit', N'%', N'Tỷ lệ số đơn hàng giao đúng hạn và đầy đủ số lượng'),
    (N'C08', N'Thời hạn thanh toán công nợ',      N'Benefit', N'Ngày', N'Số ngày cho phép chậm trả sau khi nhận hóa đơn'),
    (N'C09', N'Chứng chỉ chất lượng quốc tế',     N'Benefit', N'Điểm', N'ISO 9001, ISO 14001, IATF 16949 (thang 0-10)'),
    (N'C10', N'Chi phí vận chuyển & bốc dỡ',       N'Cost',    N'VND',  N'Chi phí phụ trội cho vận chuyển đến kho nhà máy'),
    (N'C11', N'Mức độ linh hoạt thay đổi đơn',    N'Benefit', N'Điểm', N'Khả năng điều chỉnh tăng/giảm đơn gấp 3 ngày (thang 0-10)'),
    (N'C12', N'Tỷ lệ phế phẩm khi kiểm tra đầu vào', N'Cost',  N'%', N'Tỷ lệ phần trăm lô hàng lỗi qua kiểm định QA');
END
GO

-- 3. Sinh 196 Nhà cung cấp mới (để tổng cộng đạt 200 nhà cung cấp SUP001 -> SUP200)
DECLARE @CurSuppliers INT = (SELECT COUNT(*) FROM Suppliers);
IF @CurSuppliers < 200
BEGIN
    DECLARE @sIndex INT = 5;
    WHILE @sIndex <= 200
    BEGIN
        DECLARE @sCode NVARCHAR(20) = N'SUP' + RIGHT('000' + CAST(@sIndex AS VARCHAR(10)), 3);
        DECLARE @sName NVARCHAR(200) = N'Công ty Cổ phần / TNHH Công nghiệp ' + CAST(@sIndex AS NVARCHAR(10));
        DECLARE @sCity NVARCHAR(100) = CASE (@sIndex % 5)
            WHEN 0 THEN N'Hà Nội'
            WHEN 1 THEN N'Bình Dương'
            WHEN 2 THEN N'Hải Phòng'
            WHEN 3 THEN N'Đồng Nai'
            ELSE N'Bắc Ninh'
        END;
        DECLARE @sContact NVARCHAR(100) = N'Đại diện KD số ' + CAST(@sIndex AS NVARCHAR(10));

        IF NOT EXISTS (SELECT 1 FROM Suppliers WHERE SupplierCode = @sCode)
        BEGIN
            INSERT INTO Suppliers (SupplierCode, SupplierName, Address, ContactPerson, Status)
            VALUES (@sCode, @sName, @sCity, @sContact, CASE WHEN @sIndex % 15 = 0 THEN N'Inactive' ELSE N'Active' END);
        END

        SET @sIndex = @sIndex + 1;
    END
END
GO

-- 4. Liên kết SupplierGroups (mỗi nhà cung cấp vào 1-2 nhóm hàng)
INSERT INTO SupplierGroups (SupplierID, GroupID)
SELECT s.SupplierID, g.GroupID
FROM Suppliers s
CROSS JOIN ProductGroups g
WHERE g.GroupID = ((s.SupplierID % 10) + 1)
  AND NOT EXISTS (
      SELECT 1 FROM SupplierGroups sg 
      WHERE sg.SupplierID = s.SupplierID AND sg.GroupID = g.GroupID
  );
GO

-- 5. Sinh 40 Đợt đánh giá (EvaluationBatches) trải dài 6 tháng qua
-- Trạng thái: 15 Completed, 15 Evaluating, 6 Draft, 4 Cancelled
DECLARE @CurBatches INT = (SELECT COUNT(*) FROM EvaluationBatches);
IF @CurBatches < 40
BEGIN
    DECLARE @bIndex INT = 1;
    DECLARE @baseDate DATETIME2 = DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1);

    WHILE @bIndex <= 40
    BEGIN
        DECLARE @bCode NVARCHAR(20) = N'EB' + RIGHT('00' + CAST(@bIndex AS VARCHAR(10)), 2);
        DECLARE @bName NVARCHAR(200) = N'Đợt đánh giá định kỳ ' + @bCode;
        DECLARE @bGroup INT = ((@bIndex % 10) + 1);
        DECLARE @bStatus NVARCHAR(20);
        DECLARE @monthOffset INT;
        DECLARE @createdAt DATETIME2;
        DECLARE @completedAt DATETIME2 = NULL;
        DECLARE @cancelledAt DATETIME2 = NULL;

        IF @bIndex <= 15
        BEGIN
            SET @bStatus = N'Completed';
            -- Trải đều 5 tháng trước đến tháng trước (3 batch mỗi tháng)
            SET @monthOffset = -5 + ((@bIndex - 1) / 3);
            SET @createdAt = DATEADD(DAY, (@bIndex % 25) + 1, DATEADD(MONTH, @monthOffset, @baseDate));
            SET @completedAt = DATEADD(DAY, 7, @createdAt);
        END
        ELSE IF @bIndex <= 30
        BEGIN
            SET @bStatus = N'Evaluating';
            -- Tháng gần nhất hoặc tháng hiện tại
            SET @monthOffset = -1 + ((@bIndex - 16) / 8);
            SET @createdAt = DATEADD(DAY, ((@bIndex - 15) * 2) % 28 + 1, DATEADD(MONTH, @monthOffset, @baseDate));
        END
        ELSE IF @bIndex <= 36
        BEGIN
            SET @bStatus = N'Draft';
            SET @monthOffset = 0;
            SET @createdAt = DATEADD(DAY, (@bIndex - 30) * 3, @baseDate);
        END
        ELSE
        BEGIN
            SET @bStatus = N'Cancelled';
            SET @monthOffset = -3;
            SET @createdAt = DATEADD(DAY, (@bIndex - 36) * 5, DATEADD(MONTH, @monthOffset, @baseDate));
            SET @cancelledAt = DATEADD(DAY, 2, @createdAt);
        END

        IF NOT EXISTS (SELECT 1 FROM EvaluationBatches WHERE BatchCode = @bCode)
        BEGIN
            INSERT INTO EvaluationBatches 
            (BatchCode, BatchName, Description, GroupID, StartDate, EndDate, Status, CreatedBy, CreatedAt, CompletedAt, CancelledAt)
            VALUES 
            (@bCode, @bName, N'Dữ liệu giả lập đo kiểm hiệu năng Round 3', @bGroup, 
             CAST(@createdAt AS DATE), CAST(DATEADD(DAY, 14, @createdAt) AS DATE),
             @bStatus, 2, @createdAt, @completedAt, @cancelledAt);
        END

        SET @bIndex = @bIndex + 1;
    END
END
GO

-- 6. Gán BatchCriteria và BatchSuppliers cho mỗi Batch
-- Mỗi batch có 6-8 tiêu chí và 6-12 nhà cung cấp
INSERT INTO BatchCriteria (BatchID, CriteriaID)
SELECT b.BatchID, c.CriteriaID
FROM EvaluationBatches b
CROSS JOIN Criteria c
WHERE c.CriteriaID <= (6 + (b.BatchID % 5)) -- từ 6 đến 10 tiêu chí
  AND NOT EXISTS (
      SELECT 1 FROM BatchCriteria bc 
      WHERE bc.BatchID = b.BatchID AND bc.CriteriaID = c.CriteriaID
  );
GO

INSERT INTO BatchSuppliers (BatchID, SupplierID)
SELECT b.BatchID, s.SupplierID
FROM EvaluationBatches b
JOIN SupplierGroups sg ON sg.GroupID = b.GroupID
JOIN Suppliers s ON s.SupplierID = sg.SupplierID
WHERE s.Status = N'Active'
  AND s.SupplierID <= 150
  AND (s.SupplierID % 5) = (b.BatchID % 5)
  AND NOT EXISTS (
      SELECT 1 FROM BatchSuppliers bs 
      WHERE bs.BatchID = b.BatchID AND bs.SupplierID = s.SupplierID
  );
GO

-- Đảm bảo mỗi batch có ít nhất 5 nhà cung cấp
INSERT INTO BatchSuppliers (BatchID, SupplierID)
SELECT b.BatchID, s.SupplierID
FROM EvaluationBatches b
CROSS JOIN (SELECT TOP 6 SupplierID FROM Suppliers WHERE Status = N'Active') s
WHERE NOT EXISTS (
    SELECT 1 FROM BatchSuppliers bs WHERE bs.BatchID = b.BatchID AND bs.SupplierID = s.SupplierID
);
GO

-- 7. Sinh EvaluationScores cho các Batch Completed (1..15) và Evaluating có điểm (16..25)
INSERT INTO EvaluationScores (BatchID, SupplierID, CriteriaID, ScoreValue, EvaluatedBy, EvaluatedAt)
SELECT 
    bs.BatchID,
    bs.SupplierID,
    bc.CriteriaID,
    -- Giả lập điểm hợp lý: điểm thang 10 hoặc đơn vị khác
    CAST(
        CASE 
            WHEN c.Unit = N'Điểm' THEN 5.0 + ((bs.SupplierID * 7 + bc.CriteriaID * 3) % 45) / 10.0
            WHEN c.Unit = N'VND' THEN 100000.0 + ((bs.SupplierID * 1000 + bc.CriteriaID * 500) % 50000)
            WHEN c.Unit = N'Ngày' THEN 3.0 + ((bs.SupplierID + bc.CriteriaID) % 15)
            WHEN c.Unit = N'%' THEN 80.0 + ((bs.SupplierID * 3 + bc.CriteriaID * 2) % 18)
            ELSE 7.5
        END AS DECIMAL(18,4)
    ) AS ScoreValue,
    3 AS EvaluatedBy,
    b.CreatedAt AS EvaluatedAt
FROM EvaluationBatches b
JOIN BatchSuppliers bs ON bs.BatchID = b.BatchID
JOIN BatchCriteria bc ON bc.BatchID = b.BatchID
JOIN Criteria c ON c.CriteriaID = bc.CriteriaID
WHERE b.Status IN (N'Completed', N'Evaluating')
  AND b.BatchID <= 25
  AND NOT EXISTS (
      SELECT 1 FROM EvaluationScores es 
      WHERE es.BatchID = bs.BatchID AND es.SupplierID = bs.SupplierID AND es.CriteriaID = bc.CriteriaID
  );
GO

-- 8. Sinh AHP Pairwise Comparisons, Weights, và AHPResults cho Batches 1..25
-- AHP so sánh chéo giữa các criteria trong BatchCriteria
INSERT INTO AHPPairwiseComparisons (BatchID, CriteriaRowID, CriteriaColID, ComparisonValue, EvaluatedBy, EvaluatedAt)
SELECT 
    bc1.BatchID,
    bc1.CriteriaID AS CriteriaRowID,
    bc2.CriteriaID AS CriteriaColID,
    CAST(
        CASE 
            WHEN bc1.CriteriaID = bc2.CriteriaID THEN 1.0
            WHEN bc1.CriteriaID < bc2.CriteriaID THEN 
                CASE ((bc1.CriteriaID + bc2.CriteriaID) % 3)
                    WHEN 0 THEN 2.0
                    WHEN 1 THEN 3.0
                    ELSE 1.0
                END
            ELSE 
                1.0 / CASE ((bc1.CriteriaID + bc2.CriteriaID) % 3)
                    WHEN 0 THEN 2.0
                    WHEN 1 THEN 3.0
                    ELSE 1.0
                END
        END AS DECIMAL(10,4)
    ) AS ComparisonValue,
    3 AS EvaluatedBy,
    b.CreatedAt AS EvaluatedAt
FROM BatchCriteria bc1
JOIN BatchCriteria bc2 ON bc1.BatchID = bc2.BatchID
JOIN EvaluationBatches b ON b.BatchID = bc1.BatchID
WHERE b.Status IN (N'Completed', N'Evaluating')
  AND b.BatchID <= 25
  AND NOT EXISTS (
      SELECT 1 FROM AHPPairwiseComparisons ahp 
      WHERE ahp.BatchID = bc1.BatchID AND ahp.CriteriaRowID = bc1.CriteriaID AND ahp.CriteriaColID = bc2.CriteriaID
  );
GO

-- AHPResults
INSERT INTO AHPResults (BatchID, LambdaMax, CIValue, RIValue, CRValue, IsConsistent, n, CalculatedAt)
SELECT 
    b.BatchID,
    6.250000,
    0.050000,
    1.240000,
    0.040322,
    1,
    (SELECT COUNT(*) FROM BatchCriteria bc WHERE bc.BatchID = b.BatchID),
    b.CreatedAt
FROM EvaluationBatches b
WHERE b.Status IN (N'Completed', N'Evaluating')
  AND b.BatchID <= 25
  AND NOT EXISTS (SELECT 1 FROM AHPResults ar WHERE ar.BatchID = b.BatchID);
GO

-- AHPCriteriaWeights (chuẩn hoá trọng số đều hoặc xấp xỉ)
INSERT INTO AHPCriteriaWeights (BatchID, CriteriaID, WeightValue)
SELECT 
    bc.BatchID,
    bc.CriteriaID,
    CAST(1.0 / (SELECT COUNT(*) FROM BatchCriteria sub WHERE sub.BatchID = bc.BatchID) AS DECIMAL(10,6))
FROM BatchCriteria bc
JOIN EvaluationBatches b ON b.BatchID = bc.BatchID
WHERE b.Status IN (N'Completed', N'Evaluating')
  AND b.BatchID <= 25
  AND NOT EXISTS (
      SELECT 1 FROM AHPCriteriaWeights w 
      WHERE w.BatchID = bc.BatchID AND w.CriteriaID = bc.CriteriaID
  );
GO

-- 9. Sinh TopsisIdealSolutions và TopsisResults cho Batches 1..20
INSERT INTO TopsisIdealSolutions (BatchID, CriteriaID, PISValue, NISValue)
SELECT 
    bc.BatchID,
    bc.CriteriaID,
    0.350000,
    0.050000
FROM BatchCriteria bc
JOIN EvaluationBatches b ON b.BatchID = bc.BatchID
WHERE b.BatchID <= 20
  AND NOT EXISTS (
      SELECT 1 FROM TopsisIdealSolutions tis 
      WHERE tis.BatchID = bc.BatchID AND tis.CriteriaID = bc.CriteriaID
  );
GO

-- TopsisResults: Xếp hạng cho từng Supplier trong Batch
;WITH RankedSuppliers AS (
    SELECT 
        bs.BatchID,
        bs.SupplierID,
        ROW_NUMBER() OVER (PARTITION BY bs.BatchID ORDER BY bs.SupplierID) AS RankNo,
        COUNT(*) OVER (PARTITION BY bs.BatchID) AS TotalSuppliersInBatch
    FROM BatchSuppliers bs
    JOIN EvaluationBatches b ON b.BatchID = bs.BatchID
    WHERE b.BatchID <= 20
)
INSERT INTO TopsisResults (BatchID, SupplierID, DistanceToPIS, DistanceToNIS, ClosenessCoefficient, RankNo, CalculatedAt)
SELECT 
    rs.BatchID,
    rs.SupplierID,
    CAST(0.100000 + (rs.RankNo * 0.01) AS DECIMAL(18,6)),
    CAST(0.300000 - (rs.RankNo * 0.005) AS DECIMAL(18,6)),
    CAST(0.150000 + (0.800000 / (1.0 + (rs.RankNo * 0.2))) AS DECIMAL(10,6)),
    rs.RankNo,
    b.CreatedAt
FROM RankedSuppliers rs
JOIN EvaluationBatches b ON b.BatchID = rs.BatchID
WHERE NOT EXISTS (
    SELECT 1 FROM TopsisResults tr 
    WHERE tr.BatchID = rs.BatchID AND tr.SupplierID = rs.SupplierID
);
GO

-- 10. Approvals cho Completed Batches (1..15)
INSERT INTO Approvals (BatchID, SelectedSupplierID, ApprovedBy, ApprovalDate, Notes, Status, IsFinal)
SELECT 
    b.BatchID,
    (SELECT TOP 1 tr.SupplierID FROM TopsisResults tr WHERE tr.BatchID = b.BatchID AND tr.RankNo = 1),
    4 AS ApprovedBy,
    b.CompletedAt,
    N'Phê duyệt nhà cung cấp hạng 1 theo khuyến nghị hệ thống DSS.',
    N'Approved',
    1
FROM EvaluationBatches b
WHERE b.Status = N'Completed'
  AND NOT EXISTS (SELECT 1 FROM Approvals app WHERE app.BatchID = b.BatchID);
GO

-- 11. Reports cho Completed Batches (1..15)
INSERT INTO Reports (BatchID, ReportType, GeneratedBy, GeneratedAt, FilePath)
SELECT 
    b.BatchID,
    N'PDF',
    4,
    b.CompletedAt,
    N'reports/' + b.BatchCode + N'_FinalReport.pdf'
FROM EvaluationBatches b
WHERE b.Status = N'Completed'
  AND NOT EXISTS (SELECT 1 FROM Reports r WHERE r.BatchID = b.BatchID AND r.ReportType = N'PDF');
GO

INSERT INTO Reports (BatchID, ReportType, GeneratedBy, GeneratedAt, FilePath)
SELECT 
    b.BatchID,
    N'EXCEL',
    4,
    b.CompletedAt,
    N'reports/' + b.BatchCode + N'_FinalReport.xlsx'
FROM EvaluationBatches b
WHERE b.Status = N'Completed'
  AND NOT EXISTS (SELECT 1 FROM Reports r WHERE r.BatchID = b.BatchID AND r.ReportType = N'EXCEL');
GO

-- 12. Sinh 30,000 dòng AuditLogs bằng bảng tally
DECLARE @AuditCount INT = (SELECT COUNT(*) FROM AuditLogs);
IF @AuditCount < 30000
BEGIN
    PRINT N'Đang sinh 30,000 dòng AuditLogs giả lập...';

    ;WITH 
    L0 AS (SELECT 1 AS c UNION ALL SELECT 1),
    L1 AS (SELECT 1 AS c FROM L0 AS A CROSS JOIN L0 AS B), -- 4
    L2 AS (SELECT 1 AS c FROM L1 AS A CROSS JOIN L1 AS B), -- 16
    L3 AS (SELECT 1 AS c FROM L2 AS A CROSS JOIN L2 AS B), -- 256
    L4 AS (SELECT 1 AS c FROM L3 AS A CROSS JOIN L3 AS B), -- 65536
    Nums AS (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM L4)
    INSERT INTO AuditLogs (UserID, ActionType, TargetTable, TargetID, OldValue, NewValue, ActionTime, Description)
    SELECT TOP (30000 - @AuditCount)
        ((n % 4) + 1) AS UserID,
        CASE (n % 8)
            WHEN 0 THEN N'LOGIN'
            WHEN 1 THEN N'VIEW_DASHBOARD'
            WHEN 2 THEN N'SAVE_EVALUATION'
            WHEN 3 THEN N'CALCULATE_AHP'
            WHEN 4 THEN N'CALCULATE_TOPSIS'
            WHEN 5 THEN N'APPROVE_BATCH'
            WHEN 6 THEN N'EXPORT_REPORT'
            ELSE N'LOGOUT'
        END AS ActionType,
        CASE (n % 5)
            WHEN 0 THEN N'EvaluationBatches'
            WHEN 1 THEN N'EvaluationScores'
            WHEN 2 THEN N'AHPPairwiseComparisons'
            WHEN 3 THEN N'TopsisResults'
            ELSE N'Approvals'
        END AS TargetTable,
        ((n % 40) + 1) AS TargetID,
        NULL AS OldValue,
        NULL AS NewValue,
        DATEADD(SECOND, -(n * 500), SYSDATETIME()) AS ActionTime,
        N'Ghi nhận thao tác giả lập kiểm thử hiệu năng #' + CAST(n AS NVARCHAR(10)) AS Description
    FROM Nums;
END
GO
