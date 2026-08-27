using System;
using System.Collections.Generic;

namespace SupplierSelectionDSS.Models;

public class DashboardViewModel
{
    // =========================================================
    // KPI
    // =========================================================

    public int SupplierCount { get; set; }

    public int ActiveSupplierCount { get; set; }

    public int CriteriaCount { get; set; }

    public int ActiveCriteriaCount { get; set; }

    public int EvaluationBatchCount { get; set; }

    public int DraftBatchCount { get; set; }

    public int EvaluatingBatchCount { get; set; }

    public int CompletedBatchCount { get; set; }


    // =========================================================
    // RECENT BATCHES
    // =========================================================

    public List<EvaluationBatchSummaryViewModel>
        RecentBatches
    { get; set; } = new();


    // =========================================================
    // TOPSIS
    // =========================================================

    public List<DashboardRankingViewModel>
        TopRankings
    { get; set; } = new();
}


// =============================================================
// RECENT EVALUATION BATCH
// =============================================================

public class EvaluationBatchSummaryViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } =
        string.Empty;

    public string BatchName { get; set; } =
        string.Empty;

    public string GroupCode { get; set; } =
        string.Empty;

    public string GroupName { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public DateTime CreatedAt { get; set; }

    public int SupplierCount { get; set; }

    public int CriteriaCount { get; set; }
}


// =============================================================
// TOPSIS RANKING
// =============================================================

public class DashboardRankingViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } =
        string.Empty;

    public string BatchName { get; set; } =
        string.Empty;

    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } =
        string.Empty;

    public string SupplierName { get; set; } =
        string.Empty;

    public decimal ClosenessCoefficient { get; set; }

    public int RankNo { get; set; }

    public DateTime CalculatedAt { get; set; }
}