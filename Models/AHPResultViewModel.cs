namespace SupplierSelectionDSS.Models;

public class AHPResultViewModel
{
    public bool HasResult { get; set; }

    public int BatchId { get; set; }

    public int CriteriaCount { get; set; }

    public double LambdaMax { get; set; }

    public double CIValue { get; set; }

    public double RIValue { get; set; }

    public double CRValue { get; set; }

    public bool IsConsistent { get; set; }

    public DateTime? CalculatedAt { get; set; }


    public List<AHPWeightViewModel> Weights { get; set; }
        = new();
}