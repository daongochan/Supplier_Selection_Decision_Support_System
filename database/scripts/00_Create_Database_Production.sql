/* =====================================================================
   PRODUCTION DATABASE INITIALIZATION
   Database: SupplierSelectionDSS
   Purpose: Create the database only if it does not already exist.

   WARNING:
   - NEVER drops or recreates an existing production database.
   - Run this file before the schema files.
   ===================================================================== */

USE master;
GO

IF DB_ID(N'SupplierSelectionDSS') IS NULL
BEGIN
    PRINT N'Creating production database SupplierSelectionDSS...';

    CREATE DATABASE SupplierSelectionDSS
    COLLATE Vietnamese_CI_AS;
END
ELSE
BEGIN
    PRINT N'Database SupplierSelectionDSS already exists. No destructive action performed.';
END
GO

-- Production recovery model.
-- FULL is recommended when transaction-log backups / point-in-time restore are required.
ALTER DATABASE SupplierSelectionDSS SET RECOVERY FULL;
GO

PRINT N'Production database initialization completed.';
GO
