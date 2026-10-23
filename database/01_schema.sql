/* ================================================================
   MASTER SCRIPT - SUPPLIER SELECTION DSS
   Thứ tự chạy: 00 -> 09
   ================================================================ */

/* ==================== 00_Create_Database.sql ==================== */

USE master;
GO

-- =====================================================================
-- SAFETY GUARD: Chống xóa nhầm cơ sở dữ liệu SupplierSelectionDSS
-- =====================================================================
IF DB_ID(N'SupplierSelectionDSS') IS NOT NULL
BEGIN
    DECLARE @ConfirmRecreate NVARCHAR(50) = N'$(ConfirmRecreate)';

    IF UPPER(LTRIM(RTRIM(@ConfirmRecreate))) <> N'YES'
    BEGIN
        RAISERROR(
N'=============================================================================
CẢNH BÁO AN TOÀN DỮ LIỆU (DATA LOSS PREVENTION):
Cơ sở dữ liệu [SupplierSelectionDSS] ĐÃ TỒN TẠI trên SQL Server!
Thực thi script này sẽ XÓA SẠCH toàn bộ bảng và dữ liệu hiện có.

Để tiếp tục xóa và tạo mới hoàn toàn, bạn BẮT BUỘC phải truyền biến xác nhận:
  - Qua sqlcmd : sqlcmd -S . -E -C -v ConfirmRecreate=YES -i database/01_schema.sql
  - Trong SSMS : Bật "SQLCMD Mode" (Query -> SQLCMD Mode) và truyền ConfirmRecreate=YES

Quá trình khởi tạo đã dừng lại để bảo vệ an toàn dữ liệu của bạn.
=============================================================================', 16, 1);
        SET NOEXEC ON;
    END
    ELSE
    BEGIN
        PRINT N'Xác nhận ConfirmRecreate=YES hợp lệ. Đang tiến hành xóa và khởi tạo lại [SupplierSelectionDSS]...';
        ALTER DATABASE SupplierSelectionDSS SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE SupplierSelectionDSS;
    END
END
GO

CREATE DATABASE SupplierSelectionDSS
COLLATE Vietnamese_CI_AS;
GO

ALTER DATABASE SupplierSelectionDSS SET RECOVERY SIMPLE;
GO

USE SupplierSelectionDSS;

GO

/* ==================== 01_Master_Data.sql ==================== */

/* =====================================================================
   1. NHÓM DANH MỤC (MASTER DATA)
   ===================================================================== */

-- 1.1. Người dùng hệ thống
CREATE TABLE Users (
    UserID        INT IDENTITY(1,1) NOT NULL,
    Username      NVARCHAR(50)   NOT NULL,
    PasswordHash  NVARCHAR(255)  NOT NULL,          -- Hash (bcrypt / argon2 / PBKDF2)
    FullName      NVARCHAR(100)  NOT NULL,
    Email         NVARCHAR(100)  NULL,
    Phone         NVARCHAR(20)   NULL,
    Role          NVARCHAR(30)   NOT NULL,          -- Administrator | PurchasingStaff | Expert | Manager
    Status        NVARCHAR(15)   NOT NULL CONSTRAINT DF_Users_Status DEFAULT (N'Active'),
    LastLoginAt   DATETIME2      NULL,
    CreatedAt     DATETIME2      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt     DATETIME2      NULL,
    CONSTRAINT PK_Users PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT CK_Users_Role   CHECK (Role IN (N'Administrator', N'PurchasingStaff', N'Expert', N'Manager')),
    CONSTRAINT CK_Users_Status CHECK (Status IN (N'Active', N'Locked', N'Inactive'))
);
GO

-- 1.2. Nhóm hàng hoá / dịch vụ
CREATE TABLE ProductGroups (
    GroupID       INT IDENTITY(1,1) NOT NULL,
    GroupCode     NVARCHAR(20)   NOT NULL,
    GroupName     NVARCHAR(150)  NOT NULL,
    Description   NVARCHAR(500)  NULL,
    Status        NVARCHAR(15)   NOT NULL CONSTRAINT DF_ProductGroups_Status DEFAULT (N'Active'),
    CreatedAt     DATETIME2      NOT NULL CONSTRAINT DF_ProductGroups_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt     DATETIME2      NULL,
    CONSTRAINT PK_ProductGroups PRIMARY KEY (GroupID),
    CONSTRAINT UQ_ProductGroups_Code UNIQUE (GroupCode),
    CONSTRAINT CK_ProductGroups_Status CHECK (Status IN (N'Active', N'Inactive'))
);
GO

-- 1.3. Nhà cung cấp
CREATE TABLE Suppliers (
    SupplierID     INT IDENTITY(1,1) NOT NULL,
    SupplierCode   NVARCHAR(20)   NOT NULL,
    SupplierName   NVARCHAR(200)  NOT NULL,
    Address        NVARCHAR(255)  NULL,
    ContactPerson  NVARCHAR(100)  NULL,
    Email          NVARCHAR(100)  NULL,
    Phone          NVARCHAR(20)   NULL,
    TaxCode        NVARCHAR(20)   NULL,
    Website        NVARCHAR(255)  NULL,
    BusinessField  NVARCHAR(150)  NULL,
    PaymentTerms   NVARCHAR(255)  NULL,
    Status         NVARCHAR(15)   NOT NULL CONSTRAINT DF_Suppliers_Status DEFAULT (N'Active'),
    CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt      DATETIME2      NULL,
    CONSTRAINT PK_Suppliers PRIMARY KEY (SupplierID),
    CONSTRAINT UQ_Suppliers_Code UNIQUE (SupplierCode),
    CONSTRAINT CK_Suppliers_Status CHECK (Status IN (N'Active', N'Inactive'))
);
GO

-- 1.4. Quan hệ N-N Nhà cung cấp ↔ Nhóm hàng hoá
CREATE TABLE SupplierGroups (
    SupplierID  INT NOT NULL,
    GroupID     INT NOT NULL,
    CONSTRAINT PK_SupplierGroups PRIMARY KEY (SupplierID, GroupID),
    CONSTRAINT FK_SupplierGroups_Supplier FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID),
    CONSTRAINT FK_SupplierGroups_Group    FOREIGN KEY (GroupID)    REFERENCES ProductGroups(GroupID)
);
GO

-- 1.5. Tiêu chí đánh giá
CREATE TABLE Criteria (
    CriteriaID    INT IDENTITY(1,1) NOT NULL,
    CriteriaCode  NVARCHAR(20)   NOT NULL,
    CriteriaName  NVARCHAR(150)  NOT NULL,
    Description   NVARCHAR(500)  NULL,
    CriteriaType  NVARCHAR(10)   NOT NULL,   -- Benefit | Cost
    Unit          NVARCHAR(30)   NULL,
    Status        NVARCHAR(15)   NOT NULL CONSTRAINT DF_Criteria_Status DEFAULT (N'Active'),
    CreatedAt     DATETIME2      NOT NULL CONSTRAINT DF_Criteria_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt     DATETIME2      NULL,
    CONSTRAINT PK_Criteria PRIMARY KEY (CriteriaID),
    CONSTRAINT UQ_Criteria_Code UNIQUE (CriteriaCode),
    CONSTRAINT CK_Criteria_Type   CHECK (CriteriaType IN (N'Benefit', N'Cost')),
    CONSTRAINT CK_Criteria_Status CHECK (Status IN (N'Active', N'Inactive'))
);
GO

GO

/* ==================== 02_Evaluation_Data.sql ==================== */

/* =====================================================================
   2. NHÓM ĐỢT ĐÁNH GIÁ / DỮ LIỆU NGHIỆP VỤ
   ===================================================================== */

-- 2.1. Đợt đánh giá
CREATE TABLE EvaluationBatches (
    BatchID      INT IDENTITY(1,1) NOT NULL,
    BatchCode    NVARCHAR(20)   NOT NULL,
    BatchName    NVARCHAR(200)  NOT NULL,
    Description  NVARCHAR(500)  NULL,
    GroupID      INT            NOT NULL,
    StartDate    DATE           NULL,
    EndDate      DATE           NULL,
    Status       NVARCHAR(20)   NOT NULL CONSTRAINT DF_Batch_Status DEFAULT (N'Draft'),
    CreatedBy    INT            NOT NULL,
    CreatedAt    DATETIME2      NOT NULL CONSTRAINT DF_Batch_CreatedAt DEFAULT (SYSDATETIME()),
    CompletedAt  DATETIME2      NULL,
    CancelledAt  DATETIME2      NULL,
    RowVersion   ROWVERSION     NOT NULL,
    CONSTRAINT PK_EvaluationBatches PRIMARY KEY (BatchID),
    CONSTRAINT UQ_Batch_Code UNIQUE (BatchCode),
    CONSTRAINT FK_Batch_Group FOREIGN KEY (GroupID)   REFERENCES ProductGroups(GroupID),
    CONSTRAINT FK_Batch_User  FOREIGN KEY (CreatedBy) REFERENCES Users(UserID),
    CONSTRAINT CK_Batch_Status CHECK (Status IN (N'Draft', N'Evaluating', N'Completed', N'Cancelled')),
    CONSTRAINT CK_Batch_Dates  CHECK (EndDate IS NULL OR StartDate IS NULL OR EndDate >= StartDate)
);
GO

-- 2.2. Nhà cung cấp tham gia đợt đánh giá
CREATE TABLE BatchSuppliers (
    BatchID     INT NOT NULL,
    SupplierID  INT NOT NULL,
    CONSTRAINT PK_BatchSuppliers PRIMARY KEY (BatchID, SupplierID),
    CONSTRAINT FK_BatchSuppliers_Batch    FOREIGN KEY (BatchID)    REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_BatchSuppliers_Supplier FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID)
);
GO

-- 2.3. Bộ tiêu chí áp dụng cho đợt đánh giá
CREATE TABLE BatchCriteria (
    BatchID     INT NOT NULL,
    CriteriaID  INT NOT NULL,
    CONSTRAINT PK_BatchCriteria PRIMARY KEY (BatchID, CriteriaID),
    CONSTRAINT FK_BatchCriteria_Batch    FOREIGN KEY (BatchID)    REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_BatchCriteria_Criteria FOREIGN KEY (CriteriaID) REFERENCES Criteria(CriteriaID)
);
GO

-- 2.4. Ma trận quyết định (điểm đánh giá)
CREATE TABLE EvaluationScores (
    ScoreID      INT IDENTITY(1,1) NOT NULL,
    BatchID      INT            NOT NULL,
    SupplierID   INT            NOT NULL,
    CriteriaID   INT            NOT NULL,
    ScoreValue   DECIMAL(18,4)  NOT NULL,
    EvaluatedBy  INT            NOT NULL,
    EvaluatedAt  DATETIME2      NOT NULL CONSTRAINT DF_Score_EvaluatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_EvaluationScores PRIMARY KEY (ScoreID),
    CONSTRAINT UQ_EvaluationScores UNIQUE (BatchID, SupplierID, CriteriaID),
    CONSTRAINT FK_Score_Batch    FOREIGN KEY (BatchID)     REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_Score_Supplier FOREIGN KEY (SupplierID)  REFERENCES Suppliers(SupplierID),
    CONSTRAINT FK_Score_Criteria FOREIGN KEY (CriteriaID)  REFERENCES Criteria(CriteriaID),
    CONSTRAINT FK_Score_User     FOREIGN KEY (EvaluatedBy) REFERENCES Users(UserID)
);
GO

GO

/* ==================== 03_AHP.sql ==================== */

/* =====================================================================
   3. NHÓM AHP
   ===================================================================== */

-- 3.1. Ma trận so sánh cặp (Saaty 1-9)
CREATE TABLE AHPPairwiseComparisons (
    ComparisonID     INT IDENTITY(1,1) NOT NULL,
    BatchID          INT            NOT NULL,
    CriteriaRowID    INT            NOT NULL,
    CriteriaColID    INT            NOT NULL,
    ComparisonValue  DECIMAL(10,4)  NOT NULL,
    EvaluatedBy      INT            NOT NULL,
    EvaluatedAt      DATETIME2      NOT NULL CONSTRAINT DF_AHPComp_EvaluatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_AHPPairwiseComparisons PRIMARY KEY (ComparisonID),
    CONSTRAINT UQ_AHPComparison UNIQUE (BatchID, CriteriaRowID, CriteriaColID),
    CONSTRAINT FK_AHPComp_Batch   FOREIGN KEY (BatchID)       REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_AHPComp_CritRow FOREIGN KEY (CriteriaRowID) REFERENCES Criteria(CriteriaID),
    CONSTRAINT FK_AHPComp_CritCol FOREIGN KEY (CriteriaColID) REFERENCES Criteria(CriteriaID),
    CONSTRAINT FK_AHPComp_User    FOREIGN KEY (EvaluatedBy)   REFERENCES Users(UserID),
    CONSTRAINT CK_AHPComp_Value   CHECK (ComparisonValue BETWEEN 0.1111 AND 9),
    CONSTRAINT CK_AHP_Diagonal CHECK (
        (CriteriaRowID = CriteriaColID AND ComparisonValue = 1)
        OR (CriteriaRowID <> CriteriaColID)
    )
);
GO

-- 3.2. Kết quả kiểm tra tính nhất quán AHP
CREATE TABLE AHPResults (
    AHPResultID   INT IDENTITY(1,1) NOT NULL,
    BatchID       INT            NOT NULL,
    LambdaMax     DECIMAL(10,6)  NOT NULL,
    CIValue       DECIMAL(10,6)  NOT NULL,
    RIValue       DECIMAL(10,6)  NOT NULL,
    CRValue       DECIMAL(10,6)  NOT NULL,
    IsConsistent  BIT            NOT NULL,
    n             INT            NULL,          -- Số tiêu chí
    CalculatedAt  DATETIME2      NOT NULL CONSTRAINT DF_AHPResults_CalculatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_AHPResults PRIMARY KEY (AHPResultID),
    CONSTRAINT UQ_AHPResults_Batch UNIQUE (BatchID),
    CONSTRAINT FK_AHPResults_Batch FOREIGN KEY (BatchID) REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT CK_AHPResults_n CHECK (n IS NULL OR n > 0)
);
GO

-- 3.3. Trọng số tiêu chí sau AHP
CREATE TABLE AHPCriteriaWeights (
    WeightID     INT IDENTITY(1,1) NOT NULL,
    BatchID      INT            NOT NULL,
    CriteriaID   INT            NOT NULL,
    WeightValue  DECIMAL(10,6)  NOT NULL,
    CONSTRAINT PK_AHPCriteriaWeights PRIMARY KEY (WeightID),
    CONSTRAINT UQ_AHPWeight UNIQUE (BatchID, CriteriaID),
    CONSTRAINT FK_AHPWeight_Batch    FOREIGN KEY (BatchID)    REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_AHPWeight_Criteria FOREIGN KEY (CriteriaID) REFERENCES Criteria(CriteriaID),
    CONSTRAINT CK_AHPWeight_Value    CHECK (WeightValue >= 0 AND WeightValue <= 1)
);
GO

GO

/* ==================== 04_TOPSIS.sql ==================== */

/* =====================================================================
   4. NHÓM TOPSIS
   ===================================================================== */

-- 4.1. Phương án lý tưởng dương (PIS) và âm (NIS)
CREATE TABLE TopsisIdealSolutions (
    IdealID     INT IDENTITY(1,1) NOT NULL,
    BatchID     INT            NOT NULL,
    CriteriaID  INT            NOT NULL,
    PISValue    DECIMAL(18,6)  NOT NULL,
    NISValue    DECIMAL(18,6)  NOT NULL,
    CONSTRAINT PK_TopsisIdealSolutions PRIMARY KEY (IdealID),
    CONSTRAINT UQ_TopsisIdeal UNIQUE (BatchID, CriteriaID),
    CONSTRAINT FK_Ideal_Batch    FOREIGN KEY (BatchID)    REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_Ideal_Criteria FOREIGN KEY (CriteriaID) REFERENCES Criteria(CriteriaID)
);
GO

-- 4.2. Kết quả xếp hạng TOPSIS
CREATE TABLE TopsisResults (
    ResultID              INT IDENTITY(1,1) NOT NULL,
    BatchID               INT            NOT NULL,
    SupplierID            INT            NOT NULL,
    DistanceToPIS         DECIMAL(18,6)  NOT NULL,
    DistanceToNIS         DECIMAL(18,6)  NOT NULL,
    ClosenessCoefficient  DECIMAL(10,6)  NOT NULL,
    RankNo                INT            NOT NULL,
    CalculatedAt          DATETIME2      NOT NULL CONSTRAINT DF_Topsis_CalculatedAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_TopsisResults PRIMARY KEY (ResultID),
    CONSTRAINT UQ_TopsisResult UNIQUE (BatchID, SupplierID),
    CONSTRAINT FK_Topsis_Batch    FOREIGN KEY (BatchID)    REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_Topsis_Supplier FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID),
    CONSTRAINT CK_Topsis_CC CHECK (ClosenessCoefficient BETWEEN 0 AND 1)
);
GO

GO

/* ==================== 05_Approval_Reports_AuditLogs.sql ==================== */

/* =====================================================================
   5. NHÓM PHÊ DUYỆT / BÁO CÁO / NHẬT KÝ
   ===================================================================== */

-- 5.1. Phê duyệt lựa chọn nhà cung cấp
CREATE TABLE Approvals (
    ApprovalID          INT IDENTITY(1,1) NOT NULL,
    BatchID             INT            NOT NULL,
    SelectedSupplierID  INT            NULL,
    ApprovedBy          INT            NOT NULL,
    ApprovalDate        DATETIME2      NOT NULL CONSTRAINT DF_Approval_Date DEFAULT (SYSDATETIME()),
    Notes               NVARCHAR(500)  NULL,
    Status              NVARCHAR(25)   NOT NULL,
    IsFinal             BIT            NOT NULL CONSTRAINT DF_Approval_IsFinal DEFAULT (0),
    CONSTRAINT PK_Approvals PRIMARY KEY (ApprovalID),
    CONSTRAINT FK_Approval_Batch    FOREIGN KEY (BatchID)            REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_Approval_Supplier FOREIGN KEY (SelectedSupplierID) REFERENCES Suppliers(SupplierID),
    CONSTRAINT FK_Approval_User     FOREIGN KEY (ApprovedBy)         REFERENCES Users(UserID),
    CONSTRAINT CK_Approval_Status   CHECK (Status IN (N'Approved', N'Rejected', N'PendingReevaluation'))
);
GO

-- 5.2. Báo cáo xuất ra
CREATE TABLE Reports (
    ReportID     INT IDENTITY(1,1) NOT NULL,
    BatchID      INT            NOT NULL,
    ReportType   NVARCHAR(10)   NOT NULL,
    GeneratedBy  INT            NOT NULL,
    GeneratedAt  DATETIME2      NOT NULL CONSTRAINT DF_Report_GeneratedAt DEFAULT (SYSDATETIME()),
    FilePath     NVARCHAR(255)  NULL,
    CONSTRAINT PK_Reports PRIMARY KEY (ReportID),
    CONSTRAINT FK_Report_Batch FOREIGN KEY (BatchID)     REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_Report_User  FOREIGN KEY (GeneratedBy) REFERENCES Users(UserID),
    CONSTRAINT CK_Report_Type  CHECK (ReportType IN (N'PDF', N'Excel'))
);
GO

-- 5.3. Nhật ký hoạt động (Audit Log)
CREATE TABLE AuditLogs (
    LogID        INT IDENTITY(1,1) NOT NULL,
    UserID       INT            NULL,
    ActionType   NVARCHAR(50)   NOT NULL,
    TargetTable  NVARCHAR(50)   NULL,
    TargetID     INT            NULL,
    OldValue     NVARCHAR(MAX)  NULL,
    NewValue     NVARCHAR(MAX)  NULL,
    ActionTime   DATETIME2      NOT NULL CONSTRAINT DF_AuditLog_ActionTime DEFAULT (SYSDATETIME()),
    Description  NVARCHAR(500)  NULL,
    CONSTRAINT PK_AuditLogs PRIMARY KEY (LogID),
    CONSTRAINT FK_AuditLog_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- 5.4. Lưu vết ảnh chụp nhanh đợt đánh giá (Batch Round Snapshot)
CREATE TABLE BatchRoundSnapshots (
    SnapshotID          INT IDENTITY(1,1) NOT NULL,
    BatchID             INT            NOT NULL,
    SnapshotType        NVARCHAR(50)   NOT NULL,
    SnapshotData        NVARCHAR(MAX)  NOT NULL,
    CreatedAt           DATETIME2      NOT NULL CONSTRAINT DF_BatchRoundSnapshots_CreatedAt DEFAULT (SYSDATETIME()),
    CreatedBy           INT            NULL,
    CONSTRAINT PK_BatchRoundSnapshots PRIMARY KEY (SnapshotID),
    CONSTRAINT FK_BatchRoundSnapshots_Batch FOREIGN KEY (BatchID) REFERENCES EvaluationBatches(BatchID),
    CONSTRAINT FK_BatchRoundSnapshots_User FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
);
GO

GO

/* ==================== 06_Triggers.sql ==================== */

/* =====================================================================
   6. TRIGGER KIỂM TRA TÍNH TOÀN VẸN NGHIỆP VỤ
   ===================================================================== */

-- 6.1. EvaluationScores: Criteria phải thuộc BatchCriteria và Batch chưa đóng
CREATE TRIGGER TR_EvaluationScores_ValidateCriteria
ON EvaluationScores
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN EvaluationBatches eb ON eb.BatchID = i.BatchID
        WHERE eb.Status IN (N'Completed', N'Cancelled')
    )
    BEGIN
        RAISERROR(N'Không thể thay đổi dữ liệu khi đợt đánh giá đã hoàn tất hoặc đã hủy.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

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

-- 6.3. AHPPairwiseComparisons: CriteriaRowID & CriteriaColID phải thuộc BatchCriteria và Batch chưa đóng
CREATE TRIGGER TR_AHPPairwiseComparisons_ValidateCriteria
ON AHPPairwiseComparisons
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN EvaluationBatches eb ON eb.BatchID = i.BatchID
        WHERE eb.Status IN (N'Completed', N'Cancelled')
    )
    BEGIN
        RAISERROR(N'Không thể thay đổi ma trận AHP khi đợt đánh giá đã hoàn tất hoặc đã hủy.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

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

-- 6.4. AHPCriteriaWeights: Criteria phải thuộc BatchCriteria và Batch chưa đóng
CREATE TRIGGER TR_AHPCriteriaWeights_ValidateCriteria
ON AHPCriteriaWeights
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN EvaluationBatches eb ON eb.BatchID = i.BatchID
        WHERE eb.Status IN (N'Completed', N'Cancelled')
    )
    BEGIN
        RAISERROR(N'Không thể thay đổi trọng số AHP khi đợt đánh giá đã hoàn tất hoặc đã hủy.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

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

-- 6.5. TopsisIdealSolutions: Criteria phải thuộc BatchCriteria và Batch chưa đóng
CREATE TRIGGER TR_TopsisIdealSolutions_ValidateCriteria
ON TopsisIdealSolutions
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN EvaluationBatches eb ON eb.BatchID = i.BatchID
        WHERE eb.Status IN (N'Completed', N'Cancelled')
    )
    BEGIN
        RAISERROR(N'Không thể thay đổi kết quả TOPSIS khi đợt đánh giá đã hoàn tất hoặc đã hủy.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

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

-- 6.6. TopsisResults: Supplier phải thuộc BatchSuppliers và Batch chưa đóng
CREATE TRIGGER TR_TopsisResults_ValidateSupplier
ON TopsisResults
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN EvaluationBatches eb ON eb.BatchID = i.BatchID
        WHERE eb.Status IN (N'Completed', N'Cancelled')
    )
    BEGIN
        RAISERROR(N'Không thể thay đổi kết quả TOPSIS khi đợt đánh giá đã hoàn tất hoặc đã hủy.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

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

-- 6.7. Approvals: SelectedSupplier phải thuộc BatchSuppliers (nếu có chọn) và không ghi đè quyết định đã chốt
CREATE TRIGGER TR_Approvals_ValidateSupplier
ON Approvals
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN EvaluationBatches eb ON eb.BatchID = i.BatchID
        WHERE eb.Status = N'Cancelled'
           OR (
               eb.Status = N'Completed'
               AND EXISTS (
                   SELECT 1 FROM Approvals a
                   WHERE a.BatchID = i.BatchID
                     AND a.IsFinal = 1
                     AND a.ApprovalID NOT IN (SELECT ApprovalID FROM inserted)
               )
           )
    )
    BEGIN
        RAISERROR(N'Không thể thêm hoặc sửa quyết định khi đợt đánh giá đã chốt hoặc đã hủy.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

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

GO

/* ==================== 07_Indexes.sql ==================== */

/* =====================================================================
   7. INDEX BỔ SUNG
   ===================================================================== */

CREATE INDEX IX_Users_Role_Status               ON Users(Role, Status);
CREATE INDEX IX_EvaluationBatches_Group         ON EvaluationBatches(GroupID);
CREATE INDEX IX_EvaluationBatches_Status        ON EvaluationBatches(Status);
CREATE INDEX IX_EvaluationScores_Batch          ON EvaluationScores(BatchID);
CREATE INDEX IX_EvaluationScores_Supplier       ON EvaluationScores(SupplierID);
CREATE INDEX IX_EvaluationScores_Batch_Supplier ON EvaluationScores(BatchID, SupplierID);
CREATE INDEX IX_AHPPairwise_Batch               ON AHPPairwiseComparisons(BatchID);
CREATE INDEX IX_AHPPairwise_Batch_Row           ON AHPPairwiseComparisons(BatchID, CriteriaRowID);
CREATE INDEX IX_AHPPairwise_Batch_Col           ON AHPPairwiseComparisons(BatchID, CriteriaColID);
CREATE INDEX IX_AHPCriteriaWeights_Batch        ON AHPCriteriaWeights(BatchID);
CREATE INDEX IX_TopsisIdeal_Batch               ON TopsisIdealSolutions(BatchID);
CREATE INDEX IX_TopsisIdeal_Criteria            ON TopsisIdealSolutions(CriteriaID);
CREATE INDEX IX_TopsisResults_Batch_Rank        ON TopsisResults(BatchID, RankNo);
CREATE INDEX IX_TopsisResults_CC                ON TopsisResults(BatchID, ClosenessCoefficient DESC);
CREATE INDEX IX_Suppliers_Status                ON Suppliers(Status);
CREATE INDEX IX_AuditLogs_User_Time             ON AuditLogs(UserID, ActionTime);
CREATE INDEX IX_EvaluationBatches_CreatedAt      ON EvaluationBatches(CreatedAt DESC);
CREATE INDEX IX_Reports_BatchID_GeneratedAt     ON Reports(BatchID, GeneratedAt DESC);
CREATE INDEX IX_Approvals_BatchID_ApprovalDate  ON Approvals(BatchID, ApprovalDate DESC);
CREATE INDEX IX_AuditLogs_ActionTime            ON AuditLogs(ActionTime DESC);
CREATE INDEX IX_BatchRoundSnapshots_Batch       ON BatchRoundSnapshots(BatchID, CreatedAt DESC);
GO

GO

/* ==================== 08_Views.sql ==================== */

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

SET NOEXEC OFF;
GO
