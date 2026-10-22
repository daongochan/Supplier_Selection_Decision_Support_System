using System;
using System.Collections.Generic;

namespace SupplierSelectionDSS.Models;

public class DashboardViewModel
{
    // =========================================================
    // 1. BATCH CONTEXT & SELECTOR
    // =========================================================
    public int? SelectedBatchId { get; set; }

    public List<BatchSelectionOptionViewModel> AvailableBatches { get; set; } = new();

    public DashboardCurrentBatchViewModel? CurrentBatch { get; set; }

    // =========================================================
    // 2. SYSTEM KPI METRICS (100% GENUINE DATABASE DATA)
    // =========================================================
    public int TotalSuppliers { get; set; }

    public int ActiveSuppliers { get; set; }

    public int TotalProductGroups { get; set; }

    public int TotalBatches { get; set; }

    public int CompletedBatches { get; set; }

    public int TotalCriteria { get; set; }

    // =========================================================
    // 3. CURRENT BATCH AHP & TOPSIS KPIS
    // =========================================================
    public decimal? AhpCrValue { get; set; }

    public bool? IsAhpConsistent { get; set; }

    public decimal? AhpLambdaMax { get; set; }

    public decimal? AhpCiValue { get; set; }

    public decimal? AhpRiValue { get; set; }

    public string? Top1SupplierName { get; set; }

    public string? Top1SupplierCode { get; set; }

    public decimal? Top1ClosenessCoefficient { get; set; }

    // =========================================================
    // 4. CHARTS ROW 1 DTOS
    // =========================================================
    public DashboardDonutChartDto BatchStatusDistribution { get; set; } = new();

    public DashboardTrendChartDto EvaluationTrend { get; set; } = new();

    public DashboardDonutChartDto SupplierStatusDistribution { get; set; } = new();

    // =========================================================
    // 5. TOPSIS RANKING & BAR CHART & WORKFLOW PROGRESS (ROW 2)
    // =========================================================
    public List<DashboardTopsisRankingItemDto> TopTopsisRankings { get; set; } = new();

    public DashboardWorkflowProgressDto WorkflowProgress { get; set; } = new();

    // =========================================================
    // 6. AHP WEIGHTS, RADAR COMPARISON, SYSTEM INFO, APPROVAL (ROW 3)
    // =========================================================
    public List<DashboardAhpWeightItemDto> AhpCriteriaWeights { get; set; } = new();

    public DashboardRadarComparisonDto RadarComparison { get; set; } = new();

    public DashboardSystemInfoDto SystemInfo { get; set; } = new();

    public DashboardApprovalDto? ApprovalInfo { get; set; }

    // =========================================================
    // 7. RECENT ACTIVITIES (AUDIT LOGS)
    // =========================================================
    public List<DashboardActivityItemDto> RecentActivities { get; set; } = new();
}

// =============================================================
// SUB-MODELS & DTOS
// =============================================================

public class BatchSelectionOptionViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BatchName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class DashboardCurrentBatchViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BatchName { get; set; } = string.Empty;

    public string GroupCode { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int TotalSuppliers { get; set; }

    public int ScoredSuppliers { get; set; }

    public int TotalCriteria { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ProgressPercent => TotalSuppliers > 0
        ? (int)Math.Round((double)ScoredSuppliers * 100 / TotalSuppliers)
        : 0;
}

public class DashboardDonutChartDto
{
    public int TotalCount { get; set; }

    public List<string> Labels { get; set; } = new();

    public List<int> Values { get; set; } = new();

    public List<string> Colors { get; set; } = new();

    public List<string> SubLabels { get; set; } = new();
}

public class DashboardTrendChartDto
{
    public List<string> MonthLabels { get; set; } = new();

    public List<int> SupplierCounts { get; set; } = new();

    public List<int> BatchCounts { get; set; } = new();
}

public class DashboardTopsisRankingItemDto
{
    public int RankNo { get; set; }

    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public decimal ClosenessCoefficient { get; set; }

    public decimal DistanceToPis { get; set; }

    public decimal DistanceToNis { get; set; }

    public string Status { get; set; } = "Active";
}

public class DashboardWorkflowProgressDto
{
    public bool Step1DataInputDone { get; set; }

    public bool Step2AhpDone { get; set; }

    public bool Step3TopsisDone { get; set; }

    public bool Step4ComparisonDone { get; set; }

    public bool Step5ApprovalDone { get; set; }

    public int CurrentStepNumber { get; set; } = 1;

    public string CurrentStepName { get; set; } = "Nhập dữ liệu";

    public string CurrentStepDescription { get; set; } = "Thu thập và nhập điểm đánh giá theo tiêu chí.";

    public int OverallPercent => CurrentStepNumber switch
    {
        1 => Step1DataInputDone ? 20 : 10,
        2 => Step2AhpDone ? 40 : 30,
        3 => Step3TopsisDone ? 60 : 50,
        4 => Step4ComparisonDone ? 80 : 70,
        5 => Step5ApprovalDone ? 100 : 90,
        _ => 0
    };
}

public class DashboardAhpWeightItemDto
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public string CriteriaType { get; set; } = "Benefit";

    public decimal WeightValue { get; set; }

    public double Percentage => (double)Math.Round(WeightValue * 100, 1, MidpointRounding.AwayFromZero);
}

public class DashboardRadarComparisonDto
{
    public List<string> CriteriaLabels { get; set; } = new();

    public List<string> CriteriaTypes { get; set; } = new();

    public List<string> CriteriaUnits { get; set; } = new();

    public List<DashboardRadarDatasetDto> Datasets { get; set; } = new();
}

public class DashboardRadarDatasetDto
{
    public string SupplierName { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public List<double> Scores { get; set; } = new();

    public List<double> RawScores { get; set; } = new();

    public List<string> FormattedScores { get; set; } = new();
}

public class DashboardSystemInfoDto
{
    public int UserCount { get; set; }

    public int RoleCount { get; set; } = 4;

    public string AppVersion { get; set; } = "v1.0.0 Enterprise";

    public string DatabaseType { get; set; } = "Microsoft SQL Server";

    public string SystemStatus { get; set; } = "Sẵn sàng (Online)";
}

public class DashboardApprovalDto
{
    public int ApprovalId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? SelectedSupplierName { get; set; }

    public string? SelectedSupplierCode { get; set; }

    public string ApproverName { get; set; } = string.Empty;

    public DateTime ApprovalDate { get; set; }

    public string? Notes { get; set; }

    public bool HasReport { get; set; }

    public int? LatestReportId { get; set; }
}

public class DashboardActivityItemDto
{
    public int LogId { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime ActionTime { get; set; }

    public string UserName { get; set; } = "Hệ thống";
}
