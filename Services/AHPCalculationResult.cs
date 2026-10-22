namespace SupplierSelectionDSS.Services;

/// <summary>
/// Kết quả tính toán vector trọng số AHP và các chỉ số kiểm tra tính nhất quán.
/// </summary>
public class AHPCalculationResult
{
    public double[] Weights { get; init; } = Array.Empty<double>();

    public double LambdaMax { get; init; }

    public double CIValue { get; init; }

    public double RIValue { get; init; }

    public double CRValue { get; init; }

    public bool IsConsistent { get; init; }

    public int N { get; init; }
}
