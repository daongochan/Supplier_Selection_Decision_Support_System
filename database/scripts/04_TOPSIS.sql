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
