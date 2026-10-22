namespace SupplierSelectionDSS.Models;

/// <summary>
/// Đại diện cho một hàng trong ma trận trung gian của TOPSIS (ma trận chuẩn hóa hoặc ma trận có trọng số).
/// </summary>
public class TOPSISMatrixRowViewModel
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị chuẩn hóa hoặc có trọng số theo CriteriaId.
    /// </summary>
    public Dictionary<int, decimal> Values { get; set; } = new();
}
