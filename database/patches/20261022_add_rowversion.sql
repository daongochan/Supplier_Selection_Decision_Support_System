/* =====================================================================
   PATCH: 20261022_add_rowversion.sql
   Mục đích: Bổ sung cột RowVersion (rowversion/timestamp) cho bảng EvaluationBatches
   để hỗ trợ Optimistic Concurrency Control (kiểm soát tương tranh lạc quan).
   Tính chất: Idempotent - an toàn khi chạy nhiều lần.
   ===================================================================== */

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.EvaluationBatches') 
      AND name = N'RowVersion'
)
BEGIN
    PRINT 'Đang bổ sung cột RowVersion vào bảng EvaluationBatches...';
    ALTER TABLE dbo.EvaluationBatches
    ADD RowVersion ROWVERSION NOT NULL;
    PRINT 'Đã bổ sung cột RowVersion thành công.';
END
ELSE
BEGIN
    PRINT 'Cột RowVersion đã tồn tại trên bảng EvaluationBatches. Bỏ qua.';
END
GO
