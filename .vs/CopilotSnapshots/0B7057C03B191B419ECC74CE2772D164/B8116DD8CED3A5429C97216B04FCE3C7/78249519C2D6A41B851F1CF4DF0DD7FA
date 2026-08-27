namespace SupplierSelectionDSS.Models;

public class AHPComparisonViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BatchName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<AHPComparisonCriteriaViewModel> Criteria { get; set; } = new();

    public Dictionary<string, decimal> Values { get; set; } = new();

    public AHPResultViewModel? Result { get; set; }
}


public class AHPComparisonCriteriaViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;
}
