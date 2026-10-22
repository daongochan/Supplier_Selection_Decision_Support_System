using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Domain.Enums;

namespace SupplierSelectionDSS.Domain.Rules;

/// <summary>
/// Quản lý tập trung toàn bộ các quy tắc nghiệp vụ (Business Rules)
/// về trạng thái và chuyển đổi trạng thái của Đợt đánh giá (Evaluation Batch).
/// </summary>
public static class EvaluationBatchStateRules
{
    public const int MinimumSuppliersCount = 2;
    public const int MinimumCriteriaCount = 2;

    /// <summary>
    /// Kiểm tra trạng thái chuỗi có tương ứng với trạng thái mục tiêu hay không.
    /// </summary>
    public static bool IsStatus(string? currentStatus, string targetStatus)
    {
        return string.Equals(currentStatus?.Trim(), targetStatus, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kiểm tra đợt đánh giá có thể chuyển từ Draft sang Evaluating hay không.
    /// </summary>
    public static bool CanStart(string? currentStatus, int supplierCount, int criteriaCount)
    {
        if (!IsStatus(currentStatus, BatchStatus.Draft))
        {
            return false;
        }

        return supplierCount >= MinimumSuppliersCount && criteriaCount >= MinimumCriteriaCount;
    }

    /// <summary>
    /// Đảm bảo đợt đánh giá có thể bắt đầu, ném ngoại lệ nếu vi phạm.
    /// </summary>
    public static void EnsureCanStart(string? currentStatus, int supplierCount, int criteriaCount, string? batchCode = null)
    {
        if (!IsStatus(currentStatus, BatchStatus.Draft))
        {
            throw new InvalidOperationException(
                $"Không thể bắt đầu Batch {(batchCode != null ? $"'{batchCode}' " : "")}vì trạng thái hiện tại là {currentStatus}. Chỉ đợt đánh giá ở trạng thái '{BatchStatus.Draft}' mới có thể bắt đầu.");
        }

        if (supplierCount < MinimumSuppliersCount)
        {
            throw new InvalidOperationException(
                $"Batch phải có ít nhất {MinimumSuppliersCount} nhà cung cấp trước khi bắt đầu đánh giá.");
        }

        if (criteriaCount < MinimumCriteriaCount)
        {
            throw new InvalidOperationException(
                $"Batch phải có ít nhất {MinimumCriteriaCount} tiêu chí trước khi bắt đầu đánh giá.");
        }
    }

    /// <summary>
    /// Kiểm tra đợt đánh giá có đang ở trạng thái cho phép nhập điểm (Evaluating) hay không.
    /// </summary>
    public static bool CanEvaluate(string? currentStatus)
    {
        return IsStatus(currentStatus, BatchStatus.Evaluating);
    }

    /// <summary>
    /// Đảm bảo đợt đánh giá cho phép nhập điểm.
    /// </summary>
    public static void EnsureCanEvaluate(string? currentStatus)
    {
        if (!CanEvaluate(currentStatus))
        {
            throw new InvalidOperationException(
                $"Đợt đánh giá hiện ở trạng thái '{currentStatus}'. Chỉ đợt đánh giá ở trạng thái '{BatchStatus.Evaluating}' mới được phép nhập/chỉnh sửa điểm đánh giá.");
        }
    }

    /// <summary>
    /// Kiểm tra đợt đánh giá có thể thực hiện AHP hay không.
    /// </summary>
    public static bool CanPerformAHP(string? currentStatus, int criteriaCount)
    {
        return IsStatus(currentStatus, BatchStatus.Evaluating) && criteriaCount >= MinimumCriteriaCount;
    }

    /// <summary>
    /// Đảm bảo đợt đánh giá có thể thực hiện AHP.
    /// </summary>
    public static void EnsureCanPerformAHP(string? currentStatus, int criteriaCount)
    {
        if (!IsStatus(currentStatus, BatchStatus.Evaluating))
        {
            throw new InvalidOperationException(
                $"Chỉ được phép thực hiện AHP khi Batch đang ở trạng thái '{BatchStatus.Evaluating}'. Trạng thái hiện tại: '{currentStatus}'.");
        }

        if (criteriaCount < MinimumCriteriaCount)
        {
            throw new InvalidOperationException(
                $"AHP yêu cầu ít nhất {MinimumCriteriaCount} tiêu chí để so sánh cặp.");
        }
    }

    /// <summary>
    /// Kiểm tra đợt đánh giá có thể tính toán TOPSIS hay không.
    /// Điều kiện tiên quyết: Batch đang Evaluating, AHP đã có kết quả nhất quán, đầy đủ điểm.
    /// </summary>
    public static bool CanPerformTOPSIS(
        string? currentStatus,
        bool hasAhpResult,
        bool isAhpConsistent,
        bool hasCompleteScores)
    {
        return IsStatus(currentStatus, BatchStatus.Evaluating)
            && hasAhpResult
            && isAhpConsistent
            && hasCompleteScores;
    }

    /// <summary>
    /// Đảm bảo đợt đánh giá có thể tính toán TOPSIS.
    /// </summary>
    public static void EnsureCanPerformTOPSIS(
        string? currentStatus,
        bool hasAhpResult,
        bool isAhpConsistent,
        double? crValue = null)
    {
        if (!IsStatus(currentStatus, BatchStatus.Evaluating))
        {
            throw new InvalidOperationException(
                $"Chỉ được tính TOPSIS khi Batch đang ở trạng thái '{BatchStatus.Evaluating}'. Trạng thái hiện tại: '{currentStatus}'.");
        }

        if (!hasAhpResult)
        {
            throw new InvalidOperationException(
                "Chưa có kết quả AHP. Hãy thực hiện tính toán ma trận AHP trước khi chạy TOPSIS.");
        }

        if (!isAhpConsistent)
        {
            var crText = crValue.HasValue ? $" (CR = {crValue.Value:0.0000})" : "";
            throw new InvalidOperationException(
                $"AHP chưa đạt tính nhất quán{crText}. Hãy điều chỉnh ma trận so sánh AHP đạt CR <= 0.10 trước khi chạy TOPSIS.");
        }
    }

    /// <summary>
    /// Kiểm tra đợt đánh giá có thể phê duyệt quyết định (Approval) hay không.
    /// </summary>
    public static bool CanApprove(string? currentStatus, bool hasTopsisRanking)
    {
        return IsStatus(currentStatus, BatchStatus.Evaluating) && hasTopsisRanking;
    }

    /// <summary>
    /// Đảm bảo đợt đánh giá có thể xử lý quyết định phê duyệt.
    /// </summary>
    public static void EnsureCanApprove(string? currentStatus, bool hasTopsisRanking)
    {
        if (IsStatus(currentStatus, BatchStatus.Completed))
        {
            throw new InvalidOperationException(
                "Batch đã hoàn tất (Completed) và đã có quyết định phê duyệt cuối cùng.");
        }

        if (IsStatus(currentStatus, BatchStatus.Cancelled))
        {
            throw new InvalidOperationException(
                "Batch đã bị huỷ (Cancelled) và không thể xử lý quyết định.");
        }

        if (!IsStatus(currentStatus, BatchStatus.Evaluating))
        {
            throw new InvalidOperationException(
                $"Batch chưa ở trạng thái đánh giá ('{BatchStatus.Evaluating}'). Hiện tại: '{currentStatus}'.");
        }

        if (!hasTopsisRanking)
        {
            throw new InvalidOperationException(
                "Chưa có kết quả xếp hạng TOPSIS. Vui lòng hoàn thành bước TOPSIS trước khi phê duyệt.");
        }
    }

    /// <summary>
    /// Kiểm tra đợt đánh giá có thể bị hủy (Cancel) hay không.
    /// Chỉ cho phép hủy khi đang ở Draft hoặc Evaluating. Không cho phép hủy khi đã Completed hoặc Cancelled.
    /// </summary>
    public static bool CanCancel(string? currentStatus)
    {
        return IsStatus(currentStatus, BatchStatus.Draft) || IsStatus(currentStatus, BatchStatus.Evaluating);
    }

    /// <summary>
    /// Đảm bảo đợt đánh giá có thể bị hủy.
    /// </summary>
    public static void EnsureCanCancel(string? currentStatus)
    {
        if (IsStatus(currentStatus, BatchStatus.Completed))
        {
            throw new InvalidOperationException(
                "Đợt đánh giá đã hoàn tất (Completed) và không thể hủy bỏ.");
        }

        if (IsStatus(currentStatus, BatchStatus.Cancelled))
        {
            throw new InvalidOperationException(
                "Đợt đánh giá này đã ở trạng thái hủy (Cancelled).");
        }

        if (!CanCancel(currentStatus))
        {
            throw new InvalidOperationException(
                $"Không thể hủy đợt đánh giá ở trạng thái '{currentStatus}'.");
        }
    }

    /// <summary>
    /// Kiểm tra đợt đánh giá có thể yêu cầu đánh giá lại (Re-evaluate) hay không.
    /// </summary>
    public static bool CanReevaluate(string? currentStatus)
    {
        return IsStatus(currentStatus, BatchStatus.Evaluating);
    }

    /// <summary>
    /// Đảm bảo đợt đánh giá có thể yêu cầu đánh giá lại.
    /// </summary>
    public static void EnsureCanReevaluate(string? currentStatus)
    {
        if (IsStatus(currentStatus, BatchStatus.Completed))
        {
            throw new InvalidOperationException(
                "Batch đã hoàn tất và không thể yêu cầu đánh giá lại.");
        }

        if (IsStatus(currentStatus, BatchStatus.Cancelled))
        {
            throw new InvalidOperationException(
                "Batch đã bị huỷ và không thể yêu cầu đánh giá lại.");
        }

        if (!IsStatus(currentStatus, BatchStatus.Evaluating))
        {
            throw new InvalidOperationException(
                $"Batch chưa ở trạng thái Evaluating. Hiện tại: '{currentStatus}'.");
        }
    }

    /// <summary>
    /// Kiểm tra đợt đánh giá đã đóng (không thể thay đổi dữ liệu) hay chưa.
    /// </summary>
    public static bool IsLocked(string? currentStatus)
    {
        return IsStatus(currentStatus, BatchStatus.Completed) || IsStatus(currentStatus, BatchStatus.Cancelled);
    }
}
