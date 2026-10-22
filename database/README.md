# Hướng Dẫn Cài Đặt Cơ Sở Dữ Liệu - Supplier Selection DSS

> [!CAUTION]
> **CẢNH BÁO AN TOÀN DỮ LIỆU (DATA LOSS PREVENTION):**
> Các script `01_schema.sql`, `scripts/00_Create_Database.sql`, và `scripts/Run_All.sql` được trang bị **chốt an toàn (Safety Guard)**. Mặc định, nếu cơ sở dữ liệu `SupplierSelectionDSS` **đã tồn tại**, script sẽ **LẬP TỨC DỪNG LẠI** và không thực hiện bất kỳ thay đổi nào để tránh mất mát dữ liệu của người dùng.
>
> Nếu bạn thực sự muốn xóa sạch và khởi tạo lại database từ đầu, bạn phải chủ động cung cấp cờ xác nhận `ConfirmRecreate=YES`.

> [!IMPORTANT]
> **HƯỚNG DẪN SAO LƯU TRƯỚC KHI THAO TÁC (BACKUP DATABASE):**
> Trước khi thực thi bất kỳ lệnh khởi tạo lại hoặc chạy script vá cấu trúc, hãy luôn tạo bản sao lưu dữ liệu hiện tại bằng lệnh:
> ```sql
> BACKUP DATABASE SupplierSelectionDSS
> TO DISK = N'C:\Backups\SupplierSelectionDSS_BeforeReset.bak'
> WITH FORMAT, INIT, NAME = N'SupplierSelectionDSS Full Backup';
> ```

---

## 1. Yêu cầu hệ thống
- SQL Server 2019 trở lên (hoặc SQL Server LocalDB, Azure SQL Edge, SQL Server Docker).
- SQL Server Management Studio (SSMS), Azure Data Studio, hoặc tiện ích `sqlcmd`.
- Collation chuẩn: `Vietnamese_CI_AS` (hỗ trợ sắp xếp tiếng Việt chính xác).

---

## 2. Hướng dẫn khởi tạo

### Cách 1: Chạy bằng `sqlcmd` (Khuyến nghị cho dòng lệnh)

Mở PowerShell tại thư mục gốc dự án:

**Trường hợp 1: Tạo mới trên môi trường sạch (Database chưa tồn tại)**
```powershell
# 1. Tạo Schema và các đối tượng (17 Tables, Constraints, 7 Triggers, Indexes, 2 Views)
sqlcmd -S . -E -C -i database/01_schema.sql

# 2. Khởi tạo dữ liệu mẫu (Seed Data)
sqlcmd -S . -E -C -i database/02_seed.sql
```

**Trường hợp 2: Muốn xóa sạch database cũ và tạo lại từ đầu (Bắt buộc xác nhận)**
```powershell
# Truyền biến ConfirmRecreate=YES để xác nhận drop và tạo mới
sqlcmd -S . -E -C -v ConfirmRecreate=YES -i database/01_schema.sql
sqlcmd -S . -E -C -i database/02_seed.sql
```

*(Lưu ý: Thêm cờ `-C` nếu driver SQL Server yêu cầu TrustServerCertificate. Nếu dùng SQL Express hoặc instance khác, thay `.` bằng `.\SQLEXPRESS`)*.

### Cách 2: Chạy trong SQL Server Management Studio (SSMS)

1. Mở SSMS, kết nối tới SQL Server instance của bạn.
2. Nếu database chưa tồn tại: Mở file [01_schema.sql](01_schema.sql) và nhấn **Execute (F5)**.
3. Nếu database đã tồn tại và bạn muốn xóa tạo lại: Bật **SQLCMD Mode** (Menu `Query` -> chọn `SQLCMD Mode`), thêm dòng `:setvar ConfirmRecreate YES` ở đầu file rồi nhấn **Execute (F5)**.
4. Mở file [02_seed.sql](02_seed.sql) và nhấn **Execute (F5)** để nạp dữ liệu mẫu ban đầu.

---

## 3. Dữ liệu mẫu & Tài khoản demo

> [!WARNING]
> **CHỈ DÙNG CHO MÔI TRƯỜNG PHÁT TRIỂN & DEMO (DEV ONLY):**
> Tệp `database/02_seed.sql` chứa danh sách tài khoản demo có mật khẩu công khai dưới đây. Tuyệt đối **KHÔNG** sử dụng dữ liệu seed này trên môi trường Production!

| Username | Mật khẩu | Họ và tên | Vai trò (Role) | Chức năng chính |
| :--- | :--- | :--- | :--- | :--- |
| `admin` | `Admin@123456` | Quản trị viên hệ thống | `Administrator` | Quản trị người dùng, nhật ký audit, xem toàn bộ |
| `staff01` | `Staff@123456` | Nguyễn Văn A | `PurchasingStaff` | Quản lý NCC, nhóm hàng, tiêu chí, tạo đợt đánh giá |
| `expert01` | `Expert@123456` | Trần Thị B | `Expert` | Chấm điểm ma trận quyết định, so sánh cặp AHP, chạy TOPSIS |
| `mgr01` | `Manager@123456` | Lê Văn C | `Manager` | Phê duyệt kết quả chọn nhà cung cấp, xuất báo cáo |

> [!NOTE]
> Mật khẩu được băm an toàn bằng thuật toán **BCrypt** (cost factor = 11).

---

## 4. Cấu hình chuỗi kết nối trong ứng dụng

Để ứng dụng kết nối tới CSDL, cấu hình trong `appsettings.json` hoặc User Secrets:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SupplierSelectionDSS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Ghi đè bằng biến môi trường (Production / CI / Docker):
```bash
ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=SupplierSelectionDSS;User Id=sa;Password=YourStrongPassword;TrustServerCertificate=True"
```
