/* =====================================================================
   ROLLBACK PATCH: 20261024_enable_read_committed_snapshot_rollback.sql
   Mục đích:
     Hoàn tác cài đặt Read Committed Snapshot Isolation (RCSI) trên
     database [SupplierSelectionDSS], đưa về trạng thái Read Committed khóa dòng
     truyền thống (mặc định ban đầu).

   Tính chất & Nguyên tắc An toàn:
     - Idempotent: Kiểm tra is_read_committed_snapshot_on trước khi thực hiện.
     - Fail-Fast (WITH NO_WAIT): Thất bại rõ ràng ngay lập tức nếu có kết
       nối cản trở việc lấy catalog lock, không treo vô hạn.
     - KHÔNG tự động chạy bản vá này trừ khi có yêu cầu hoàn tác rõ ràng.
   ===================================================================== */

USE master;
GO

IF DB_ID(N'SupplierSelectionDSS') IS NULL
BEGIN
    RAISERROR(N'Database [SupplierSelectionDSS] không tồn tại trên instance này. Bản vá hoàn tác bị hủy.', 16, 1);
    RETURN;
END
GO

IF EXISTS (
    SELECT 1 
    FROM sys.databases 
    WHERE name = N'SupplierSelectionDSS' 
      AND is_read_committed_snapshot_on = 0
)
BEGIN
    PRINT N'[THÔNG TIN] Cơ sở dữ liệu [SupplierSelectionDSS] ĐÃ Ở TRẠNG THÁI READ_COMMITTED_SNAPSHOT OFF. Không cần hoàn tác.';
END
ELSE
BEGIN
    PRINT N'[BẮT ĐẦU HOÀN TÁC] Đang tắt READ_COMMITTED_SNAPSHOT cho [SupplierSelectionDSS] với tùy chọn WITH NO_WAIT...';
    
    BEGIN TRY
        ALTER DATABASE [SupplierSelectionDSS] 
        SET READ_COMMITTED_SNAPSHOT OFF 
        WITH NO_WAIT;
        
        PRINT N'[THÀNH CÔNG] Đã hoàn tác READ_COMMITTED_SNAPSHOT về OFF thành công.';
    END TRY
    BEGIN CATCH
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrNum INT = ERROR_NUMBER();
        
        PRINT N'';
        PRINT N'=============================================================================';
        PRINT N'[CẢNH BÁO KHÓA DANH MỤC] Không thể lấy khóa độc quyền trên database để tắt RCSI ngay lập tức.';
        PRINT N'Mã lỗi: ' + CAST(@ErrNum AS NVARCHAR(10)) + N' - ' + @ErrMsg;
        PRINT N'Danh sách các session hiện đang kết nối tới [SupplierSelectionDSS]:';
        PRINT N'=============================================================================';
        
        SELECT 
            s.session_id,
            s.login_name,
            s.host_name,
            s.program_name,
            s.status,
            s.last_request_start_time,
            s.last_request_end_time
        FROM sys.dm_exec_sessions s
        WHERE s.database_id = DB_ID(N'SupplierSelectionDSS');
        
        RAISERROR(N'Lệnh ALTER DATABASE SET READ_COMMITTED_SNAPSHOT OFF bị chặn bởi kết nối hiện hữu. Vui lòng đóng các ứng dụng đang kết nối rồi chạy lại hoàn tác.', 16, 1);
    END CATCH
END
GO

-- Xác minh trạng thái cuối cùng
SELECT 
    name AS DatabaseName,
    is_read_committed_snapshot_on AS IsRCSIOn,
    snapshot_isolation_state_desc AS SnapshotIsolationState,
    recovery_model_desc AS RecoveryModel
FROM sys.databases
WHERE name = N'SupplierSelectionDSS';
GO
