namespace SupplierSelectionDSS.Models;

public class DecisionResultViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BatchName { get; set; } = string.Empty;

    public string BatchStatus { get; set; } = string.Empty;


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
}


public class DecisionRankingViewModel
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public decimal DistanceToPIS { get; set; }

    public decimal DistanceToNIS { get; set; }

    public decimal ClosenessCoefficient { get; set; }

    public int RankNo { get; set; }
}