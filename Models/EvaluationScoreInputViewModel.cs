namespace SupplierSelectionDSS.Models;

public class EvaluationScoreInputViewModel
{
    public int BatchId { get; set; }

    public List<EvaluationScoreInputRowViewModel> Rows { get; set; }
        = new();
}


public class EvaluationScoreInputRowViewModel
{
    public int SupplierId { get; set; }

    public Dictionary<int, decimal?> Scores { get; set; }
        = new();
}