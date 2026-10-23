namespace SupplierSelectionDSS.Models;

public class EvaluationScoreInputViewModel
{
    public int BatchId { get; set; }

    public string? RowVersion { get; set; }

    public bool ReplaceAllSubmittedCells { get; set; }

    public List<EvaluationScoreInputRowViewModel> Rows { get; set; }
        = new();
}


public class EvaluationScoreInputRowViewModel
{
    public int SupplierId { get; set; }

    public Dictionary<int, decimal?> Scores { get; set; }
        = new();

    public HashSet<int> ClearedCriteriaIds { get; set; }
        = new();
}
