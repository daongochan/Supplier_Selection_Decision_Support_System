# Hướng Dẫn Cài Đặt Cơ Sở Dữ Liệu - Supplier Selection DSS

Cơ sở dữ liệu của hệ thống được quản lý theo phương thức Database-First với các đối tượng nâng cao:
- 17 Bảng dữ liệu quan hệ
- 7 Triggers toàn vẹn nghiệp vụ (`TR_*_Validate*`)
- 2 Views hỗ trợ báo cáo (`vw_BatchRanking`, `vw_BatchCriteriaWeights`)
- Collation: `Vietnamese_CI_AS` (hỗ trợ sắp xếp tiếng Việt chính xác)

---

## 1. Yêu cầu hệ thống
- SQL Server 2019 trở lên (hoặc SQL Server LocalDB, Azure SQL Edge, SQL Server Docker).
- SQL Server Management Studio (SSMS), Azure Data Studio, hoặc tiện ích `sqlcmd`.

---

## 2. Hướng dẫn khởi tạo

### Cách 1: Chạy bằng `sqlcmd` (Khuyến nghị cho dòng lệnh)

Mở PowerShell tại thư mục gốc dự án:

```powershell
# 1. Tạo Schema và các đối tượng (Tables, Constraints, Triggers, Views)
sqlcmd -S . -E -i database/01_schema.sql

# 2. Khởi tạo dữ liệu mẫu (Seed Data)
sqlcmd -S . -E -i database/02_seed.sql
```

*(Nếu dùng SQL Express hoặc instance khác, thay `.` bằng `.\SQLEXPRESS`)*.

### Cách 2: Chạy trong SQL Server Management Studio (SSMS)

1. Mở SSMS, kết nối tới SQL Server instance của bạn.
2. Mở file [database/01_schema.sql](file:///d:/DEAN/Projects/SupplierSelectionDSS/database/01_schema.sql) và nhấn **Execute (F5)**.
3. Mở file [database/02_seed.sql](file:///d:/DEAN/Projects/SupplierSelectionDSS/database/02_seed.sql) và nhấn **Execute (F5)**.

---

## 3. Tài khoản demo được khởi tạo

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
