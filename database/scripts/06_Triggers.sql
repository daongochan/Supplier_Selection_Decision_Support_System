/* =====================================================================
   6. TRIGGER KIỂM TRA TÍNH TOÀN VẸN NGHIỆP VỤ
   ===================================================================== */

-- 6.1. EvaluationScores: Criteria phải thuộc BatchCriteria
CREATE TRIGGER TR_EvaluationScores_ValidateCriteria
ON EvaluationScores
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1 FROM BatchCriteria bc
            WHERE bc.BatchID = i.BatchID AND bc.CriteriaID = i.CriteriaID
        )
    )
    BEGIN
        RAISERROR(N'CriteriaID phải thuộc danh sách BatchCriteria của Batch tương ứng.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 6.2. EvaluationScores: Supplier phải thuộc BatchSuppliers
CREATE TRIGGER TR_EvaluationScores_ValidateSupplier
ON EvaluationScores
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1 FROM BatchSuppliers bs
            WHERE bs.BatchID = i.BatchID AND bs.SupplierID = i.SupplierID
        )
    )
    BEGIN
        RAISERROR(N'SupplierID phải thuộc danh sách BatchSuppliers của Batch tương ứng.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 6.3. AHPPairwiseComparisons: CriteriaRowID & CriteriaColID phải thuộc BatchCriteria
CREATE TRIGGER TR_AHPPairwiseComparisons_ValidateCriteria
ON AHPPairwiseComparisons
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1 FROM BatchCriteria bc
            WHERE bc.BatchID = i.BatchID AND bc.CriteriaID = i.CriteriaRowID
        )
        OR NOT EXISTS (
            SELECT 1 FROM BatchCriteria bc
            WHERE bc.BatchID = i.BatchID AND bc.CriteriaID = i.CriteriaColID
        )
    )
    BEGIN
        RAISERROR(N'CriteriaRowID và CriteriaColID phải thuộc danh sách BatchCriteria của Batch tương ứng.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 6.4. AHPCriteriaWeights: Criteria phải thuộc BatchCriteria
CREATE TRIGGER TR_AHPCriteriaWeights_ValidateCriteria
ON AHPCriteriaWeights
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1 FROM BatchCriteria bc
            WHERE bc.BatchID = i.BatchID AND bc.CriteriaID = i.CriteriaID
        )
    )
    BEGIN
        RAISERROR(N'CriteriaID phải thuộc danh sách BatchCriteria của Batch tương ứng.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 6.5. TopsisIdealSolutions: Criteria phải thuộc BatchCriteria
CREATE TRIGGER TR_TopsisIdealSolutions_ValidateCriteria
ON TopsisIdealSolutions
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1 FROM BatchCriteria bc
            WHERE bc.BatchID = i.BatchID AND bc.CriteriaID = i.CriteriaID
        )
    )
    BEGIN
        RAISERROR(N'CriteriaID phải thuộc danh sách BatchCriteria của Batch tương ứng.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 6.6. TopsisResults: Supplier phải thuộc BatchSuppliers
CREATE TRIGGER TR_TopsisResults_ValidateSupplier
ON TopsisResults
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE NOT EXISTS (
            SELECT 1 FROM BatchSuppliers bs
            WHERE bs.BatchID = i.BatchID AND bs.SupplierID = i.SupplierID
        )
    )
    BEGIN
        RAISERROR(N'SupplierID phải thuộc danh sách BatchSuppliers của Batch tương ứng trước khi lưu kết quả TOPSIS.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 6.7. Approvals: SelectedSupplier phải thuộc BatchSuppliers (nếu có chọn)
CREATE TRIGGER TR_Approvals_ValidateSupplier
ON Approvals
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE i.SelectedSupplierID IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM BatchSuppliers bs
              WHERE bs.BatchID = i.BatchID AND bs.SupplierID = i.SelectedSupplierID
          )
    )
    BEGIN
        RAISERROR(N'SelectedSupplierID phải thuộc danh sách BatchSuppliers của Batch tương ứng.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO
