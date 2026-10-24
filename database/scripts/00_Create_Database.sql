USE master;
GO

-- =====================================================================
-- SAFETY GUARD: Chống xóa nhầm cơ sở dữ liệu SupplierSelectionDSS
-- =====================================================================
IF DB_ID(N'SupplierSelectionDSS') IS NOT NULL
BEGIN
    DECLARE @ConfirmRecreate NVARCHAR(50) = N'$(ConfirmRecreate)';

    IF UPPER(LTRIM(RTRIM(@ConfirmRecreate))) <> N'YES'
    BEGIN
        RAISERROR(
N'=============================================================================
CẢNH BÁO AN TOÀN DỮ LIỆU (DATA LOSS PREVENTION):
Cơ sở dữ liệu [SupplierSelectionDSS] ĐÃ TỒN TẠI trên SQL Server!
Thực thi script này sẽ XÓA SẠCH toàn bộ bảng và dữ liệu hiện có.

Để tiếp tục xóa và tạo mới hoàn toàn, bạn BẮT BUỘC phải truyền biến xác nhận:
  - Qua sqlcmd : sqlcmd -S . -E -C -v ConfirmRecreate=YES -i <script.sql>
  - Trong SSMS : Bật "SQLCMD Mode" (Query -> SQLCMD Mode) và truyền ConfirmRecreate=YES

Quá trình khởi tạo đã dừng lại để bảo vệ an toàn dữ liệu của bạn.
=============================================================================', 16, 1);
        SET NOEXEC ON;
    END
    ELSE
    BEGIN
        PRINT N'Xác nhận ConfirmRecreate=YES hợp lệ. Đang tiến hành xóa và khởi tạo lại [SupplierSelectionDSS]...';
        ALTER DATABASE SupplierSelectionDSS SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE SupplierSelectionDSS;
    END
END
GO

CREATE DATABASE SupplierSelectionDSS
COLLATE Vietnamese_CI_AS;
GO

ALTER DATABASE SupplierSelectionDSS SET RECOVERY SIMPLE;
GO

ALTER DATABASE SupplierSelectionDSS SET READ_COMMITTED_SNAPSHOT ON;
GO

USE SupplierSelectionDSS;
GO
