/* ================================================================
   KIỂM TRA CSDL SAU KHI DEPLOY (SQLCMD Mode)
   Chạy sau khi đã chạy 00 -> 08
   ================================================================ */

USE [$(DatabaseName)];
GO

DECLARE @CurrentContext NVARCHAR(128) = DB_NAME();
IF @CurrentContext <> N'$(DatabaseName)'
BEGIN
    RAISERROR(N'FATAL: Expected database [%s], but current database is [%s]. Aborting verification.', 16, 1, N'$(DatabaseName)', @CurrentContext);
    SET NOEXEC ON;
END
GO

PRINT N'---------------------------------------------------------------------';
PRINT N'VERIFYING DATABASE OBJECTS IN [$(DatabaseName)]...';
PRINT N'---------------------------------------------------------------------';

-- 1. Assert table count = 18
DECLARE @TableCount INT;
SELECT @TableCount = COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = N'BASE TABLE';
IF @TableCount <> 18
BEGIN
    RAISERROR(N'VERIFICATION FAILED: Expected 18 base tables, found %d.', 16, 1, @TableCount);
    SET NOEXEC ON;
END
ELSE
BEGIN
    PRINT N'[OK] Base tables verified: 18 tables present.';
END
GO

-- 2. Assert view count = 2
DECLARE @ViewCount INT;
SELECT @ViewCount = COUNT(*) FROM sys.views;
IF @ViewCount <> 2
BEGIN
    RAISERROR(N'VERIFICATION FAILED: Expected 2 views, found %d.', 16, 1, @ViewCount);
    SET NOEXEC ON;
END
ELSE
BEGIN
    PRINT N'[OK] Views verified: 2 views present (vw_BatchRanking, vw_BatchCriteriaWeights).';
END
GO

-- 3. Assert trigger count = 7
DECLARE @TriggerCount INT;
SELECT @TriggerCount = COUNT(*) FROM sys.triggers WHERE parent_class_desc = N'OBJECT_OR_COLUMN';
IF @TriggerCount <> 7
BEGIN
    RAISERROR(N'VERIFICATION FAILED: Expected 7 triggers, found %d.', 16, 1, @TriggerCount);
    SET NOEXEC ON;
END
ELSE
BEGIN
    PRINT N'[OK] Triggers verified: 7 business triggers present.';
END
GO

-- 4. Assert index count = 21 (non-PK, non-UQ custom user-table indexes)
DECLARE @IndexCount INT;
SELECT @IndexCount = COUNT(*)
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.is_ms_shipped = 0
  AND i.is_primary_key = 0
  AND i.is_unique_constraint = 0
  AND i.name LIKE 'IX_%';
IF @IndexCount <> 21
BEGIN
    RAISERROR(N'VERIFICATION FAILED: Expected 21 custom performance indexes, found %d.', 16, 1, @IndexCount);
    SET NOEXEC ON;
END
ELSE
BEGIN
    PRINT N'[OK] Performance indexes verified: 21 indexes present.';
END
GO

-- 5. Assert NO demo users exist in fresh production database
DECLARE @UserCount INT;
SELECT @UserCount = COUNT(*) FROM Users;
IF @UserCount <> 0
BEGIN
    RAISERROR(N'VERIFICATION FAILED: Fresh production database must not contain demo seed users. Found %d users.', 16, 1, @UserCount);
    SET NOEXEC ON;
END
ELSE
BEGIN
    PRINT N'[OK] Clean user table verified: 0 demo users present.';
END
GO
