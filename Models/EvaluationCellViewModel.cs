namespace SupplierSelectionDSS.Models;

public class EvaluationCellViewModel
{
    public decimal? Score { get; set; }

    public string? RawInput { get; set; }

    public string? ErrorMessage { get; set; }

    public int? EvaluationScoreId { get; set; }
}
