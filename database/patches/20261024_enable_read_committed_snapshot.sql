/* =====================================================================
   PATCH: 20261024_enable_read_committed_snapshot.sql
   Mục đích:
     Bật Read Committed Snapshot Isolation (RCSI) cho database
     [SupplierSelectionDSS] để triệt tiêu tình trạng độc quyền đọc-ghi
     (Reader-Writer Blocking) trên SQL Server cục bộ, ngăn ngừa lỗi
     SqlException: Execution Timeout Expired (60s).

   Tính chất & Nguyên tắc An toàn:
     - Idempotent: Kiểm tra is_read_committed_snapshot_on trước khi thực hiện.
     - Fail-Fast (WITH NO_WAIT): Thất bại rõ ràng ngay lập tức nếu có kết
       nối cản trở việc lấy catalog lock, không treo vô hạn.
     - KHÔNG dùng ROLLBACK IMMEDIATE hoặc KILL bừa bãi khi thất bại;
       thay vào đó báo cáo chi tiết các session đang kết nối.
     - Phải thực thi ngoài transaction (ALTER DATABASE không được nằm
       trong explicit transaction).
     - Xác minh lại qua sys.databases sau khi chạy.
   ===================================================================== */

USE master;
GO

IF DB_ID(N'SupplierSelectionDSS') IS NULL
BEGIN
    RAISERROR(N'Database [SupplierSelectionDSS] không tồn tại trên instance này. Bản vá bị hủy.', 16, 1);
    RETURN;
END
GO

IF EXISTS (
    SELECT 1 
    FROM sys.databases 
    WHERE name = N'SupplierSelectionDSS' 
      AND is_read_committed_snapshot_on = 1
)
BEGIN
    PRINT N'[THÔNG TIN] Cơ sở dữ liệu [SupplierSelectionDSS] ĐÃ ĐƯỢC BẬT READ_COMMITTED_SNAPSHOT. Không cần thay đổi.';
END
ELSE
BEGIN
    PRINT N'[BẮT ĐẦU] Đang bật READ_COMMITTED_SNAPSHOT cho [SupplierSelectionDSS] với tùy chọn WITH NO_WAIT...';
    
    BEGIN TRY
        ALTER DATABASE [SupplierSelectionDSS] 
        SET READ_COMMITTED_SNAPSHOT ON 
        WITH NO_WAIT;
        
        PRINT N'[THÀNH CÔNG] Đã bật READ_COMMITTED_SNAPSHOT thành công.';
    END TRY
    BEGIN CATCH
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrNum INT = ERROR_NUMBER();
        
        PRINT N'';
        PRINT N'=============================================================================';
        PRINT N'[CẢNH BÁO KHÓA DANH MỤC] Không thể lấy khóa độc quyền trên database để bật RCSI ngay lập tức.';
        PRINT N'Mã lỗi: ' + CAST(@ErrNum AS NVARCHAR(10)) + N' - ' + @ErrMsg;
        PRINT N'Quy tắc an toàn: Không tự động KILL session hay ngắt kết nối cưỡng bức.';
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
        
        RAISERROR(N'Lệnh ALTER DATABASE SET READ_COMMITTED_SNAPSHOT ON bị chặn bởi kết nối hiện hữu. Vui lòng đóng các ứng dụng đang kết nối rồi chạy lại bản vá.', 16, 1);
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
