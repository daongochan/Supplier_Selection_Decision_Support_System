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
