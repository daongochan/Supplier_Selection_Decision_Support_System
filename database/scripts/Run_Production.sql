/* =====================================================================
   PRODUCTION PROVISIONING RUNNER (SQLCMD Mode)
   Database: SupplierSelectionDSS

   IMPORTANT:
   - This script provisions a FRESH production database from scratch.
   - For upgrading an ALREADY RUNNING database, DO NOT run this script.
     Instead, apply the incremental idempotent patches in database/patches/
     in chronological order.
   - Must be executed in SQLCMD mode with the working directory set to:
     database/scripts/
     Example:
       cd database/scripts
       sqlcmd -S <server> -U <user> -P <password> -b -i Run_Production.sql
   ===================================================================== */

:On Error exit

PRINT N'=====================================================================';
PRINT N'STARTING PRODUCTION DATABASE PROVISIONING: SupplierSelectionDSS';
PRINT N'=====================================================================';

PRINT N'[1/10] Initializing database (non-destructive safety check)...';
:r 00_Create_Database_Production.sql

PRINT N'[2/10] Creating Master Data tables (Users, ProductGroups, Suppliers, Criteria)...';
:r 01_Master_Data.sql

PRINT N'[3/10] Creating Evaluation Data tables (Batches, Suppliers, Criteria, Scores)...';
:r 02_Evaluation_Data.sql

PRINT N'[4/10] Creating AHP tables (Comparisons, CriteriaWeights, Results)...';
:r 03_AHP.sql

PRINT N'[5/10] Creating TOPSIS tables (IdealSolutions, Results)...';
:r 04_TOPSIS.sql

PRINT N'[6/10] Creating Approvals, Reports, AuditLogs and BatchRoundSnapshots...';
:r 05_Approval_Reports_AuditLogs.sql

PRINT N'[7/10] Creating business rule triggers (status protection & invariants)...';
:r 06_Triggers.sql

PRINT N'[8/10] Creating performance indexes...';
:r 07_Indexes.sql

PRINT N'[9/10] Creating analytical and reporting views...';
:r 08_Views.sql

PRINT N'[10/10] Verifying database structure...';
:r 10_Verify_Database.sql

PRINT N'=====================================================================';
PRINT N'PRODUCTION DATABASE PROVISIONING COMPLETED SUCCESSFULLY.';
PRINT N'Note: Demo seed data was EXCLUDED. Bootstrap your production';
PRINT N'administrator account using a secure, authorized procedure.';
PRINT N'=====================================================================';
GO
