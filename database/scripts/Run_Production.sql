/* =====================================================================
   PRODUCTION PROVISIONING RUNNER (SQLCMD Mode)
   Target Database: $(DatabaseName) (Standard default: SupplierSelectionDSS)

   IMPORTANT:
   - This script provisions a FRESH production database from scratch.
   - For upgrading an ALREADY RUNNING database, DO NOT run this script.
     Instead, apply the incremental idempotent patches in database/patches/
     in chronological order.
   - Must be executed in SQLCMD mode with the working directory set to:
     database/scripts/
     Example:
       cd database/scripts
       sqlcmd -S <server> -U <user> -P <password> -b -v DatabaseName=SupplierSelectionDSS -i Run_Production.sql
   ===================================================================== */

:On Error exit

PRINT N'=====================================================================';
PRINT N'STARTING FRESH PRODUCTION PROVISIONING: [$(DatabaseName)]';
PRINT N'=====================================================================';
GO

PRINT N'[1/10] Creating and validating database context...';
GO
:r 00_Create_Database_Production.sql

-- Safety guard: confirm we are inside $(DatabaseName) before any DDL
DECLARE @CurrentContext NVARCHAR(128) = DB_NAME();
IF @CurrentContext <> N'$(DatabaseName)' OR @CurrentContext IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR(N'FATAL: Current database is [%s], expected [%s]. Aborting business DDL.', 16, 1, @CurrentContext, N'$(DatabaseName)');
    SET NOEXEC ON;
END
GO

PRINT N'[2/10] Creating Master Data tables (Users, ProductGroups, Suppliers, Criteria)...';
GO
:r 01_Master_Data.sql

IF DB_NAME() <> N'$(DatabaseName)' BEGIN RAISERROR(N'Context corrupted during DDL', 16, 1); SET NOEXEC ON; END
GO

PRINT N'[3/10] Creating Evaluation Data tables (Batches, Suppliers, Criteria, Scores)...';
GO
:r 02_Evaluation_Data.sql

IF DB_NAME() <> N'$(DatabaseName)' BEGIN RAISERROR(N'Context corrupted during DDL', 16, 1); SET NOEXEC ON; END
GO

PRINT N'[4/10] Creating AHP tables (Comparisons, CriteriaWeights, Results)...';
GO
:r 03_AHP.sql

IF DB_NAME() <> N'$(DatabaseName)' BEGIN RAISERROR(N'Context corrupted during DDL', 16, 1); SET NOEXEC ON; END
GO

PRINT N'[5/10] Creating TOPSIS tables (IdealSolutions, Results)...';
GO
:r 04_TOPSIS.sql

IF DB_NAME() <> N'$(DatabaseName)' BEGIN RAISERROR(N'Context corrupted during DDL', 16, 1); SET NOEXEC ON; END
GO

PRINT N'[6/10] Creating Approvals, Reports, AuditLogs and BatchRoundSnapshots...';
GO
:r 05_Approval_Reports_AuditLogs.sql

IF DB_NAME() <> N'$(DatabaseName)' BEGIN RAISERROR(N'Context corrupted during DDL', 16, 1); SET NOEXEC ON; END
GO

PRINT N'[7/10] Creating business rule triggers (status protection & invariants)...';
GO
:r 06_Triggers.sql

IF DB_NAME() <> N'$(DatabaseName)' BEGIN RAISERROR(N'Context corrupted during DDL', 16, 1); SET NOEXEC ON; END
GO

PRINT N'[8/10] Creating performance indexes...';
GO
:r 07_Indexes.sql

IF DB_NAME() <> N'$(DatabaseName)' BEGIN RAISERROR(N'Context corrupted during DDL', 16, 1); SET NOEXEC ON; END
GO

PRINT N'[9/10] Creating analytical and reporting views...';
GO
:r 08_Views.sql

IF DB_NAME() <> N'$(DatabaseName)' BEGIN RAISERROR(N'Context corrupted during DDL', 16, 1); SET NOEXEC ON; END
GO

PRINT N'[10/10] Verifying database structure...';
GO
:r 10_Verify_Database.sql

PRINT N'=====================================================================';
PRINT N'PRODUCTION DATABASE PROVISIONING COMPLETED SUCCESSFULLY.';
PRINT N'Target Database: [$(DatabaseName)]';
PRINT N'Demo seed data was EXCLUDED.';
PRINT N'Bootstrap your production administrator account using a secure, authorized procedure.';
PRINT N'=====================================================================';
GO
