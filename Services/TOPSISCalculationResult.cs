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
}
