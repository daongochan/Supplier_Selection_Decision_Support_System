/* =====================================================================
   PATCH: 20261022_round3_indexes_and_snapshots.sql
   Mục đích:
   1. Bổ sung bảng BatchRoundSnapshots để lưu vết snapshot các vòng đánh giá.
   2. Bổ sung 4 index tối ưu hiệu năng:
      - IX_EvaluationBatches_CreatedAt ON EvaluationBatches(CreatedAt DESC)
      - IX_Reports_BatchID_GeneratedAt ON Reports(BatchID, GeneratedAt DESC)
      - IX_Approvals_BatchID_ApprovalDate ON Approvals(BatchID, ApprovalDate DESC)
      - IX_AuditLogs_ActionTime ON AuditLogs(ActionTime DESC)
   Tính chất: Idempotent - an toàn khi chạy nhiều lần.
   ===================================================================== */

-- 1. Bảng BatchRoundSnapshots
IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = N'BatchRoundSnapshots'
)
BEGIN
    PRINT 'Đang tạo bảng BatchRoundSnapshots...';
    CREATE TABLE dbo.BatchRoundSnapshots (
        SnapshotID          INT IDENTITY(1,1) NOT NULL,
        BatchID             INT            NOT NULL,
        SnapshotType        NVARCHAR(50)   NOT NULL,
        SnapshotData        NVARCHAR(MAX)  NOT NULL,
        CreatedAt           DATETIME2      NOT NULL CONSTRAINT DF_BatchRoundSnapshots_CreatedAt DEFAULT (SYSDATETIME()),
        CreatedBy           INT            NULL,
        CONSTRAINT PK_BatchRoundSnapshots PRIMARY KEY (SnapshotID),
        CONSTRAINT FK_BatchRoundSnapshots_Batch FOREIGN KEY (BatchID) REFERENCES dbo.EvaluationBatches(BatchID),
        CONSTRAINT FK_BatchRoundSnapshots_User FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserID)
    );
    PRINT 'Đã tạo bảng BatchRoundSnapshots thành công.';
END
ELSE
BEGIN
    PRINT 'Bảng BatchRoundSnapshots đã tồn tại. Bỏ qua.';
END
GO

-- 2. Index cho BatchRoundSnapshots
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = N'IX_BatchRoundSnapshots_Batch' 
      AND object_id = OBJECT_ID(N'dbo.BatchRoundSnapshots')
)
BEGIN
    PRINT 'Đang tạo index IX_BatchRoundSnapshots_Batch...';
    CREATE INDEX IX_BatchRoundSnapshots_Batch ON dbo.BatchRoundSnapshots(BatchID, CreatedAt DESC);
    PRINT 'Đã tạo index IX_BatchRoundSnapshots_Batch thành công.';
END
GO

-- 3. 4 Index tối ưu hiệu năng
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = N'IX_EvaluationBatches_CreatedAt' 
      AND object_id = OBJECT_ID(N'dbo.EvaluationBatches')
)
BEGIN
    PRINT 'Đang tạo index IX_EvaluationBatches_CreatedAt...';
    CREATE INDEX IX_EvaluationBatches_CreatedAt ON dbo.EvaluationBatches(CreatedAt DESC);
    PRINT 'Đã tạo index IX_EvaluationBatches_CreatedAt thành công.';
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = N'IX_Reports_BatchID_GeneratedAt' 
      AND object_id = OBJECT_ID(N'dbo.Reports')
)
BEGIN
    PRINT 'Đang tạo index IX_Reports_BatchID_GeneratedAt...';
    CREATE INDEX IX_Reports_BatchID_GeneratedAt ON dbo.Reports(BatchID, GeneratedAt DESC);
    PRINT 'Đã tạo index IX_Reports_BatchID_GeneratedAt thành công.';
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = N'IX_Approvals_BatchID_ApprovalDate' 
      AND object_id = OBJECT_ID(N'dbo.Approvals')
)
BEGIN
    PRINT 'Đang tạo index IX_Approvals_BatchID_ApprovalDate...';
    CREATE INDEX IX_Approvals_BatchID_ApprovalDate ON dbo.Approvals(BatchID, ApprovalDate DESC);
    PRINT 'Đã tạo index IX_Approvals_BatchID_ApprovalDate thành công.';
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = N'IX_AuditLogs_ActionTime' 
      AND object_id = OBJECT_ID(N'dbo.AuditLogs')
)
BEGIN
    PRINT 'Đang tạo index IX_AuditLogs_ActionTime...';
    CREATE INDEX IX_AuditLogs_ActionTime ON dbo.AuditLogs(ActionTime DESC);
    PRINT 'Đã tạo index IX_AuditLogs_ActionTime thành công.';
END
GO
