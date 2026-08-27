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


// =============================================================
// CRITERIA
// =============================================================

public class TOPSISCriteriaViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public string CriteriaType { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public decimal Weight { get; set; }

    public double WeightPercentage =>
        (double)Weight * 100.0;
}


// =============================================================
// SUPPLIER DECISION MATRIX ROW
// =============================================================

public class TOPSISEvaluationRowViewModel
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public Dictionary<int, decimal> Scores { get; set; }
        = new();
}


// =============================================================
// IDEAL SOLUTION
// =============================================================

public class TOPSISIdealViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public string CriteriaType { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public decimal PISValue { get; set; }

    public decimal NISValue { get; set; }
}


// =============================================================
// RANKING
// =============================================================

public class TOPSISRankingViewModel
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public decimal DistanceToPIS { get; set; }

    public decimal DistanceToNIS { get; set; }

    public decimal ClosenessCoefficient { get; set; }

    public int RankNo { get; set; }
}