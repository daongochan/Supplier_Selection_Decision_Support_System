using System.Threading;
using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Exceptions;

namespace SupplierSelectionDSS.Services;

public static class BatchConcurrencyHelper
{
    private static long _inMemoryVersionCounter = 1;

    public const string ConcurrencyConflictMessage =
        "Dữ liệu đợt đánh giá vừa được người khác thay đổi (Optimistic Concurrency). Vui lòng tải lại trang và kiểm tra lại trước khi thực hiện.";

    public static string ToBase64(byte[]? rowVersion)
    {
        if (rowVersion == null || rowVersion.Length == 0)
        {
            return string.Empty;
        }

        return Convert.ToBase64String(rowVersion);
    }

    public static byte[]? TryFromBase64(string? rowVersionBase64)
    {
        if (string.IsNullOrWhiteSpace(rowVersionBase64))
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(rowVersionBase64.Trim());
        }
        catch (FormatException)
        {
            throw new BusinessRuleException("Phiên bản dữ liệu (RowVersion) không hợp lệ.");
        }
    }

    public static void EnsureInitializedForInMemory(EvaluationBatch batch)
    {
        if (batch.RowVersion == null || batch.RowVersion.Length == 0)
        {
            batch.RowVersion = NextInMemoryVersionBytes();
        }
    }

    public static void VerifyExpectedRowVersion(
        EvaluationBatch batch,
        string? expectedRowVersionBase64)
    {
        var expectedBytes = TryFromBase64(expectedRowVersionBase64);
        if (expectedBytes == null)
        {
            return;
        }

        var currentBytes = batch.RowVersion ?? Array.Empty<byte>();
        if (!currentBytes.AsSpan().SequenceEqual(expectedBytes))
        {
            throw new BusinessRuleException(ConcurrencyConflictMessage);
        }
    }

    public static async Task BumpAndVerifyBatchVersionAsync(
        ApplicationDbContext context,
        EvaluationBatch batch,
        string? expectedRowVersionBase64 = null,
        string? newStatus = null,
        DateTime? completedAt = null,
        DateTime? cancelledAt = null,
        bool clearCompletedAt = false)
    {
        var expectedBytes = TryFromBase64(expectedRowVersionBase64);
        var currentBytes = batch.RowVersion ?? Array.Empty<byte>();

        if (expectedBytes != null && currentBytes.Length > 0 && !currentBytes.AsSpan().SequenceEqual(expectedBytes))
        {
            throw new BusinessRuleException(ConcurrencyConflictMessage);
        }

        var tokenToMatch = expectedBytes ?? currentBytes;
        var targetStatus = newStatus ?? batch.Status;

        if (context.Database.IsRelational())
        {
            int rowsAffected;
            if (completedAt.HasValue)
            {
                rowsAffected = await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE EvaluationBatches SET Status = {targetStatus}, CompletedAt = {completedAt.Value} WHERE BatchID = {batch.BatchId} AND RowVersion = {tokenToMatch} AND Status NOT IN ({BatchStatus.Completed}, {BatchStatus.Cancelled})");
            }
            else if (cancelledAt.HasValue)
            {
                rowsAffected = await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE EvaluationBatches SET Status = {targetStatus}, CancelledAt = {cancelledAt.Value}, Description = {batch.Description} WHERE BatchID = {batch.BatchId} AND RowVersion = {tokenToMatch} AND Status NOT IN ({BatchStatus.Completed}, {BatchStatus.Cancelled})");
            }
            else if (clearCompletedAt)
            {
                rowsAffected = await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE EvaluationBatches SET Status = {targetStatus}, CompletedAt = NULL WHERE BatchID = {batch.BatchId} AND RowVersion = {tokenToMatch} AND Status NOT IN ({BatchStatus.Completed}, {BatchStatus.Cancelled})");
            }
            else
            {
                rowsAffected = await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE EvaluationBatches SET Status = {targetStatus}, StartDate = {batch.StartDate} WHERE BatchID = {batch.BatchId} AND RowVersion = {tokenToMatch} AND Status NOT IN ({BatchStatus.Completed}, {BatchStatus.Cancelled})");
            }

            if (rowsAffected == 0)
            {
                throw new BusinessRuleException(ConcurrencyConflictMessage);
            }

            var entry = context.Entry(batch);
            if (entry.State != EntityState.Detached)
            {
                await entry.ReloadAsync();
            }
        }
        else
        {
            if (newStatus != null)
            {
                batch.Status = newStatus;
            }

            if (completedAt.HasValue)
            {
                batch.CompletedAt = completedAt.Value;
            }
            else if (clearCompletedAt)
            {
                batch.CompletedAt = null;
            }

            if (cancelledAt.HasValue)
            {
                batch.CancelledAt = cancelledAt.Value;
            }

            batch.RowVersion = NextInMemoryVersionBytes();
        }
    }

    private static byte[] NextInMemoryVersionBytes()
    {
        var next = Interlocked.Increment(ref _inMemoryVersionCounter);
        var bytes = BitConverter.GetBytes(next);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return bytes;
    }
}
