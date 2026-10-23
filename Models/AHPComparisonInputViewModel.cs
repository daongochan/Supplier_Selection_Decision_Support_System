namespace SupplierSelectionDSS.Models;

public class AHPComparisonInputViewModel
{
    public int BatchId { get; set; }

    public string? RowVersion { get; set; }

    public List<AHPComparisonInputItemViewModel> Comparisons { get; set; }
        = new();
}
