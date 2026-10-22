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
