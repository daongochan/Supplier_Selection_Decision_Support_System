/* =====================================================================
   9. DỮ LIỆU MẪU (SEED DATA)
   ===================================================================== */

INSERT INTO Users (Username, PasswordHash, FullName, Email, Role, Status) VALUES
(N'admin',    N'<hash>', N'Quản trị viên hệ thống', N'admin@abc-mfg.vn',    N'Administrator',   N'Active'),
(N'staff01',  N'<hash>', N'Nguyễn Văn A',          N'staff01@abc-mfg.vn', N'PurchasingStaff', N'Active'),
(N'expert01', N'<hash>', N'Trần Thị B',            N'expert01@abc-mfg.vn',N'Expert',          N'Active'),
(N'mgr01',    N'<hash>', N'Lê Văn C',              N'mgr01@abc-mfg.vn',   N'Manager',         N'Active');
GO

INSERT INTO ProductGroups (GroupCode, GroupName, Description) VALUES
(N'PG01', N'Nguyên vật liệu điện dân dụng', N'Nhóm nguyên vật liệu và linh kiện phục vụ sản xuất thiết bị điện dân dụng');
GO

INSERT INTO Suppliers (SupplierCode, SupplierName, Address, ContactPerson, Status) VALUES
(N'SUP01', N'Nhà cung cấp A', N'Hà Nội',   N'Người liên hệ A', N'Active'),
(N'SUP02', N'Nhà cung cấp B', N'Hải Phòng',N'Người liên hệ B', N'Active'),
(N'SUP03', N'Nhà cung cấp C', N'Bắc Ninh', N'Người liên hệ C', N'Active'),
(N'SUP04', N'Nhà cung cấp D', N'Hưng Yên', N'Người liên hệ D', N'Active');
GO

INSERT INTO Criteria (CriteriaCode, CriteriaName, CriteriaType, Unit) VALUES
(N'C01', N'Giá thành',           N'Cost',    N'VND'),
(N'C02', N'Chất lượng',          N'Benefit', N'Điểm'),
(N'C03', N'Thời gian giao hàng', N'Cost',    N'Ngày'),
(N'C04', N'Năng lực cung ứng',   N'Benefit', N'Điểm'),
(N'C05', N'Uy tín',              N'Benefit', N'Điểm'),
(N'C06', N'Dịch vụ hậu mãi',     N'Benefit', N'Điểm');
GO

/* =====================================================================
   KẾT THÚC SCRIPT
   ===================================================================== */
PRINT N'Cơ sở dữ liệu SupplierSelectionDSS đã được tạo thành công (Final Version).';
