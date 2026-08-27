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


public class AHPWeightViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public double WeightValue { get; set; }

    public double Percentage =>
        WeightValue * 100.0;
}