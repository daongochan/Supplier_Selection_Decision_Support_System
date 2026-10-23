namespace SupplierSelectionDSS.Models;

public class DecisionResultViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BatchName { get; set; } = string.Empty;

    public string BatchStatus { get; set; } = string.Empty;

    public string RowVersion { get; set; } = string.Empty;


    // =========================================================
    // TOPSIS
    // =========================================================

    public List<DecisionRankingViewModel> Ranking { get; set; }
        = new();


    public DecisionRankingViewModel? RecommendedSupplier =>
        Ranking
            .OrderBy(x => x.RankNo)
            .FirstOrDefault();


    // =========================================================
    // APPROVAL
    // =========================================================

    public bool HasApproval { get; set; }

    public int? SelectedSupplierId { get; set; }

    public string? SelectedSupplierCode { get; set; }

    public string? SelectedSupplierName { get; set; }

    public string? ApprovalStatus { get; set; }

    public string? ApprovalNotes { get; set; }

    public bool IsFinal { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public string? ApprovedByName { get; set; }

    /// <summary>
    /// Kết quả phân tích độ nhạy (biến thiên trọng số +-10%, +-20%).
    /// </summary>
    public SensitivityAnalysisResultViewModel? SensitivityAnalysis { get; set; }

    public int CurrentRoundNumber { get; set; } = 1;

    public bool IsFrozenFromSnapshot { get; set; }

    public string? SnapshotMethodVersion { get; set; }

    public List<DecisionHistoryItemViewModel> DecisionHistory { get; set; }
        = new();

    public List<RoundSnapshotSummaryViewModel> RoundSnapshots { get; set; }
        = new();
}

