using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Services;

public class ReportData
{
    public EvaluationBatch Batch { get; set; } = null!;

    public List<EvaluationScore> Scores { get; set; } = new();

    public Ahpresult? AhpResult { get; set; }

    public List<ReportAHPWeight> AhpWeights { get; set; } = new();

    public List<AhppairwiseComparison> AhpComparisons { get; set; } = new();

    public List<TopsisIdealSolution> Ideals { get; set; } = new();

    public List<TopsisResult> TopsisResults { get; set; } = new();

    public Approval? Approval { get; set; }

    public List<Models.TOPSISMatrixRowViewModel> NormalizedMatrix { get; set; } = new();

    public List<Models.TOPSISMatrixRowViewModel> WeightedMatrix { get; set; } = new();

    public Models.SensitivityAnalysisResultViewModel? SensitivityAnalysis { get; set; }
}


public class ReportAHPWeight
{
    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public decimal WeightValue { get; set; }
}
