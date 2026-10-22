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
