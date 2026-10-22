/* =====================================================================
   PRODUCTION DEPLOYMENT WORKFLOW
   Database: SupplierSelectionDSS

   IMPORTANT:
   - DO NOT run 00_Create_Database.sql in production.
   - DO NOT run 09_Seed_Data.sql unless seed data is intentionally required.
   - Execute the files below in order.
   ===================================================================== */

-- 1.  00_Create_Database_Production.sql
-- 2.  01_Master_Data.sql
-- 3.  02_Evaluation_Data.sql
-- 4.  03_AHP.sql
-- 5.  04_TOPSIS.sql
-- 6.  05_Approval_Reports_AuditLogs.sql
-- 7.  06_Triggers.sql
-- 8.  07_Indexes.sql
-- 9.  08_Views.sql
-- 10. 10_Verify_Database.sql

-- 09_Seed_Data.sql is intentionally excluded from this workflow.
