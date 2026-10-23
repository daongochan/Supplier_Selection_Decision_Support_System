namespace SupplierSelectionDSS.Models;

public class BatchSnapshotPayload
{
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string? GroupCode { get; set; }
    public string? GroupName { get; set; }
    public int RoundNumber { get; set; } = 1;
    public string SnapshotType { get; set; } = string.Empty;
    public string MethodVersion { get; set; } = "AHP-PrincipalEigenvector-v1+TOPSIS-EuclideanL2-v1";

    public int? SelectedSupplierId { get; set; }
    public string? SelectedSupplierCode { get; set; }
    public string? SelectedSupplierName { get; set; }

    public int? ApprovedBy { get; set; }
    public int? RequestedBy { get; set; }
    public string? ActorName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string? Notes { get; set; }

    public List<BatchSnapshotSupplierItem> Suppliers { get; set; } = new();
    public List<BatchSnapshotCriterionItem> Criteria { get; set; } = new();
    public List<BatchSnapshotScoreItem> EvaluationScores { get; set; } = new();
    public List<BatchSnapshotComparisonItem> AhpComparisons { get; set; } = new();
    public BatchSnapshotAhpResultItem? AhpResult { get; set; }
    public List<BatchSnapshotWeightItem> AhpWeights { get; set; } = new();
    public List<BatchSnapshotIdealItem> TopsisIdeals { get; set; } = new();
    public List<BatchSnapshotTopsisResultItem> TopsisResults { get; set; } = new();

    public List<TOPSISMatrixRowViewModel> NormalizedMatrix { get; set; } = new();
    public List<TOPSISMatrixRowViewModel> WeightedMatrix { get; set; } = new();
    public SensitivityAnalysisResultViewModel? SensitivityAnalysis { get; set; }
}

public class BatchSnapshotSupplierItem
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
}

public class BatchSnapshotCriterionItem
{
    public int CriteriaId { get; set; }
    public string CriteriaCode { get; set; } = string.Empty;
    public string CriteriaName { get; set; } = string.Empty;
    public string CriteriaType { get; set; } = string.Empty;
    public string? Unit { get; set; }
}

public class BatchSnapshotScoreItem
{
    public int SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public int CriteriaId { get; set; }
    public string? CriteriaCode { get; set; }
    public string? CriteriaName { get; set; }
    public decimal ScoreValue { get; set; }
}

public class BatchSnapshotComparisonItem
{
    public int CriteriaRowId { get; set; }
    public string? CriteriaRowCode { get; set; }
    public int CriteriaColId { get; set; }
    public string? CriteriaColCode { get; set; }
    public decimal ComparisonValue { get; set; }
}

public class BatchSnapshotAhpResultItem
{
    public decimal LambdaMax { get; set; }
    public decimal Civalue { get; set; }
    public decimal Rivalue { get; set; }
    public decimal Crvalue { get; set; }
    public bool IsConsistent { get; set; }
    public int? N { get; set; }
}

public class BatchSnapshotWeightItem
{
    public int CriteriaId { get; set; }
    public string? CriteriaCode { get; set; }
    public string? CriteriaName { get; set; }
    public decimal WeightValue { get; set; }
}

public class BatchSnapshotIdealItem
{
    public int CriteriaId { get; set; }
    public string? CriteriaCode { get; set; }
    public string? CriteriaName { get; set; }
    public string? CriteriaType { get; set; }
    public string? Unit { get; set; }
    public decimal Pisvalue { get; set; }
    public decimal Nisvalue { get; set; }
}

public class BatchSnapshotTopsisResultItem
{
    public int SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public decimal DistanceToPis { get; set; }
    public decimal DistanceToNis { get; set; }
    public decimal ClosenessCoefficient { get; set; }
    public int RankNo { get; set; }
}

public class DecisionHistoryItemViewModel
{
    public int ApprovalId { get; set; }
    public int RoundNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public bool IsSuperseded { get; set; }
    public int? SelectedSupplierId { get; set; }
    public string? SelectedSupplierCode { get; set; }
    public string? SelectedSupplierName { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime ApprovalDate { get; set; }
    public string? Notes { get; set; }
}

public class RoundSnapshotSummaryViewModel
{
    public int SnapshotId { get; set; }
    public int RoundNumber { get; set; }
    public string SnapshotType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ActorName { get; set; }
    public string? Notes { get; set; }
    public string? TopSupplierCode { get; set; }
    public string? TopSupplierName { get; set; }
    public decimal? TopClosenessCoefficient { get; set; }
    public string? SelectedSupplierCode { get; set; }
    public string? SelectedSupplierName { get; set; }
    public decimal? AhpCrValue { get; set; }
    public string MethodVersion { get; set; } = string.Empty;
    public List<DecisionRankingViewModel> Ranking { get; set; } = new();
    public List<BatchSnapshotWeightItem> Weights { get; set; } = new();
}
