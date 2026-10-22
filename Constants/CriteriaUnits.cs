namespace SupplierSelectionDSS.Constants;

/// <summary>
/// Các hằng số đơn vị và định dạng thang đo của tiêu chí đánh giá.
/// </summary>
public static class CriteriaUnits
{
    /// <summary>
    /// Đơn vị thang đo định tính chuẩn từ 0 đến 10.
    /// </summary>
    public const string Rating = "Điểm";

    /// <summary>
    /// Kiểm tra xem tiêu chí có phải là loại thang điểm định tính chuẩn (0 - 10) hay không.
    /// </summary>
    public static bool IsRatingScale(string? unit)
    {
        return string.Equals(unit?.Trim(), Rating, StringComparison.OrdinalIgnoreCase);
    }
}
