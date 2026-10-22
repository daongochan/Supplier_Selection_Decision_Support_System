namespace SupplierSelectionDSS.Models;

public class TOPSISCriteriaViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public string CriteriaType { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public decimal Weight { get; set; }

    public double WeightPercentage => (double)Weight * 100.0;
}
