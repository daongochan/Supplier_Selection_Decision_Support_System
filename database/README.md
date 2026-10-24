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
> Tuyệt đối **KHÔNG** chạy dữ liệu seed `02_seed.sql` trên môi trường Production!

Đối với máy chủ Production mới hoàn toàn (chưa có dữ liệu):
1. Chuyển thư mục vào `database/scripts`:
   ```bash
   cd database/scripts
   ```
2. Thực thi runner SQLCMD có kiểm soát lỗi fail-fast (`:On Error exit`):
   ```bash
   sqlcmd -S <production-server> -U <admin-user> -P <admin-password> -C -b -i Run_Production.sql
   ```
3. Khởi tạo tài khoản quản trị viên Production đầu tiên theo quy trình bảo mật (xem mục 4).

---

## 3. Nâng Cấp Database Đang Chạy (Upgrade / Migration via Patches)

Khi nâng cấp ứng dụng trên database đã chứa dữ liệu nghiệp vụ, **tuyệt đối không chạy script tạo mới**. Hãy sao lưu trước và áp dụng tuần tự các bản vá trong `database/patches/`:

### Danh mục bản vá theo thứ tự thời gian:

| Thứ tự | Bản vá | Mục đích | Điều kiện tiên quyết & Cách kiểm tra |
|---|---|---|---|
| 1 | [20261022_add_rowversion.sql](patches/20261022_add_rowversion.sql) | Bổ sung cột `RowVersion` (`rowversion`/`timestamp`) vào `EvaluationBatches` phục vụ Optimistic Concurrency Control. | Bảng `EvaluationBatches` đã tồn tại. Tự kiểm tra qua `sys.columns`. |
| 2 | [20261022_round3_indexes_and_snapshots.sql](patches/20261022_round3_indexes_and_snapshots.sql) | Tạo bảng `BatchRoundSnapshots` và 4 chỉ mục tối ưu (`IX_EvaluationBatches_CreatedAt`, `IX_Reports_BatchID_GeneratedAt`, `IX_Approvals_BatchID_ApprovalDate`, `IX_AuditLogs_ActionTime`). | Đã áp dụng bản vá 1. Tự kiểm tra qua `sys.tables` và `sys.indexes`. |

**Lệnh áp dụng bản vá:**
```powershell
sqlcmd -S . -E -C -b -i database/patches/20261022_add_rowversion.sql
sqlcmd -S . -E -C -b -i database/patches/20261022_round3_indexes_and_snapshots.sql
```

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

## 6. Kiểm Tra Đồng Bộ Schema (Sync Verification)

Chạy công cụ kiểm tra tự động để đối chiếu tính toàn vẹn giữa schema tổng hợp và các module:

```bash
python tools/verify/verify_schema_sync.py
```
Công cụ xác minh:
- Đủ 18 bảng, 7 triggers, 2 views, 21 indexes.
- Cấu trúc từng cột giữa `01_schema.sql` và các file module khớp nhau.
- Safety Guard hợp lệ, không có dữ liệu seed trùng lặp hoặc hash placeholder.
