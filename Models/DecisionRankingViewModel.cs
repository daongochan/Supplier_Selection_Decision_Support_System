namespace SupplierSelectionDSS.Models;

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
