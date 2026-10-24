/* =====================================================================
   PRODUCTION DATABASE INITIALIZATION (SQLCMD Mode)
   Database Variable: $(DatabaseName)
   Purpose: Create a FRESH production database only if it does not already exist.

   SAFETY INVARIANTS:
   - Rejects execution if $(DatabaseName) already exists on this server.
   - Never modifies, alters recovery model, or writes to an existing database.
   - Rejects invalid database names or attempts to target system databases.
   - Must be executed in SQLCMD mode with :On Error exit.
   ===================================================================== */

USE master;
GO

-- 1. Validate DatabaseName variable
DECLARE @DbName NVARCHAR(128) = N'$(DatabaseName)';

IF @DbName IS NULL OR LTRIM(RTRIM(@DbName)) = N'' OR @DbName = N'$' + N'(DatabaseName)'
BEGIN
    RAISERROR(N'FATAL: Scripting variable $(DatabaseName) is undefined or empty. Pass -v DatabaseName="YourDbName" to sqlcmd.', 16, 1);
    SET NOEXEC ON;
END
GO

-- 2. Reject system databases or illegal identifier characters
DECLARE @DbName NVARCHAR(128) = N'$(DatabaseName)';

IF LOWER(LTRIM(RTRIM(@DbName))) IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR(N'FATAL: Target database cannot be a system database ([%s]). Aborting.', 16, 1, @DbName);
    SET NOEXEC ON;
END

IF @DbName LIKE N'%[^A-Za-z0-9_]%'
BEGIN
    RAISERROR(N'FATAL: Database name [%s] contains invalid characters. Only alphanumeric and underscore characters are allowed.', 16, 1, @DbName);
    SET NOEXEC ON;
END
GO

-- 3. Strict Pre-existence Guard: Refuse fresh provisioning if database already exists
IF DB_ID(N'$(DatabaseName)') IS NOT NULL
BEGIN
    RAISERROR(
N'=============================================================================
FATAL: PRODUCTION PROVISIONING ABORTED
Database [%s] ALREADY EXISTS on this SQL Server instance!

Run_Production.sql is strictly intended for FRESH initial provisioning of a new database.
It will NOT overwrite, modify recovery models, or alter an existing database.

- To upgrade an existing database, run the incremental patches in database/patches/
  in chronological order.
- To safely test fresh provisioning, target a new disposable database name:
  sqlcmd -S <server> -b -v DatabaseName=SupplierSelectionDSS_Test_Fresh -i Run_Production.sql
=============================================================================', 16, 1, N'$(DatabaseName)');
    SET NOEXEC ON;
END
ELSE
BEGIN
    PRINT N'Database [$(DatabaseName)] does not exist. Creating fresh production database...';
    CREATE DATABASE [$(DatabaseName)]
    COLLATE Vietnamese_CI_AS;

    ALTER DATABASE [$(DatabaseName)] SET RECOVERY FULL;
    ALTER DATABASE [$(DatabaseName)] SET READ_COMMITTED_SNAPSHOT ON;
    PRINT N'Database [$(DatabaseName)] created successfully with RECOVERY FULL and READ_COMMITTED_SNAPSHOT ON.';
END
GO

-- 4. Switch context to the newly created target database
USE [$(DatabaseName)];
GO

-- 5. Verify that current execution context is strictly the target database
DECLARE @CurrentContext NVARCHAR(128) = DB_NAME();
IF @CurrentContext <> N'$(DatabaseName)' OR @CurrentContext IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR(N'FATAL: Failed to switch to target database [%s]. Current context is [%s]. Aborting.', 16, 1, N'$(DatabaseName)', @CurrentContext);
    SET NOEXEC ON;
END
GO

PRINT N'Database context switched to target database. Ready for business DDL.';
GO
