namespace SupplierSelectionDSS.Services;

/// <summary>
/// Kết quả tính toán khoảng cách của một nhà cung cấp tới PIS (D+), NIS (D-)
/// và hệ số tương tự/độ gần gũi (Closeness Coefficient Ci).
/// </summary>
public class TOPSISSupplierDistance
{
    public double DistanceToPIS { get; init; }

    public double DistanceToNIS { get; init; }

    public double ClosenessCoefficient { get; init; }
}
