using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Models;

namespace SupplierSelectionDSS.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache? _cache;

    private const string SystemKpisCacheKey = "Dashboard_SystemKpis_v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public DashboardService(ApplicationDbContext context, IMemoryCache? cache = null)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(int? batchId = null)
    {
        var model = new DashboardViewModel();

        // =========================================================
        // 1. SYSTEM-WIDE KPIS (CACHED FOR 30s)
        // =========================================================
        var kpis = await GetSystemKpisAsync();
        model.TotalSuppliers = kpis.TotalSuppliers;
        model.ActiveSuppliers = kpis.ActiveSuppliers;
        model.TotalProductGroups = kpis.TotalProductGroups;
        model.TotalBatches = kpis.TotalBatches;
        model.CompletedBatches = kpis.CompletedBatches;
        model.TotalCriteria = kpis.TotalCriteria;
        model.BatchStatusDistribution = kpis.BatchStatusDistribution;
        model.SupplierStatusDistribution = kpis.SupplierStatusDistribution;
        model.EvaluationTrend = kpis.EvaluationTrend;

        // =========================================================
        // 2. AVAILABLE BATCHES LIST (ORDERED DESC, TOP 50)
        // =========================================================
        var batches = await _context.EvaluationBatches
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new BatchSelectionOptionViewModel
            {
                BatchId = x.BatchId,
                BatchCode = x.BatchCode,
                BatchName = x.BatchName,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        // If a specific batch was requested and is outside top 50, load it so dropdown can select it
        if (batchId.HasValue && batches.All(b => b.BatchId != batchId.Value))
        {
            var requestedBatch = await _context.EvaluationBatches
                .AsNoTracking()
                .Where(x => x.BatchId == batchId.Value)
                .Select(x => new BatchSelectionOptionViewModel
                {
                    BatchId = x.BatchId,
                    BatchCode = x.BatchCode,
                    BatchName = x.BatchName,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (requestedBatch != null)
            {
                batches.Insert(0, requestedBatch);
            }
        }

        model.AvailableBatches = batches;

        // Determine active target batch
        int? targetBatchId = null;
        if (batchId.HasValue && batches.Any(b => b.BatchId == batchId.Value))
        {
            targetBatchId = batchId.Value;
        }
        else
        {
            // Prefer latest Evaluating or Completed batch, otherwise the latest created
            var defaultBatch = batches.FirstOrDefault(b => b.Status == BatchStatus.Evaluating)
                               ?? batches.FirstOrDefault(b => b.Status == BatchStatus.Completed)
                               ?? batches.FirstOrDefault();
            targetBatchId = defaultBatch?.BatchId;
        }

        model.SelectedBatchId = targetBatchId;

        // =========================================================
        // 3. TARGET BATCH CONTEXT & METRICS
        // =========================================================
        if (targetBatchId.HasValue)
        {
            var bId = targetBatchId.Value;

            // Load batch header and criteria via projection to avoid Cartesian product and entity overhead
            var currentBatchEntity = await _context.EvaluationBatches
                .AsNoTracking()
                .Where(x => x.BatchId == bId)
                .Select(x => new
                {
                    x.BatchId,
                    x.BatchCode,
                    x.BatchName,
                    GroupCode = x.Group != null ? x.Group.GroupCode : string.Empty,
                    GroupName = x.Group != null ? x.Group.GroupName : "Unassigned",
                    x.Status,
                    TotalSuppliers = x.Suppliers.Count,
                    x.CreatedAt,
                    Criteria = x.Criteria.OrderBy(c => c.CriteriaCode).Select(c => new
                    {
                        c.CriteriaId,
                        c.CriteriaCode,
                        c.CriteriaName,
                        c.CriteriaType,
                        c.Unit
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (currentBatchEntity != null)
            {
                var scoredSuppliersCount = await _context.EvaluationScores
                    .AsNoTracking()
                    .Where(x => x.BatchId == bId)
                    .Select(x => x.SupplierId)
                    .Distinct()
                    .CountAsync();

                model.CurrentBatch = new DashboardCurrentBatchViewModel
                {
                    BatchId = currentBatchEntity.BatchId,
                    BatchCode = currentBatchEntity.BatchCode,
                    BatchName = currentBatchEntity.BatchName,
                    GroupCode = currentBatchEntity.GroupCode,
                    GroupName = currentBatchEntity.GroupName,
                    Status = currentBatchEntity.Status,
                    TotalSuppliers = currentBatchEntity.TotalSuppliers,
                    ScoredSuppliers = scoredSuppliersCount,
                    TotalCriteria = currentBatchEntity.Criteria.Count,
                    CreatedAt = currentBatchEntity.CreatedAt
                };

                // AHP Result for Current Batch
                var ahp = await _context.Ahpresults
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BatchId == bId);

                if (ahp != null)
                {
                    model.AhpCrValue = ahp.Crvalue;
                    model.IsAhpConsistent = ahp.IsConsistent;
                    model.AhpLambdaMax = ahp.LambdaMax;
                    model.AhpCiValue = ahp.Civalue;
                    model.AhpRiValue = ahp.Rivalue;
                }

                // AHP Criteria Weights
                model.AhpCriteriaWeights = await _context.AhpcriteriaWeights
                    .AsNoTracking()
                    .Where(x => x.BatchId == bId)
                    .Include(x => x.Criteria)
                    .OrderByDescending(x => x.WeightValue)
                    .Select(w => new DashboardAhpWeightItemDto
                    {
                        CriteriaId = w.CriteriaId,
                        CriteriaCode = w.Criteria.CriteriaCode,
                        CriteriaName = w.Criteria.CriteriaName,
                        CriteriaType = w.Criteria.CriteriaType,
                        WeightValue = w.WeightValue
                    })
                    .ToListAsync();

                // TOPSIS Rankings for Current Batch (Top 5)
                var topsisResults = await _context.TopsisResults
                    .AsNoTracking()
                    .Where(x => x.BatchId == bId)
                    .Include(x => x.Supplier)
                    .OrderBy(x => x.RankNo)
                    .Take(5)
                    .ToListAsync();

                if (topsisResults.Any())
                {
                    model.Top1SupplierName = topsisResults[0].Supplier?.SupplierName ?? topsisResults[0].SupplierId.ToString();
                    model.Top1SupplierCode = topsisResults[0].Supplier?.SupplierCode ?? string.Empty;
                    model.Top1ClosenessCoefficient = topsisResults[0].ClosenessCoefficient;

                    model.TopTopsisRankings = topsisResults.Select(r => new DashboardTopsisRankingItemDto
                    {
                        RankNo = r.RankNo,
                        SupplierId = r.SupplierId,
                        SupplierCode = r.Supplier?.SupplierCode ?? string.Empty,
                        SupplierName = r.Supplier?.SupplierName ?? "NCC #" + r.SupplierId,
                        GroupName = currentBatchEntity.GroupName,
                        ClosenessCoefficient = r.ClosenessCoefficient,
                        DistanceToPis = r.DistanceToPis,
                        DistanceToNis = r.DistanceToNis,
                        Status = r.Supplier?.Status ?? "Active"
                    }).ToList();
                }

                // Approvals for Current Batch
                var approval = await _context.Approvals
                    .AsNoTracking()
                    .Where(x => x.BatchId == bId)
                    .OrderByDescending(x => x.ApprovalDate)
                    .Include(x => x.SelectedSupplier)
                    .Include(x => x.ApprovedByNavigation)
                    .FirstOrDefaultAsync();

                if (approval != null)
                {
                    var latestReportId = await _context.Reports
                        .AsNoTracking()
                        .Where(x => x.BatchId == bId)
                        .OrderByDescending(x => x.GeneratedAt)
                        .Select(x => (int?)x.ReportId)
                        .FirstOrDefaultAsync();

                    model.ApprovalInfo = new DashboardApprovalDto
                    {
                        ApprovalId = approval.ApprovalId,
                        Status = approval.Status,
                        SelectedSupplierName = approval.SelectedSupplier?.SupplierName,
                        SelectedSupplierCode = approval.SelectedSupplier?.SupplierCode,
                        ApproverName = approval.ApprovedByNavigation?.FullName ?? "System Administrator",
                        ApprovalDate = approval.ApprovalDate,
                        Notes = approval.Notes,
                        HasReport = latestReportId.HasValue,
                        LatestReportId = latestReportId
                    };
                }

                // Workflow Stepper Calculation (aligned with BatchWorkflowReadiness)
                var totalScoresCount = await _context.EvaluationScores
                    .AsNoTracking()
                    .CountAsync(x => x.BatchId == bId);
                var expectedScoresCount = currentBatchEntity.TotalSuppliers * currentBatchEntity.Criteria.Count;

                var hasScores = expectedScoresCount > 0 && totalScoresCount >= expectedScoresCount;
                var hasAhp = ahp != null && ahp.IsConsistent && model.AhpCriteriaWeights.Count >= currentBatchEntity.Criteria.Count;
                var hasTopsis = currentBatchEntity.TotalSuppliers > 0 && topsisResults.Count >= Math.Min(5, currentBatchEntity.TotalSuppliers);
                var hasApproval = string.Equals(currentBatchEntity.Status, BatchStatus.Completed, StringComparison.OrdinalIgnoreCase)
                    || (approval != null && string.Equals(approval.Status, "Approved", StringComparison.OrdinalIgnoreCase));

                var workflow = new DashboardWorkflowProgressDto
                {
                    Step1DataInputDone = hasScores,
                    Step2AhpDone = hasAhp,
                    Step3TopsisDone = hasTopsis,
                    Step4ComparisonDone = hasTopsis,
                    Step5ApprovalDone = hasApproval
                };

                if (hasApproval)
                {
                    workflow.CurrentStepNumber = 5;
                    workflow.CurrentStepName = "Approval";
                    workflow.CurrentStepDescription = "Evaluation batch has been approved and the final supplier decision is recorded.";
                }
                else if (hasTopsis)
                {
                    workflow.CurrentStepNumber = 4;
                    workflow.CurrentStepName = "Review & Decision";
                    workflow.CurrentStepDescription = "TOPSIS ranking is complete and ready for comparative review and final approval.";
                }
                else if (hasAhp)
                {
                    workflow.CurrentStepNumber = 3;
                    workflow.CurrentStepName = "TOPSIS";
                    workflow.CurrentStepDescription = "AHP criteria weights are consistent; proceed to calculate TOPSIS supplier ranking.";
                }
                else if (hasScores)
                {
                    workflow.CurrentStepNumber = 2;
                    workflow.CurrentStepName = "AHP";
                    workflow.CurrentStepDescription = "All supplier evaluation scores are entered; proceed to AHP pairwise comparison.";
                }
                else
                {
                    workflow.CurrentStepNumber = 1;
                    workflow.CurrentStepName = "Supplier Scoring";
                    workflow.CurrentStepDescription = $"Enter supplier evaluation scores ({totalScoresCount}/{expectedScoresCount} cells completed).";
                }

                model.WorkflowProgress = workflow;

                // Radar Comparison for Top 3 Suppliers (Normalized 0-100 scale)
                if (topsisResults.Count >= 2 && currentBatchEntity.Criteria.Any())
                {
                    var topSuppliers = topsisResults.Take(3).ToList();
                    var criteriaList = currentBatchEntity.Criteria;

                    var batchScores = await _context.EvaluationScores
                        .AsNoTracking()
                        .Where(x => x.BatchId == bId)
                        .Select(s => new
                        {
                            s.SupplierId,
                            s.CriteriaId,
                            s.ScoreValue
                        })
                        .ToListAsync();

                    var radar = new DashboardRadarComparisonDto
                    {
                        CriteriaLabels = criteriaList.Select(c => c.CriteriaName).ToList(),
                        CriteriaTypes = criteriaList.Select(c => c.CriteriaType ?? "Benefit").ToList(),
                        CriteriaUnits = criteriaList.Select(c => c.Unit ?? "").ToList()
                    };

                    // Compute min & max per criterion across batch
                    var criteriaStats = criteriaList.ToDictionary(
                        c => c.CriteriaId,
                        c =>
                        {
                            var vals = batchScores
                                .Where(s => s.CriteriaId == c.CriteriaId)
                                .Select(s => (double)s.ScoreValue)
                                .ToList();

                            return new
                            {
                                Min = vals.Any() ? vals.Min() : 0.0,
                                Max = vals.Any() ? vals.Max() : 0.0,
                                IsCost = string.Equals(c.CriteriaType, "Cost", StringComparison.OrdinalIgnoreCase),
                                Unit = c.Unit?.Trim() ?? string.Empty
                            };
                        });

                    var palette = new[] { "#2563eb", "#10b981", "#f59e0b" };
                    for (int i = 0; i < topSuppliers.Count; i++)
                    {
                        var supp = topSuppliers[i];
                        var suppScores = new List<double>();
                        var rawScores = new List<double>();
                        var formattedScores = new List<string>();

                        foreach (var crit in criteriaList)
                        {
                            var scoreObj = batchScores.FirstOrDefault(s => s.SupplierId == supp.SupplierId && s.CriteriaId == crit.CriteriaId);
                            var rawVal = scoreObj != null ? (double)scoreObj.ScoreValue : 0.0;
                            var stat = criteriaStats[crit.CriteriaId];

                            double normScore;
                            if (stat.Max <= stat.Min)
                            {
                                normScore = 100.0;
                            }
                            else if (stat.IsCost)
                            {
                                // Tiêu chí Chi phí (càng thấp càng tốt):
                                if (stat.Min > 0 && rawVal > 0)
                                {
                                    normScore = (stat.Min / rawVal) * 100.0;
                                }
                                else
                                {
                                    normScore = ((stat.Max - rawVal) / (stat.Max - stat.Min)) * 100.0;
                                }
                            }
                            else
                            {
                                // Tiêu chí Lợi ích (càng cao càng tốt):
                                normScore = stat.Max > 0 ? (rawVal / stat.Max) * 100.0 : 0.0;
                            }

                            normScore = Math.Clamp(normScore, 0.0, 100.0);
                            suppScores.Add(Math.Round(normScore, 1));
                            rawScores.Add(Math.Round(rawVal, 2));

                            var unitText = !string.IsNullOrEmpty(stat.Unit) ? $" {stat.Unit}" : string.Empty;
                            var valText = rawVal >= 1000 ? $"{rawVal:N0}{unitText}" : $"{rawVal:0.##}{unitText}";
                            formattedScores.Add(valText);
                        }

                        radar.Datasets.Add(new DashboardRadarDatasetDto
                        {
                            SupplierName = supp.Supplier?.SupplierName ?? (supp.Supplier?.SupplierCode ?? ("NCC #" + supp.SupplierId)),
                            Color = palette[i % palette.Length],
                            Scores = suppScores,
                            RawScores = rawScores,
                            FormattedScores = formattedScores
                        });
                    }

                    model.RadarComparison = radar;
                }
            }
        }

        // =========================================================
        // 4. SYSTEM INFO
        // =========================================================
        var version = typeof(DashboardService).Assembly.GetName().Version;
        var appVersion = version != null && (version.Major > 0 || version.Minor > 0)
            ? $"v{version.Major}.{version.Minor}.{version.Build} Enterprise"
            : "v1.2.0 Enterprise";

        model.SystemInfo = new DashboardSystemInfoDto
        {
            UserCount = kpis.UserCount,
            RoleCount = UserRole.All.Count,
            AppVersion = appVersion,
            DatabaseType = "Microsoft SQL Server",
            SystemStatus = "Ready (Online)"
        };

        // =========================================================
        // 5. RECENT ACTIVITIES (AUDIT LOGS)
        // =========================================================
        model.RecentActivities = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.ActionTime)
            .Take(5)
            .Select(l => new DashboardActivityItemDto
            {
                LogId = l.LogId,
                ActionType = l.ActionType,
                Description = l.Description,
                ActionTime = l.ActionTime,
                UserName = l.User != null ? l.User.FullName : "System"
            })
            .ToListAsync();

        return model;
    }

    private async Task<DashboardKpisCache> GetSystemKpisAsync()
    {
        if (_cache != null && _cache.TryGetValue(SystemKpisCacheKey, out DashboardKpisCache? cached) && cached != null)
        {
            return cached;
        }

        // 1. Supplier counts (1 query: Total and Active)
        var supplierCounts = await _context.Suppliers
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(x => x.Status == EntityStatus.Active)
            })
            .FirstOrDefaultAsync();

        var totalSuppliers = supplierCounts?.Total ?? 0;
        var activeSuppliers = supplierCounts?.Active ?? 0;

        // 2. Product Groups count
        var totalProductGroups = await _context.ProductGroups
            .AsNoTracking()
            .CountAsync();

        // 3. Criteria count
        var totalCriteria = await _context.Criteria
            .AsNoTracking()
            .CountAsync();

        // 4. Batch Status Distribution & Total/Completed counts (1 query)
        var batchStatusGroups = await _context.EvaluationBatches
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var completedBatches = batchStatusGroups.FirstOrDefault(x => x.Status == BatchStatus.Completed)?.Count ?? 0;
        var evaluatingBatches = batchStatusGroups.FirstOrDefault(x => x.Status == BatchStatus.Evaluating)?.Count ?? 0;
        var draftBatches = batchStatusGroups.FirstOrDefault(x => x.Status == BatchStatus.Draft)?.Count ?? 0;
        var cancelledBatches = batchStatusGroups.FirstOrDefault(x => x.Status == BatchStatus.Cancelled)?.Count ?? 0;
        var totalBatches = batchStatusGroups.Sum(x => x.Count);

        var batchStatusDistribution = new DashboardDonutChartDto
        {
            TotalCount = totalBatches,
            Labels = new List<string> { "Completed", "Evaluating", "Draft", "Cancelled" },
            Values = new List<int> { completedBatches, evaluatingBatches, draftBatches, cancelledBatches },
            Colors = new List<string> { "#16a34a", "#2563eb", "#f59e0b", "#ef4444" }
        };

        var supplierStatusDistribution = new DashboardDonutChartDto
        {
            TotalCount = totalSuppliers,
            Labels = new List<string> { "Active", "Inactive" },
            Values = new List<int> { activeSuppliers, Math.Max(0, totalSuppliers - activeSuppliers) },
            Colors = new List<string> { "#16a34a", "#94a3b8" }
        };

        // 5. Evaluation Trend (1st of month 5 months ago to cover full 6 calendar months)
        var now = DateTime.UtcNow;
        var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

        var recentBatches = await _context.EvaluationBatches
            .AsNoTracking()
            .Where(x => x.CreatedAt >= startMonth)
            .Select(b => new
            {
                b.CreatedAt,
                SupplierCount = b.Suppliers.Count
            })
            .ToListAsync();

        var trendLabels = new List<string>();
        var trendBatchCounts = new List<int>();
        var trendSupplierCounts = new List<int>();

        for (int i = 5; i >= 0; i--)
        {
            var targetMonthDate = now.AddMonths(-i);
            var monthLabel = targetMonthDate.ToString("MMM yy", System.Globalization.CultureInfo.InvariantCulture);
            trendLabels.Add(monthLabel);

            var monthBatches = recentBatches
                .Where(b => b.CreatedAt.Year == targetMonthDate.Year && b.CreatedAt.Month == targetMonthDate.Month)
                .ToList();

            trendBatchCounts.Add(monthBatches.Count);
            trendSupplierCounts.Add(monthBatches.Sum(b => b.SupplierCount));
        }

        // Fallback: if no data in the last 6 months, take the last 6 recorded months without loading full supplier entities
        if (trendBatchCounts.All(c => c == 0))
        {
            var pastBatches = await _context.EvaluationBatches
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Take(200)
                .Select(b => new
                {
                    b.CreatedAt.Year,
                    b.CreatedAt.Month,
                    SupplierCount = b.Suppliers.Count
                })
                .ToListAsync();

            if (pastBatches.Any())
            {
                trendLabels.Clear();
                trendBatchCounts.Clear();
                trendSupplierCounts.Clear();

                var groups = pastBatches
                    .GroupBy(b => new { b.Year, b.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .TakeLast(6);

                foreach (var g in groups)
                {
                    var d = new DateTime(g.Key.Year, g.Key.Month, 1);
                    trendLabels.Add(d.ToString("MMM yy", System.Globalization.CultureInfo.InvariantCulture));
                    trendBatchCounts.Add(g.Count());
                    trendSupplierCounts.Add(g.Sum(b => b.SupplierCount));
                }
            }
        }

        var evaluationTrend = new DashboardTrendChartDto
        {
            MonthLabels = trendLabels,
            BatchCounts = trendBatchCounts,
            SupplierCounts = trendSupplierCounts
        };

        // 6. User Count
        var userCount = await _context.Users
            .AsNoTracking()
            .CountAsync();

        var kpiResult = new DashboardKpisCache(
            totalSuppliers,
            activeSuppliers,
            totalProductGroups,
            totalBatches,
            completedBatches,
            totalCriteria,
            userCount,
            batchStatusDistribution,
            supplierStatusDistribution,
            evaluationTrend);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Size = 1
        };
        _cache?.Set(SystemKpisCacheKey, kpiResult, cacheOptions);
        return kpiResult;
    }

    private sealed record DashboardKpisCache(
        int TotalSuppliers,
        int ActiveSuppliers,
        int TotalProductGroups,
        int TotalBatches,
        int CompletedBatches,
        int TotalCriteria,
        int UserCount,
        DashboardDonutChartDto BatchStatusDistribution,
        DashboardDonutChartDto SupplierStatusDistribution,
        DashboardTrendChartDto EvaluationTrend);
}
