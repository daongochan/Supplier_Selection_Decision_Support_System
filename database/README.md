# Hướng Dẫn Cơ Sở Dữ Liệu — Supplier Selection DSS

Tài liệu này hướng dẫn quản lý, khởi tạo mới và nâng cấp cơ sở dữ liệu SQL Server cho hệ thống **Supplier Selection Decision Support System**.

---

## 1. Nguồn Chuẩn (Single Source of Truth)

Theo quy ước tại [AGENTS.md](../AGENTS.md):
- **Cấu trúc CSDL (Schema):** `database/01_schema.sql` là nguồn sự thật duy nhất cho định nghĩa schema (18 bảng, 7 triggers, 2 views, 21 indexes).
- **Dữ liệu khởi tạo phát triển (Development Seed):** `database/02_seed.sql` là nguồn sự thật duy nhất cho dữ liệu mẫu dev/demo (băm BCrypt cost 11).
- **Vá cấu trúc tăng dần (Incremental Patches):** Thư mục `database/patches/` chứa các bản vá idempotent để nâng cấp database đang vận hành mà không làm mất dữ liệu.

---

## 2. Khởi Tạo Cơ Sở Dữ Liệu Mới (Fresh Installation)

> [!CAUTION]
> **CHỐT AN TOÀN (DATA LOSS PREVENTION):**
> File `01_schema.sql` và `scripts/Run_All.sql` được trang bị Safety Guard. Nếu database `SupplierSelectionDSS` **đã tồn tại**, script sẽ dừng ngay lập tức (`SET NOEXEC ON`) trừ khi bạn chủ động truyền `ConfirmRecreate=YES`.

### 2.1. Môi trường Phát triển (Development)

Mở PowerShell tại thư mục gốc của repository:

**Bước 1: Tạo Schema (chỉ chạy khi database chưa tồn tại):**
```powershell
sqlcmd -S . -E -C -b -v ConfirmRecreate=NO -i database/01_schema.sql
```

*(Giải thích cờ lệnh: `-S .` chọn instance cục bộ; `-E` dùng Windows Authentication; `-C` tin cậy chứng chỉ tự ký; `-b` dừng ngay khi có lỗi batch; `-v ConfirmRecreate=NO` đảm bảo biến được định nghĩa an toàn).*

**Bước 2: Nạp dữ liệu mẫu khởi tạo (chỉ chạy khi bước 1 thành công):**
```powershell
sqlcmd -S . -E -C -b -i database/02_seed.sql
```

*(Lưu ý: Không chạy seed nhiều lần trên cùng một database).*

**Trường hợp muốn xóa sạch và khởi tạo lại database phát triển:**
```powershell
sqlcmd -S . -E -C -b -v ConfirmRecreate=YES -i database/01_schema.sql
sqlcmd -S . -E -C -b -i database/02_seed.sql
```

---

### 2.2. Môi trường Triển khai Production (Fresh Provisioning)

> [!WARNING]
> Tuyệt đối **KHÔNG** chạy dữ liệu seed `02_seed.sql` trên môi trường Production! Runner `Run_Production.sql` được thiết kế độc lập, không chứa tài khoản demo hoặc dữ liệu phát triển.

Runner `database/scripts/Run_Production.sql` thực hiện quy trình provisioning tự động gồm 10 bước có kiểm soát ngữ cảnh nghiêm ngặt:
- Chuyển rõ ràng sang database đích `USE [$(DatabaseName)]` và xác thực `DB_NAME()` trước mọi câu lệnh DDL.
- Dừng ngay lập tức nếu database đã tồn tại (từ chối fresh provisioning trên database hiện hữu để bảo vệ dữ liệu).
- Tự động kiểm tra tính toàn vẹn ở bước cuối (`10_Verify_Database.sql`): xác minh 18 bảng, 7 triggers, 2 views, 21 user indexes (`is_ms_shipped = 0`), và 0 tài khoản demo.

**Yêu cầu thực thi:**
1. Chuyển thư mục làm việc vào `database/scripts` (bắt buộc vì SQLCMD `:r` sử dụng đường dẫn tương đối):
   ```bash
   cd database/scripts
   ```
2. Thực thi lệnh với biến `DatabaseName` và cờ `-b` (fail-fast exit code):

   **Windows (Windows Authentication):**
   ```powershell
   sqlcmd -S . -E -C -b -v DatabaseName=SupplierSelectionDSS -i Run_Production.sql
   ```

   **Linux / Docker / CI (Tài khoản sa):**
   ```bash
   sqlcmd -S localhost,1433 -U sa -P 'YourStrongPassword' -C -b -v DatabaseName=SupplierSelectionDSS -i Run_Production.sql
   ```

3. **Kiểm thử tự động cho Production Runner:**
   Script `tools/verify/test_production_runner.py` kiểm chứng toàn diện 6 kịch bản bắt buộc trên SQL Server tạm:
   - **Kịch bản a:** Khởi tạo sạch tạo đúng 18 bảng, 7 triggers, 2 views, 21 indexes, recovery model `FULL`.
   - **Kịch bản b:** Không có bất kỳ đối tượng nghiệp vụ nào bị tạo nhầm trong `master`, `model`, hoặc `msdb`.
   - **Kịch bản c:** Chạy lại trên database đã tồn tại bị từ chối với exit code khác 0; cấu hình và dữ liệu DB không đổi.
   - **Kịch bản d:** Lỗi tại một module dừng ngay lập tức, không bao giờ in thông báo thành công.
   - **Kịch bản e:** Database mới tạo không chứa tài khoản demo (`Users` count = 0).
   - **Kịch bản f:** Nếu provisioning gặp lỗi dở dang, database lỗi được giữ nguyên cho DBA kiểm tra, không tự ý `DROP DATABASE`.

   Chạy kiểm thử runner:
   ```bash
   python tools/verify/test_production_runner.py
   ```

---

## 3. Nâng Cấp Database Đang Chạy (Upgrade / Migration via Patches)

Khi nâng cấp ứng dụng trên database đã chứa dữ liệu nghiệp vụ, **tuyệt đối không chạy script tạo mới**. Hãy sao lưu trước và áp dụng tuần tự các bản vá trong `database/patches/`:

### Danh mục bản vá theo thứ tự thời gian:

| Thứ tự | Bản vá | Mục đích | Điều kiện tiên quyết & Cách kiểm tra |
|---|---|---|---|
| 1 | [20261022_add_rowversion.sql](patches/20261022_add_rowversion.sql) | Bổ sung cột `RowVersion` (`rowversion`/`timestamp`) vào `EvaluationBatches` phục vụ Optimistic Concurrency Control. | Bảng `EvaluationBatches` đã tồn tại. Tự kiểm tra qua `sys.columns`. |
| 2 | [20261022_round3_indexes_and_snapshots.sql](patches/20261022_round3_indexes_and_snapshots.sql) | Tạo bảng `BatchRoundSnapshots` và 4 chỉ mục tối ưu (`IX_EvaluationBatches_CreatedAt`, `IX_Reports_BatchID_GeneratedAt`, `IX_Approvals_BatchID_ApprovalDate`, `IX_AuditLogs_ActionTime`). | Đã áp dụng bản vá 1. Tự kiểm tra qua `sys.tables` và `sys.indexes`. |
| 3 | [20261024_enable_read_committed_snapshot.sql](patches/20261024_enable_read_committed_snapshot.sql) | Bật Read Committed Snapshot Isolation (RCSI) với tùy chọn `WITH NO_WAIT` để triệt tiêu tình trạng reader-writer blocking gây timeout 60s trên SQL Server. | Database `SupplierSelectionDSS` đã tồn tại. Tự kiểm tra qua `sys.databases.is_read_committed_snapshot_on`. Có kèm script hoàn tác riêng biệt `20261024_enable_read_committed_snapshot_rollback.sql`. |

**Lệnh áp dụng bản vá:**
```powershell
sqlcmd -S . -E -C -b -i database/patches/20261022_add_rowversion.sql
sqlcmd -S . -E -C -b -i database/patches/20261022_round3_indexes_and_snapshots.sql
sqlcmd -S . -E -C -b -i database/patches/20261024_enable_read_committed_snapshot.sql
```

*(Kèm theo script hoàn tác an toàn: `database/patches/20261024_enable_read_committed_snapshot_rollback.sql`)*

Các bản vá đều có tính chất **idempotent** (an toàn khi chạy lại nhiều lần, không làm mất bản ghi hiện có).

---

## 4. Quy Trình Khởi Tạo Quản Trị Viên Production (Admin Bootstrap)

Để khởi tạo tài khoản quản trị viên đầu tiên trên Production mà không để lộ mật khẩu trong git:

1. Tạo hash mật khẩu an toàn bằng tiện ích BCrypt (cost factor $\ge 11$).
2. Thực thi lệnh INSERT có tham số hóa trực tiếp trên SQL Server Production:

```sql
USE SupplierSelectionDSS;
GO

IF NOT EXISTS (SELECT 1 FROM Users WHERE Role = 'Administrator' AND Status = 'Active')
BEGIN
    INSERT INTO Users (Username, PasswordHash, FullName, Email, Role, Status)
    VALUES (
        N'sysadmin',
        N'<PROD_BCRYPT_HASH_HERE>',
        N'Production Administrator',
        N'admin@yourdomain.com',
        N'Administrator',
        N'Active'
    );
    PRINT N'Tạo tài khoản quản trị viên Production thành công.';
END
ELSE
BEGIN
    PRINT N'Đã tồn tại tài khoản Administrator đang hoạt động. Bỏ qua.';
END
GO
```

---

## 5. Tài Khoản Demo Môi Trường Phát Triển (Dev Only)

Khai báo trong `database/02_seed.sql`:

| Username | Mật khẩu demo | Vai trò (Role) | Chức năng chính |
|---|---|---|---|
| `admin` | `Admin@123456` | `Administrator` | Quản trị người dùng, audit log, xem toàn hệ thống |
| `staff01` | `Staff@123456` | `PurchasingStaff` | Quản lý NCC, nhóm hàng, tiêu chí, tạo đợt đánh giá |
| `expert01` | `Expert@123456` | `Expert` | Chấm điểm ma trận, so sánh cặp AHP, chạy TOPSIS |
| `mgr01` | `Manager@123456` | `Manager` | Phê duyệt/từ chối kết quả, xuất báo cáo |

---

## 6. Kiểm Tra Ngữ Nghĩa Schema & Mutation Testing

### 6.1. Trình xác minh Ngữ nghĩa CSDL (Semantic Schema Verifier)
Chạy công cụ phân tích AST ngữ nghĩa tự động để đối chiếu tính tương đương giữa schema chuẩn `01_schema.sql` và các script module:

```bash
python tools/verify/verify_schema_sync.py
```
**Phạm vi kiểm tra thực tế:**
- **Đặc tính cột:** Tên cột, kiểu dữ liệu đầy đủ (bao gồm precision & scale ví dụ `DECIMAL(10,4)`), nullability (`NULL` / `NOT NULL`), thuộc tính `IDENTITY`, và giá trị `DEFAULT`.
- **Ràng buộc toàn vẹn:** Khóa chính (PK), khóa ngoại (FK kèm `ON DELETE`), ràng buộc duy nhất (UQ), và biểu thức kiểm tra (CK).
- **Chỉ mục, Trigger, View:** Định nghĩa chỉ mục (tên, thứ tự cột, thuộc tính duy nhất/lọc), mã nguồn trigger (chuẩn hóa khoảng trắng và khối batch), và câu lệnh view.
- **Tách bạch 4 báo cáo:** Báo cáo phân định rõ ràng 4 hạng mục: Safety Guards, Schema Semantic Equivalence, Seed Data Isolation, và File Hygiene.
- **Không hardcode:** Số lượng đối tượng được suy diễn động từ schema canonical `01_schema.sql`.

### 6.2. Kiểm thử Đột biến (Mutation Testing)
Bộ kiểm thử đột biến tự động xác thực khả năng phát hiện lỗi của trình xác minh:

```bash
python tools/verify/test_schema_sync_mutations.py
```
Bao gồm 8 kịch bản kiểm chứng:
1. Đổi kiểu dữ liệu `DECIMAL(10,4)` thành `DECIMAL(10,3)` -> Bị phát hiện.
2. Đổi nullability `NULL` thành `NOT NULL` -> Bị phát hiện.
3. Thay đổi biểu thức ràng buộc `CHECK` -> Bị phát hiện.
4. Thay đổi mệnh đề `ON DELETE` của `FOREIGN KEY` -> Bị phát hiện.
5. Thay đổi logic điều kiện nghiệp vụ trong `TRIGGER` -> Bị phát hiện.
6. Đổi thứ tự cột trong chỉ mục `INDEX` -> Bị phát hiện.
7. Xóa hoặc thêm cột trong bảng -> Bị phát hiện.
8. Chỉ đổi định dạng hoặc thêm comment -> Không gây báo động giả (Zero False Positives).
