using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Services;

public class BatchWorkflowReadiness
{
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string BatchStatus { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;

    public int SupplierCount { get; set; }
    public int CriteriaCount { get; set; }

    public int ExpectedScoreCount { get; set; }
    public int ActualScoreCount { get; set; }
    public bool IsSetupComplete { get; set; }
    public bool IsScoresComplete { get; set; }

    public int ExpectedPairwiseCount { get; set; }
    public int ActualPairwiseCount { get; set; }
    public bool HasAhpResult { get; set; }
    public bool HasCompleteAhpWeights { get; set; }
    public bool IsAhpConsistent { get; set; }
    public decimal? AhpCrValue { get; set; }
    public bool IsAhpReady => HasAhpResult && HasCompleteAhpWeights && IsAhpConsistent;

    public int TopsisResultCount { get; set; }
    public bool HasValidTopsisRanks { get; set; }
    public bool IsTopsisComplete =>
        IsScoresComplete
        && IsAhpReady
        && SupplierCount >= 2
        && TopsisResultCount == SupplierCount
        && HasValidTopsisRanks;

    public bool IsCompleted { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsEvaluating { get; set; }
    public bool IsDraft { get; set; }

    public string? LatestDecisionStatus { get; set; }
    public bool HasFinalApproval { get; set; }
    public bool CanGenerateReport => IsCompleted && HasFinalApproval;

    public int CurrentStepNumber { get; set; }
    public string CurrentStepKey { get; set; } = string.Empty;
    public string CurrentStepTitle { get; set; } = string.Empty;
    public string CurrentStepDescription { get; set; } = string.Empty;
}

public class BatchWorkflowReadinessService
{
    private readonly ApplicationDbContext _context;

    public BatchWorkflowReadinessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BatchWorkflowReadiness?> GetReadinessAsync(int batchId)
    {
        var raw = await _context.EvaluationBatches
            .AsNoTracking()
            .Where(b => b.BatchId == batchId)
            .Select(b => new
            {
                b.BatchId,
                b.BatchCode,
                b.BatchName,
                b.Status,
                b.RowVersion,
                SupplierCount = b.Suppliers.Count,
                CriteriaCount = b.Criteria.Count,
                ScoreCount = b.EvaluationScores.Count,
                ActualPairs = b.AhppairwiseComparisons.Count(c => c.CriteriaRowId != c.CriteriaColId),
                HasAhpResult = b.Ahpresult != null,
                IsAhpConsistent = b.Ahpresult != null && b.Ahpresult.IsConsistent,
                AhpCrValue = b.Ahpresult != null ? (decimal?)b.Ahpresult.Crvalue : null,
                AhpWeightsCount = b.AhpcriteriaWeights.Count,
                TopsisCount = b.TopsisResults.Count,
                HasValidRanks = b.TopsisResults.Count > 0 && b.TopsisResults.Count == b.Suppliers.Count && b.TopsisResults.All(r => r.RankNo > 0),
                HasFinalApproval = b.Approvals.Any(a => a.IsFinal && a.Status == ApprovalStatus.Approved),
                LatestApproval = b.Approvals
                    .OrderByDescending(a => a.ApprovalDate)
                    .ThenByDescending(a => a.ApprovalId)
                    .Select(a => new { a.Status, a.IsFinal })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (raw == null)
        {
            return null;
        }

        var supplierCount = raw.SupplierCount;
        var criteriaCount = raw.CriteriaCount;
        var expectedScores = supplierCount * criteriaCount;

        var isSetupComplete = supplierCount >= 2 && criteriaCount >= 2;
        var isScoresComplete = isSetupComplete && expectedScores > 0 && raw.ScoreCount == expectedScores;
        var expectedPairs = criteriaCount >= 2 ? criteriaCount * (criteriaCount - 1) / 2 : 0;
        var hasCompleteAhpWeights = criteriaCount >= 2 && (raw.AhpWeightsCount == criteriaCount || (raw.HasAhpResult && raw.AhpWeightsCount == 0));

        var isCancelled = string.Equals(raw.Status, BatchStatus.Cancelled, StringComparison.OrdinalIgnoreCase);
        var isCompleted = string.Equals(raw.Status, BatchStatus.Completed, StringComparison.OrdinalIgnoreCase) || raw.HasFinalApproval;
        var isDraft = string.Equals(raw.Status, BatchStatus.Draft, StringComparison.OrdinalIgnoreCase);
        var isEvaluating = string.Equals(raw.Status, BatchStatus.Evaluating, StringComparison.OrdinalIgnoreCase);

        var readiness = new BatchWorkflowReadiness
        {
            BatchId = raw.BatchId,
            BatchCode = raw.BatchCode,
            BatchName = raw.BatchName,
            BatchStatus = raw.Status,
            RowVersion = BatchConcurrencyHelper.ToBase64(raw.RowVersion),
            SupplierCount = supplierCount,
            CriteriaCount = criteriaCount,
            ExpectedScoreCount = expectedScores,
            ActualScoreCount = raw.ScoreCount,
            IsSetupComplete = isSetupComplete,
            IsScoresComplete = isScoresComplete,
            ExpectedPairwiseCount = expectedPairs,
            ActualPairwiseCount = raw.ActualPairs,
            HasAhpResult = raw.HasAhpResult,
            HasCompleteAhpWeights = hasCompleteAhpWeights,
            IsAhpConsistent = raw.IsAhpConsistent,
            AhpCrValue = raw.AhpCrValue,
            TopsisResultCount = raw.TopsisCount,
            HasValidTopsisRanks = raw.HasValidRanks,
            IsCompleted = isCompleted,
            IsCancelled = isCancelled,
            IsEvaluating = isEvaluating,
            IsDraft = isDraft,
            LatestDecisionStatus = raw.LatestApproval?.Status,
            HasFinalApproval = raw.HasFinalApproval || isCompleted
        };

        PopulateStepDetails(readiness);
        return readiness;
    }

    public static BatchWorkflowReadiness ComputeFromBatchEntity(EvaluationBatch batch)
    {
        var supplierIds = batch.Suppliers.Select(s => s.SupplierId).ToHashSet();
        var criteriaIds = batch.Criteria.Select(c => c.CriteriaId).ToHashSet();

        var supplierCount = supplierIds.Count;
        var criteriaCount = criteriaIds.Count;
        var expectedScores = supplierCount * criteriaCount;

        var validScoresCount = batch.EvaluationScores
            .Where(s => supplierIds.Contains(s.SupplierId) && criteriaIds.Contains(s.CriteriaId))
            .Select(s => (s.SupplierId, s.CriteriaId))
            .Distinct()
            .Count();

        var isSetupComplete = supplierCount >= 2 && criteriaCount >= 2;
        var isScoresComplete = isSetupComplete && expectedScores > 0 && validScoresCount == expectedScores;

        var expectedPairs = criteriaCount >= 2 ? criteriaCount * (criteriaCount - 1) / 2 : 0;
        var actualPairs = batch.AhppairwiseComparisons
            .Count(c => criteriaIds.Contains(c.CriteriaRowId) && criteriaIds.Contains(c.CriteriaColId) && c.CriteriaRowId != c.CriteriaColId);

        var hasAhpResult = batch.Ahpresult != null;
        var isAhpConsistent = batch.Ahpresult?.IsConsistent == true;
        var ahpWeightsCount = batch.AhpcriteriaWeights != null && batch.AhpcriteriaWeights.Count > 0
            ? batch.AhpcriteriaWeights.Count(w => criteriaIds.Contains(w.CriteriaId))
            : (hasAhpResult ? criteriaCount : 0);
        var hasCompleteAhpWeights = criteriaCount >= 2 && ahpWeightsCount == criteriaCount;

        var validTopsis = batch.TopsisResults
            .Where(r => supplierIds.Contains(r.SupplierId))
            .ToList();
        var topsisCount = validTopsis.Select(r => r.SupplierId).Distinct().Count();
        var hasValidRanks = validTopsis.Count == supplierCount && validTopsis.All(r => r.RankNo > 0);

        var finalApproval = batch.Approvals
            .Where(a => a.IsFinal && string.Equals(a.Status, ApprovalStatus.Approved, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.ApprovalDate)
            .ThenByDescending(a => a.ApprovalId)
            .FirstOrDefault();

        var latestApproval = batch.Approvals
            .OrderByDescending(a => a.ApprovalDate)
            .ThenByDescending(a => a.ApprovalId)
            .FirstOrDefault();

        var isCancelled = string.Equals(batch.Status, BatchStatus.Cancelled, StringComparison.OrdinalIgnoreCase);
        var isCompleted = string.Equals(batch.Status, BatchStatus.Completed, StringComparison.OrdinalIgnoreCase) || finalApproval != null;
        var isDraft = string.Equals(batch.Status, BatchStatus.Draft, StringComparison.OrdinalIgnoreCase);
        var isEvaluating = string.Equals(batch.Status, BatchStatus.Evaluating, StringComparison.OrdinalIgnoreCase);

        var readiness = new BatchWorkflowReadiness
        {
            BatchId = batch.BatchId,
            BatchCode = batch.BatchCode,
            BatchName = batch.BatchName,
            BatchStatus = batch.Status,
            RowVersion = BatchConcurrencyHelper.ToBase64(batch.RowVersion),
            SupplierCount = supplierCount,
            CriteriaCount = criteriaCount,
            ExpectedScoreCount = expectedScores,
            ActualScoreCount = validScoresCount,
            IsSetupComplete = isSetupComplete,
            IsScoresComplete = isScoresComplete,
            ExpectedPairwiseCount = expectedPairs,
            ActualPairwiseCount = actualPairs,
            HasAhpResult = hasAhpResult,
            HasCompleteAhpWeights = hasCompleteAhpWeights,
            IsAhpConsistent = isAhpConsistent,
            AhpCrValue = batch.Ahpresult?.Crvalue,
            TopsisResultCount = topsisCount,
            HasValidTopsisRanks = hasValidRanks,
            IsCompleted = isCompleted,
            IsCancelled = isCancelled,
            IsEvaluating = isEvaluating,
            IsDraft = isDraft,
            LatestDecisionStatus = latestApproval?.Status,
            HasFinalApproval = finalApproval != null || isCompleted
        };

        PopulateStepDetails(readiness);
        return readiness;
    }

    private static void PopulateStepDetails(BatchWorkflowReadiness readiness)
    {
        if (readiness.IsCancelled)
        {
            readiness.CurrentStepNumber = 0;
            readiness.CurrentStepKey = "Cancelled";
            readiness.CurrentStepTitle = "Batch Cancelled";
            readiness.CurrentStepDescription = "This evaluation batch has been cancelled and locked.";
        }
        else if (readiness.IsCompleted)
        {
            readiness.CurrentStepNumber = 5;
            readiness.CurrentStepKey = "Completed";
            readiness.CurrentStepTitle = "Evaluation Completed";
            readiness.CurrentStepDescription = "Final supplier selection decision has been approved and frozen.";
        }
        else if (readiness.IsDraft)
        {
            readiness.CurrentStepNumber = 1;
            readiness.CurrentStepKey = "Draft";
            readiness.CurrentStepTitle = "Ready to Start";
            readiness.CurrentStepDescription = "Scope is configured. Start the evaluation batch to unlock scoring and AHP.";
        }
        else if (!readiness.IsScoresComplete)
        {
            readiness.CurrentStepNumber = 2;
            readiness.CurrentStepKey = "Evaluation";
            readiness.CurrentStepTitle = "Complete Supplier Evaluation";
            readiness.CurrentStepDescription = $"Entered {readiness.ActualScoreCount} of {readiness.ExpectedScoreCount} required supplier-criterion scores.";
        }
        else if (!readiness.IsAhpReady)
        {
            readiness.CurrentStepNumber = 3;
            readiness.CurrentStepKey = "AHP";
            readiness.CurrentStepTitle = readiness.HasAhpResult ? "Review AHP Consistency" : "Calculate AHP Weights";
            readiness.CurrentStepDescription = readiness.HasAhpResult
                ? $"Current Consistency Ratio CR = {(readiness.AhpCrValue ?? 0m):0.0000}. Adjust pairwise judgments so CR <= 0.10."
                : "Complete pairwise criteria comparisons and calculate AHP weights.";
        }
        else if (!readiness.IsTopsisComplete)
        {
            readiness.CurrentStepNumber = 4;
            readiness.CurrentStepKey = "TOPSIS";
            readiness.CurrentStepTitle = "Calculate TOPSIS Ranking";
            readiness.CurrentStepDescription = "Scores and consistent AHP weights are ready. Run TOPSIS to rank suppliers.";
        }
        else
        {
            readiness.CurrentStepNumber = 5;
            readiness.CurrentStepKey = "Decision";
            readiness.CurrentStepTitle = "Review & Approve Decision";
            readiness.CurrentStepDescription = "TOPSIS ranking is complete. Review sensitivity analysis and record the final decision.";
        }
    }
}
