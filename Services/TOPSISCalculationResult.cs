namespace SupplierSelectionDSS.Services;

/// <summary>
/// Kết quả tính toán thuật toán TOPSIS bao gồm các ma trận trung gian,
/// giải pháp lý tưởng PIS/NIS, khoảng cách và thứ hạng nhà cung cấp.
/// </summary>
public class TOPSISCalculationResult
{
    public double[,] NormalizedMatrix { get; init; } = new double[0, 0];

    public double[,] WeightedMatrix { get; init; } = new double[0, 0];

    public double[] PISValues { get; init; } = Array.Empty<double>();

    public double[] NISValues { get; init; } = Array.Empty<double>();

    public TOPSISSupplierDistance[] Distances { get; init; } = Array.Empty<TOPSISSupplierDistance>();

    public int[] RankByIndex { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Danh sách chỉ số (0-based) các tiêu chí có toàn bộ giá trị bằng 0 hoặc không phân biệt.
    /// </summary>
    public IReadOnlyList<int> ZeroVarianceCriteriaIndices { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Đánh dấu nếu có từ 2 nhà cung cấp trở lên có thứ hạng hòa nhau (Standard Competition Ranking).
    /// </summary>
    public bool HasTies { get; init; }

    /// <summary>
    /// Đánh dấu khi tất cả nhà cung cấp có cùng điểm trên mọi tiêu chí (không phân biệt được).
    /// </summary>
    public bool IsIndistinguishable { get; init; }
}
