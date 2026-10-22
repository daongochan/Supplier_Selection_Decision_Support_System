# Production SQL Deployment

## Deployment order

1. `00_Create_Database_Production.sql`
2. `01_Master_Data.sql`
3. `02_Evaluation_Data.sql`
4. `03_AHP.sql`
5. `04_TOPSIS.sql`
6. `05_Approval_Reports_AuditLogs.sql`
7. `06_Triggers.sql`
8. `07_Indexes.sql`
9. `08_Views.sql`
10. `10_Verify_Database.sql`

## Important

- `00_Create_Database_Production.sql` creates `SupplierSelectionDSS` only when it does not exist.
- It never drops, recreates, or deletes an existing production database.
- The production database is set to `FULL` recovery mode.
- `09_Seed_Data.sql` is intentionally excluded from the production workflow.
- Take a backup before schema changes and validate changes in a staging environment first.
- Configure regular full/differential/log backups separately according to the production operating policy.
