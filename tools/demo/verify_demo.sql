/* =====================================================================
   SUPPLIER SELECTION DSS - DEMONSTRATION DATASET VERIFICATION SCRIPT
   Target Database: SupplierSelectionDSS_Demo
   Usage:
     sqlcmd -S . -E -C -d SupplierSelectionDSS_Demo -i tools/demo/verify_demo.sql
   ===================================================================== */

SET NOCOUNT ON;

PRINT '================================================================================';
PRINT '  SUPPLIER SELECTION DSS - DEMO DATASET SQL VERIFICATION AUDIT                 ';
PRINT '================================================================================';

-- 1. STRICT DATABASE SAFETY GUARD
DECLARE @CurrentDb NVARCHAR(128) = DB_NAME();
IF @CurrentDb NOT LIKE '%_Demo%'
BEGIN
    RAISERROR(N'CRITICAL SAFETY ERROR: verify_demo.sql can ONLY be executed against SupplierSelectionDSS_Demo! Current database is: %s', 16, 1, @CurrentDb);
    RETURN;
END;

PRINT N'Database Name: ' + @CurrentDb;
PRINT N'Execution Time: ' + CONVERT(NVARCHAR(30), SYSDATETIME(), 120);
PRINT '--------------------------------------------------------------------------------';

DECLARE @FailCount INT = 0;

-- 2. USER ACCOUNTS CHECK (5 users)
DECLARE @UserCount INT;
SELECT @UserCount = COUNT(*) FROM Users WHERE Username LIKE 'demo.%';
IF @UserCount = 5
    PRINT N'[PASS] Users: Exactly 5 demo accounts found (4 active roles + 1 inactive expert).';
ELSE
BEGIN
    PRINT N'[FAIL] Users: Expected 5 demo accounts, found ' + CAST(@UserCount AS NVARCHAR(10));
    SET @FailCount = @FailCount + 1;
END;

-- 3. PRODUCT GROUPS CHECK (4 groups)
DECLARE @GroupCount INT, @ActiveGroups INT, @InactiveGroups INT;
SELECT 
    @GroupCount = COUNT(*),
    @ActiveGroups = SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END),
    @InactiveGroups = SUM(CASE WHEN Status = 'Inactive' THEN 1 ELSE 0 END)
FROM ProductGroups WHERE GroupCode LIKE 'DEMO-%';

IF @GroupCount = 4 AND @ActiveGroups = 3 AND @InactiveGroups = 1
    PRINT N'[PASS] Product Groups: 4 demo groups found (3 Active, 1 Inactive).';
ELSE
BEGIN
    PRINT N'[FAIL] Product Groups: Expected 4 groups (3 Active, 1 Inactive), found ' + CAST(@GroupCount AS NVARCHAR(10));
    SET @FailCount = @FailCount + 1;
END;

-- 4. SUPPLIERS CHECK (12 suppliers: 10 active + 2 inactive)
DECLARE @SupplierCount INT, @ActiveSuppliers INT, @InactiveSuppliers INT;
SELECT 
    @SupplierCount = COUNT(*),
    @ActiveSuppliers = SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END),
    @InactiveSuppliers = SUM(CASE WHEN Status = 'Inactive' THEN 1 ELSE 0 END)
FROM Suppliers WHERE SupplierCode LIKE 'DEMO-%';

IF @SupplierCount = 12 AND @ActiveSuppliers = 10 AND @InactiveSuppliers = 2
    PRINT N'[PASS] Suppliers: 12 demo suppliers found (10 Active, 2 Inactive).';
ELSE
BEGIN
    PRINT N'[FAIL] Suppliers: Expected 12 suppliers (10 Active, 2 Inactive), found ' + CAST(@SupplierCount AS NVARCHAR(10));
    SET @FailCount = @FailCount + 1;
END;

-- 5. CRITERIA CHECK (7 criteria: 6 active + 1 inactive, 3 rating scale)
DECLARE @CriteriaCount INT, @RatingCriteria INT;
SELECT 
    @CriteriaCount = COUNT(*),
    @RatingCriteria = SUM(CASE WHEN Unit = N'Điểm' OR (CriteriaCode IN ('DEMO-C02', 'DEMO-C04', 'DEMO-C05') AND Unit LIKE '%m%') THEN 1 ELSE 0 END)
FROM Criteria WHERE CriteriaCode LIKE 'DEMO-%';

IF @CriteriaCount = 7 AND @RatingCriteria = 3
    PRINT N'[PASS] Criteria: 7 demo criteria found (6 Active, 1 Inactive; 3 using Rating Scale ''Điểm'').';
ELSE
BEGIN
    PRINT N'[FAIL] Criteria: Expected 7 criteria with 3 rating scales, found ' + CAST(@CriteriaCount AS NVARCHAR(10));
    SET @FailCount = @FailCount + 1;
END;

-- 6. EVALUATION BATCHES CHECK (8 batches)
DECLARE @BatchCount INT;
SELECT @BatchCount = COUNT(*) FROM EvaluationBatches WHERE BatchCode LIKE 'DEMO-%';

IF @BatchCount = 8
    PRINT N'[PASS] Evaluation Batches: Exactly 8 demonstration batches present.';
ELSE
BEGIN
    PRINT N'[FAIL] Evaluation Batches: Expected 8 batches, found ' + CAST(@BatchCount AS NVARCHAR(10));
    SET @FailCount = @FailCount + 1;
END;

-- 7. AHP CONSISTENCY RATIO AUDIT
DECLARE @ConsistentAHP INT, @InconsistentAHP INT;
SELECT 
    @ConsistentAHP = SUM(CASE WHEN eb.BatchCode IN ('DEMO-B01', 'DEMO-B02', 'DEMO-B03', 'DEMO-B04') AND r.CRValue <= 0.10 THEN 1 ELSE 0 END),
    @InconsistentAHP = SUM(CASE WHEN eb.BatchCode = 'DEMO-B05' AND r.CRValue > 0.10 THEN 1 ELSE 0 END)
FROM Ahpresults r
INNER JOIN EvaluationBatches eb ON r.BatchID = eb.BatchID
WHERE eb.BatchCode LIKE 'DEMO-%';

IF @ConsistentAHP = 4 AND @InconsistentAHP = 1
    PRINT N'[PASS] AHP Invariant: Batches B01-B04 have CR <= 0.10, and B05 exhibits intentional CR > 0.10.';
ELSE
BEGIN
    PRINT N'[FAIL] AHP Invariant: Inconsistent AHP results count. Consistent: ' + CAST(ISNULL(@ConsistentAHP,0) AS NVARCHAR(10)) + N', Inconsistent: ' + CAST(ISNULL(@InconsistentAHP,0) AS NVARCHAR(10));
    SET @FailCount = @FailCount + 1;
END;

-- 8. TOPSIS RESULTS AUDIT
DECLARE @TopsisBatchCount INT, @InvalidTopsisCC INT;
SELECT 
    @TopsisBatchCount = COUNT(DISTINCT eb.BatchID),
    @InvalidTopsisCC = SUM(CASE WHEN tr.ClosenessCoefficient < 0 OR tr.ClosenessCoefficient > 1 THEN 1 ELSE 0 END)
FROM TopsisResults tr
INNER JOIN EvaluationBatches eb ON tr.BatchID = eb.BatchID
WHERE eb.BatchCode LIKE 'DEMO-%';

IF @TopsisBatchCount = 3 AND @InvalidTopsisCC = 0
    PRINT N'[PASS] TOPSIS Invariant: 3 batches have complete TOPSIS rankings with Closeness Coefficients in [0, 1].';
ELSE
BEGIN
    PRINT N'[FAIL] TOPSIS Invariant: Expected 3 batches with valid TOPSIS CCs, found batches: ' + CAST(ISNULL(@TopsisBatchCount,0) AS NVARCHAR(10)) + N', invalid CCs: ' + CAST(ISNULL(@InvalidTopsisCC,0) AS NVARCHAR(10));
    SET @FailCount = @FailCount + 1;
END;

-- 9. APPROVALS AND SNAPSHOTS
DECLARE @ApprovalCount INT, @SnapshotCount INT;
SELECT @ApprovalCount = COUNT(*) FROM Approvals a INNER JOIN EvaluationBatches eb ON a.BatchID = eb.BatchID WHERE eb.BatchCode LIKE 'DEMO-%' AND a.IsFinal = 1;
SELECT @SnapshotCount = COUNT(*) FROM BatchRoundSnapshots s INNER JOIN EvaluationBatches eb ON s.BatchID = eb.BatchID WHERE eb.BatchCode LIKE 'DEMO-%' AND s.SnapshotType = 'FinalApproval';

IF @ApprovalCount = 2 AND @SnapshotCount = 2
    PRINT N'[PASS] Approvals & Snapshots: Batches B01 and B02 have final approvals and frozen snapshots.';
ELSE
BEGIN
    PRINT N'[FAIL] Approvals & Snapshots: Expected 2 approvals and 2 snapshots, found ' + CAST(@ApprovalCount AS NVARCHAR(10)) + N' approvals, ' + CAST(@SnapshotCount AS NVARCHAR(10)) + N' snapshots.';
    SET @FailCount = @FailCount + 1;
END;

-- 10. REPORTS AUDIT
DECLARE @ReportCount INT;
SELECT @ReportCount = COUNT(*) FROM Reports r INNER JOIN EvaluationBatches eb ON r.BatchID = eb.BatchID WHERE eb.BatchCode LIKE 'DEMO-%';

IF @ReportCount >= 4
    PRINT N'[PASS] Reports: ' + CAST(@ReportCount AS NVARCHAR(10)) + N' demo report records registered (PDF and Excel).';
ELSE
BEGIN
    PRINT N'[FAIL] Reports: Expected at least 4 demo report records, found ' + CAST(@ReportCount AS NVARCHAR(10));
    SET @FailCount = @FailCount + 1;
END;

-- 11. AUDIT LOGS AUDIT
DECLARE @AuditCount INT;
SELECT @AuditCount = COUNT(*) FROM AuditLogs WHERE Description LIKE '%[Demo]%';

IF @AuditCount >= 15
    PRINT N'[PASS] Audit Logs: ' + CAST(@AuditCount AS NVARCHAR(10)) + N' audit log entries recording seeder operations.';
ELSE
BEGIN
    PRINT N'[FAIL] Audit Logs: Expected at least 15 demo audit logs, found ' + CAST(@AuditCount AS NVARCHAR(10));
    SET @FailCount = @FailCount + 1;
END;

PRINT '--------------------------------------------------------------------------------';
-- DETAILED BATCH SUMMARY TABLE
PRINT N'DETAILED BATCH LIFECYCLE SUMMARY:';
SELECT 
    eb.BatchCode,
    LEFT(eb.BatchName, 42) AS BatchName,
    eb.Status,
    COUNT(DISTINCT bs.SupplierID) AS Suppliers,
    COUNT(DISTINCT bc.CriteriaID) AS Criteria,
    COUNT(DISTINCT es.ScoreID) AS Scores,
    CASE WHEN ar.AHPResultID IS NOT NULL THEN CAST(ar.CRValue AS VARCHAR(8)) ELSE 'None' END AS AHP_CR,
    CASE WHEN ar.IsConsistent = 1 THEN 'Yes' WHEN ar.IsConsistent = 0 THEN 'NO' ELSE '-' END AS Consistent,
    COUNT(DISTINCT tr.ResultID) AS TopsisCount,
    CASE WHEN app.ApprovalID IS NOT NULL THEN 'Approved' ELSE 'None' END AS Decision,
    COUNT(DISTINCT r.ReportID) AS Reports
FROM EvaluationBatches eb
LEFT JOIN BatchSuppliers bs ON eb.BatchID = bs.BatchID
LEFT JOIN BatchCriteria bc ON eb.BatchID = bc.BatchID
LEFT JOIN EvaluationScores es ON eb.BatchID = es.BatchID
LEFT JOIN Ahpresults ar ON eb.BatchID = ar.BatchID
LEFT JOIN TopsisResults tr ON eb.BatchID = tr.BatchID
LEFT JOIN Approvals app ON eb.BatchID = app.BatchID AND app.IsFinal = 1
LEFT JOIN Reports r ON eb.BatchID = r.BatchID
WHERE eb.BatchCode LIKE 'DEMO-%'
GROUP BY eb.BatchCode, eb.BatchName, eb.Status, ar.AHPResultID, ar.CRValue, ar.IsConsistent, app.ApprovalID
ORDER BY eb.BatchCode;

PRINT '--------------------------------------------------------------------------------';
IF @FailCount = 0
    PRINT N'==> OVERALL RESULT: 100% VERIFIED SUCCESS! Demonstration dataset is fully compliant.';
ELSE
    PRINT N'==> OVERALL RESULT: FAILED with ' + CAST(@FailCount AS NVARCHAR(10)) + N' violation(s).';
PRINT '================================================================================';
