namespace SupplierSelectionDSS.Models;

public class TOPSISViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BatchName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;


    // =========================================================
    // AHP READINESS
    // =========================================================

    public bool HasAHPResult { get; set; }

    public bool IsAHPConsistent { get; set; }

    public double AHPConsistencyRatio { get; set; }


    // =========================================================
    // CRITERIA
    // =========================================================

    public List<TOPSISCriteriaViewModel> Criteria { get; set; }
        = new();


    // =========================================================
    // SUPPLIER DECISION MATRIX
    // =========================================================

    public List<TOPSISEvaluationRowViewModel> Suppliers { get; set; }
        = new();


    // =========================================================
    // PIS / NIS
    // =========================================================

    public List<TOPSISIdealViewModel> IdealSolutions { get; set; }
        = new();


    // =========================================================
    // FINAL RANKING
    // =========================================================

    public List<TOPSISRankingViewModel> Ranking { get; set; }
        = new();

    public bool HasTOPSISResult =>
        Ranking.Any();

    // =========================================================
    // ACADEMIC FEATURES: INTERMEDIATE MATRICES & SENSITIVITY
    // =========================================================

    /// <summary>
    /// Ma trận chuẩn hóa vector R = [r_ij].
    /// </summary>
    public List<TOPSISMatrixRowViewModel> NormalizedMatrix { get; set; }
        = new();

    /// <summary>
    /// Ma trận chuẩn hóa có trọng số V = [v_ij].
    /// </summary>
    public List<TOPSISMatrixRowViewModel> WeightedMatrix { get; set; }
        = new();

    /// <summary>
    /// Kết quả phân tích độ nhạy (biến thiên trọng số +-10%, +-20%).
    /// </summary>
    public SensitivityAnalysisResultViewModel? SensitivityAnalysis { get; set; }

    /// <summary>
    /// Đánh dấu nếu có từ 2 nhà cung cấp trở lên hòa hạng.
    /// </summary>
    public bool HasTies { get; set; }

    /// <summary>
    /// Đánh dấu khi tất cả nhà cung cấp có cùng điểm trên mọi tiêu chí.
    /// </summary>
    public bool IsIndistinguishable { get; set; }

    /// <summary>
    /// Mã các tiêu chí có phương sai bằng 0 (không phân biệt được).
    /// </summary>
    public List<string> ZeroVarianceCriteriaCodes { get; set; }
        = new();
}

