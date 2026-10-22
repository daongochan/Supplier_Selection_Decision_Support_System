/* =====================================================================
   SUPPLIER SELECTION DSS - 02_SEED.SQL
   Dữ liệu khởi tạo mẫu cho hệ thống hỗ trợ ra quyết định chọn NCC
   Mật khẩu mặc định:
     - admin    : Admin@123456
     - staff01  : Staff@123456
     - expert01 : Expert@123456
     - mgr01    : Manager@123456
   ===================================================================== */

USE SupplierSelectionDSS;
GO

-- 1. Người dùng hệ thống (4 vai trò)
INSERT INTO Users (Username, PasswordHash, FullName, Email, Role, Status) VALUES
(N'admin',    N'$2a$11$En7MyyM43NQeLY/ogUTCvOgkL4hJR5ciZg0DfqjByIr4U.VcJaT/W', N'Quản trị viên hệ thống', N'admin@abc-mfg.vn',    N'Administrator',   N'Active'),
(N'staff01',  N'$2a$11$JtIGIwsVpQ1XoErsG69KOOzfvE3meTIljr7sJLXehVt/6FEYHODQy', N'Nguyễn Văn A',          N'staff01@abc-mfg.vn', N'PurchasingStaff', N'Active'),
(N'expert01', N'$2a$11$IrAtT7J657TQ.CsjIm0veewx3sxGs6ld6/aHOJBIXE2LFybXeop4e', N'Trần Thị B',            N'expert01@abc-mfg.vn',N'Expert',          N'Active'),
(N'mgr01',    N'$2a$11$5rLWhTUwvuoGChai9kH.luYozEPen.r5Dhb9HZJ8JQ7nl7EMXjahe', N'Lê Văn C',              N'mgr01@abc-mfg.vn',   N'Manager',         N'Active');
GO

-- 2. Nhóm hàng hoá / dịch vụ
INSERT INTO ProductGroups (GroupCode, GroupName, Description) VALUES
(N'PG01', N'Nguyên vật liệu điện dân dụng', N'Nhóm nguyên vật liệu và linh kiện phục vụ sản xuất thiết bị điện dân dụng');
GO

-- 3. Danh mục nhà cung cấp
INSERT INTO Suppliers (SupplierCode, SupplierName, Address, ContactPerson, Status) VALUES
(N'SUP01', N'Công ty Cổ phần Vật liệu An Phát',   N'Hà Nội',    N'Nguyễn Văn Hùng', N'Active'),
(N'SUP02', N'Công ty TNHH Kỹ thuật Hải Nam',     N'Hải Phòng', N'Lê Thị Mai',      N'Active'),
(N'SUP03', N'Tập đoàn Sản xuất & Thương mại Bắc Việt', N'Bắc Ninh', N'Trần Văn Đức', N'Active'),
(N'SUP04', N'Công ty TNHH Công nghệ & Vật liệu Đông Á', N'Hưng Yên', N'Phạm Thuỳ Linh', N'Active');
GO

-- 4. Liên kết Nhà cung cấp với Nhóm hàng hoá PG01
INSERT INTO SupplierGroups (SupplierID, GroupID)
SELECT s.SupplierID, g.GroupID
FROM Suppliers s
CROSS JOIN ProductGroups g
WHERE g.GroupCode = N'PG01';
GO

-- 5. Tiêu chí đánh giá mẫu (6 tiêu chí gồm Benefit & Cost, thang Điểm & giá trị thực)
INSERT INTO Criteria (CriteriaCode, CriteriaName, CriteriaType, Unit, Description) VALUES
(N'C01', N'Giá thành đơn vị',         N'Cost',    N'VND',  N'Chi phí mua hàng trên một đơn vị sản phẩm'),
(N'C02', N'Chất lượng sản phẩm',       N'Benefit', N'Điểm', N'Đánh giá chất lượng kỹ thuật, tỷ lệ lỗi và độ ổn định (thang 0-10)'),
(N'C03', N'Thời gian giao hàng',       N'Cost',    N'Ngày', N'Thời gian giao hàng trung bình từ khi đặt đơn'),
(N'C04', N'Năng lực cung ứng',         N'Benefit', N'Điểm', N'Khả năng đáp ứng sản lượng đơn hàng lớn khi có yêu cầu đột xuất (thang 0-10)'),
(N'C05', N'Uy tín thương hiệu',        N'Benefit', N'Điểm', N'Uy tín thương hiệu và lịch sử hợp tác trên thị trường (thang 0-10)'),
(N'C06', N'Dịch vụ hậu mãi & hỗ trợ',  N'Benefit', N'Điểm', N'Chất lượng chăm sóc khách hàng và chính sách bảo hành hỗ trợ (thang 0-10)');
GO

PRINT N'Khởi tạo dữ liệu mẫu (Seed Data) thành công.';
GO
