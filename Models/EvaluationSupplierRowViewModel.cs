namespace SupplierSelectionDSS.Models;

public class EvaluationSupplierRowViewModel
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public Dictionary<int, EvaluationCellViewModel> Scores { get; set; } = new();
}
