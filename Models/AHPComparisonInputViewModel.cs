namespace SupplierSelectionDSS.Models;

public class AHPComparisonInputViewModel
{
    public int BatchId { get; set; }

    public List<AHPComparisonInputItemViewModel> Comparisons { get; set; }
        = new();
}


public class AHPComparisonInputItemViewModel
{
    public int CriteriaRowId { get; set; }

    public int CriteriaColId { get; set; }

    public decimal Value { get; set; }
}