namespace SupplierSelectionDSS.Models;

public class TOPSISEvaluationRowViewModel
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public Dictionary<int, decimal> Scores { get; set; } = new();
}
