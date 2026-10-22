namespace SupplierSelectionDSS.Models;

/// <summary>
/// Kết quả phân tích độ nhạy (Sensitivity Analysis) của mô hình TOPSIS.
/// Đánh giá mức độ ổn định của quyết định lựa chọn nhà cung cấp khi trọng số các tiêu chí biến động ±10%, ±20%.
/// </summary>
public class SensitivityAnalysisResultViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BaseTopSupplierCode { get; set; } = string.Empty;

    public string BaseTopSupplierName { get; set; } = string.Empty;

    public decimal BaseTopSupplierCloseness { get; set; }

    /// <summary>
    /// True nếu nhà cung cấp đứng đầu không thay đổi trong toàn bộ các kịch bản biến động trọng số.
    /// </summary>
    public bool IsOverallRobust { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm các kịch bản giữ nguyên nhà cung cấp dẫn đầu (0 - 100%).
    /// </summary>
    public double RobustnessPercentage { get; set; }

    /// <summary>
    /// Thông điệp tóm tắt học thuật về độ vững bền của quyết định.
    /// </summary>
    public string SummaryMessage { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách kịch bản phân tích theo từng tiêu chí.
    /// </summary>
    public List<CriterionSensitivityScenarioViewModel> Scenarios { get; set; } = new();
}

public class CriterionSensitivityScenarioViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public decimal BaseWeight { get; set; }

    /// <summary>
    /// Kết quả chạy cho từng mức biến động trọng số (-20%, -10%, +10%, +20%).
    /// </summary>
    public List<SensitivityRunViewModel> Runs { get; set; } = new();
}

public class SensitivityRunViewModel
{
    /// <summary>
    /// Mức độ thay đổi trọng số tính theo % (ví dụ: -20, -10, 10, 20).
    /// </summary>
    public int DeltaPercent { get; set; }

    /// <summary>
    /// Trọng số mới của tiêu chí sau khi biến động.
    /// </summary>
    public decimal NewWeight { get; set; }

    /// <summary>
    /// Mã nhà cung cấp đứng đầu ở kịch bản này.
    /// </summary>
    public string TopSupplierCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên nhà cung cấp đứng đầu ở kịch bản này.
    /// </summary>
    public string TopSupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Điểm tương đồng Closeness Coefficient của nhà cung cấp đứng đầu.
    /// </summary>
    public decimal TopSupplierCloseness { get; set; }

    /// <summary>
    /// True nếu nhà cung cấp đứng đầu trùng khớp với nhà cung cấp đứng đầu ở kịch bản gốc.
    /// </summary>
    public bool IsStable { get; set; }
}
