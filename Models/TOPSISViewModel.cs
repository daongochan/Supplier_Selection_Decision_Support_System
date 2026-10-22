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
}