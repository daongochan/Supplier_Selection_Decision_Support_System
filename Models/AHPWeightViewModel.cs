namespace SupplierSelectionDSS.Models;

public class AHPWeightViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public double WeightValue { get; set; }

    public double Percentage => WeightValue * 100.0;
}
