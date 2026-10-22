# SupplierSelectionDSS - SQL Deployment Scripts

Bộ script được tách từ bản Final – 2026 do người dùng cung cấp. Nội dung gốc được giữ theo từng khối chức năng.

## Thứ tự chạy
1. 00_Create_Database.sql
2. 01_Master_Data.sql
3. 02_Evaluation_Data.sql
4. 03_AHP.sql
5. 04_TOPSIS.sql
6. 05_Approval_Reports_AuditLogs.sql
7. 06_Triggers.sql
8. 07_Indexes.sql
9. 08_Views.sql
10. 09_Seed_Data.sql
11. 10_Verify_Database.sql (chỉ kiểm tra)

Có thể chạy toàn bộ bằng `Run_All.sql`.

## Lưu ý
`00_Create_Database.sql` chứa logic xóa database `SupplierSelectionDSS` nếu database đã tồn tại, giống bản gốc. Chỉ dùng file này cho môi trường Development/Reset. Không chạy trên Production khi database đã có dữ liệu.
