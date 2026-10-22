/* =====================================================================
   8. VIEW HỖ TRỢ BÁO CÁO / DASHBOARD
   ===================================================================== */

CREATE VIEW vw_BatchRanking
AS
SELECT
    b.BatchID,
    b.BatchCode,
    b.BatchName,
    b.Status AS BatchStatus,
    s.SupplierID,
    s.SupplierCode,
    s.SupplierName,
    t.DistanceToPIS,
    t.DistanceToNIS,
    t.ClosenessCoefficient,
    t.RankNo,
    t.CalculatedAt
FROM TopsisResults t
INNER JOIN EvaluationBatches b ON t.BatchID = b.BatchID
INNER JOIN Suppliers s ON t.SupplierID = s.SupplierID;
GO

CREATE VIEW vw_BatchCriteriaWeights
AS
SELECT
    b.BatchID,
    b.BatchCode,
    c.CriteriaID,
    c.CriteriaCode,
    c.CriteriaName,
    c.CriteriaType,
    w.WeightValue
FROM AHPCriteriaWeights w
INNER JOIN EvaluationBatches b ON w.BatchID = b.BatchID
INNER JOIN Criteria c ON w.CriteriaID = c.CriteriaID;
GO
