using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Domain.Enums;

/// <summary>
/// Trạng thái vòng đời của một đợt đánh giá (Evaluation Batch).
/// Khớp 1-1 với các giá trị chuỗi lưu trữ trong database (BatchStatus):
/// "Draft", "Evaluating", "Completed", "Cancelled".
/// </summary>
public enum EvaluationBatchStatus
{
    Draft = 1,
    Evaluating = 2,
    Completed = 3,
    Cancelled = 4
}

public static class EvaluationBatchStatusExtensions
{
    public static string ToStatusString(this EvaluationBatchStatus status) => status switch
    {
        EvaluationBatchStatus.Draft => BatchStatus.Draft,
        EvaluationBatchStatus.Evaluating => BatchStatus.Evaluating,
        EvaluationBatchStatus.Completed => BatchStatus.Completed,
        EvaluationBatchStatus.Cancelled => BatchStatus.Cancelled,
        _ => status.ToString()
    };

    public static EvaluationBatchStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "draft" => EvaluationBatchStatus.Draft,
            "evaluating" => EvaluationBatchStatus.Evaluating,
            "completed" => EvaluationBatchStatus.Completed,
            "cancelled" => EvaluationBatchStatus.Cancelled,
            _ => null
        };
    }
}
