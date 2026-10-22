using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Models;

namespace SupplierSelectionDSS.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(int? batchId = null)
    {
        var model = new DashboardViewModel();

        // =========================================================
        // 1. SYSTEM-WIDE KPIS (100% GENUINE DATABASE DATA)
        // =========================================================
        model.TotalSuppliers = await _context.Suppliers.AsNoTracking().CountAsync();
        model.ActiveSuppliers = await _context.Suppliers.AsNoTracking().CountAsync(x => x.Status == EntityStatus.Active);
        model.TotalProductGroups = await _context.ProductGroups.AsNoTracking().CountAsync();
        model.TotalBatches = await _context.EvaluationBatches.AsNoTracking().CountAsync();
        model.CompletedBatches = await _context.EvaluationBatches.AsNoTracking().CountAsync(x => x.Status == BatchStatus.Completed);
        model.TotalCriteria = await _context.Criteria.AsNoTracking().CountAsync();

        // =========================================================
        // 2. AVAILABLE BATCHES LIST (ORDERED DESC)
        // =========================================================
        var batches = await _context.EvaluationBatches
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BatchSelectionOptionViewModel
            {
                BatchId = x.BatchId,
                BatchCode = x.BatchCode,
                BatchName = x.BatchName,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

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
            var currentBatchEntity = await _context.EvaluationBatches
                .AsNoTracking()
                .Include(x => x.Group)
                .Include(x => x.Suppliers)
                .Include(x => x.Criteria)
                .FirstOrDefaultAsync(x => x.BatchId == bId);

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
                    GroupCode = currentBatchEntity.Group?.GroupCode ?? string.Empty,
                    GroupName = currentBatchEntity.Group?.GroupName ?? "Chưa chỉ định",
                    Status = currentBatchEntity.Status,
                    TotalSuppliers = currentBatchEntity.Suppliers?.Count ?? 0,
                    ScoredSuppliers = scoredSuppliersCount,
                    TotalCriteria = currentBatchEntity.Criteria?.Count ?? 0,
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
                        GroupName = currentBatchEntity.Group?.GroupName ?? string.Empty,
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
                    var latestReport = await _context.Reports
                        .AsNoTracking()
                        .Where(x => x.BatchId == bId)
                        .OrderByDescending(x => x.GeneratedAt)
                        .FirstOrDefaultAsync();

                    model.ApprovalInfo = new DashboardApprovalDto
                    {
                        ApprovalId = approval.ApprovalId,
                        Status = approval.Status,
                        SelectedSupplierName = approval.SelectedSupplier?.SupplierName,
                        SelectedSupplierCode = approval.SelectedSupplier?.SupplierCode,
                        ApproverName = approval.ApprovedByNavigation?.FullName ?? "Quản trị viên",
                        ApprovalDate = approval.ApprovalDate,
                        Notes = approval.Notes,
                        HasReport = latestReport != null,
                        LatestReportId = latestReport?.ReportId
                    };
                }

                // Workflow Stepper Calculation
                var hasScores = scoredSuppliersCount > 0;
                var hasAhp = ahp != null && ahp.IsConsistent;
                var hasTopsis = topsisResults.Any();
                var hasApproval = approval != null && string.Equals(approval.Status, "Approved", StringComparison.OrdinalIgnoreCase);

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
                    workflow.CurrentStepName = "Phê duyệt";
                    workflow.CurrentStepDescription = "Đợt đánh giá đã được phê duyệt và hoàn tất quyết định.";
                }
                else if (hasTopsis)
                {
                    workflow.CurrentStepNumber = 4;
                    workflow.CurrentStepName = "So sánh & Đánh giá";
                    workflow.CurrentStepDescription = "Đã tính toán TOPSIS, sẵn sàng phân tích so sánh và trình phê duyệt.";
                }
                else if (hasAhp)
                {
                    workflow.CurrentStepNumber = 3;
                    workflow.CurrentStepName = "TOPSIS";
                    workflow.CurrentStepDescription = "Trọng số AHP đã sẵn sàng, tiếp tục tính toán xếp hạng TOPSIS.";
                }
                else if (hasScores)
                {
                    workflow.CurrentStepNumber = 2;
                    workflow.CurrentStepName = "AHP";
                    workflow.CurrentStepDescription = "Đã có dữ liệu điểm đánh giá, tiếp tục so sánh cặp tiêu chí AHP.";
                }
                else
                {
                    workflow.CurrentStepNumber = 1;
                    workflow.CurrentStepName = "Nhập dữ liệu";
                    workflow.CurrentStepDescription = "Thu thập và nhập điểm đánh giá nhà cung cấp theo tiêu chí.";
                }

                model.WorkflowProgress = workflow;

                // Radar Comparison for Top 3 Suppliers (Normalized 0-100 scale)
                if (topsisResults.Count >= 2 && currentBatchEntity.Criteria != null && currentBatchEntity.Criteria.Any())
                {
                    var topSuppliers = topsisResults.Take(3).ToList();
                    var topSupplierIds = topSuppliers.Select(s => s.SupplierId).ToList();
                    var criteriaList = currentBatchEntity.Criteria.OrderBy(c => c.CriteriaCode).ToList();

                    var batchScores = await _context.EvaluationScores
                        .AsNoTracking()
                        .Where(x => x.BatchId == bId)
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
        // 4. CHARTS ROW 1 DTOS
        // =========================================================

        // Donut: Batch Status Distribution
        var batchStatusGroups = await _context.EvaluationBatches
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var completedCount = batchStatusGroups.FirstOrDefault(x => x.Status == BatchStatus.Completed)?.Count ?? 0;
        var evaluatingCount = batchStatusGroups.FirstOrDefault(x => x.Status == BatchStatus.Evaluating)?.Count ?? 0;
        var draftCount = batchStatusGroups.FirstOrDefault(x => x.Status == BatchStatus.Draft)?.Count ?? 0;
        var cancelledCount = batchStatusGroups.FirstOrDefault(x => x.Status == BatchStatus.Cancelled)?.Count ?? 0;

        model.BatchStatusDistribution = new DashboardDonutChartDto
        {
            TotalCount = model.TotalBatches,
            Labels = new List<string> { "Hoàn thành", "Đang đánh giá", "Bản nháp", "Đã hủy" },
            Values = new List<int> { completedCount, evaluatingCount, draftCount, cancelledCount },
            Colors = new List<string> { "#16a34a", "#2563eb", "#f59e0b", "#ef4444" }
        };

        // Donut: Supplier Status Distribution
        var inactiveSuppliers = model.TotalSuppliers - model.ActiveSuppliers;
        model.SupplierStatusDistribution = new DashboardDonutChartDto
        {
            TotalCount = model.TotalSuppliers,
            Labels = new List<string> { "Hoạt động", "Không hoạt động" },
            Values = new List<int> { model.ActiveSuppliers, inactiveSuppliers },
            Colors = new List<string> { "#16a34a", "#94a3b8" }
        };

        // Line: Evaluation Trend over recent months
        var now = DateTime.UtcNow;
        var trendLabels = new List<string>();
        var trendBatchCounts = new List<int>();
        var trendSupplierCounts = new List<int>();

        // Query historical batches grouped by year and month
        var recentBatchEntities = await _context.EvaluationBatches
            .AsNoTracking()
            .Include(x => x.Suppliers)
            .Where(x => x.CreatedAt >= now.AddMonths(-5).Date)
            .ToListAsync();

        for (int i = 5; i >= 0; i--)
        {
            var targetMonthDate = now.AddMonths(-i);
            var monthLabel = "T" + targetMonthDate.Month + "/" + targetMonthDate.ToString("yy");
            trendLabels.Add(monthLabel);

            var monthBatches = recentBatchEntities
                .Where(b => b.CreatedAt.Year == targetMonthDate.Year && b.CreatedAt.Month == targetMonthDate.Month)
                .ToList();

            trendBatchCounts.Add(monthBatches.Count);
            trendSupplierCounts.Add(monthBatches.Sum(b => b.Suppliers?.Count ?? 0));
        }

        // If no data in the last 6 months (e.g. database seeded on specific past dates), fall back to all recorded batches
        if (trendBatchCounts.All(c => c == 0))
        {
            var allBatchesGrouped = await _context.EvaluationBatches
                .AsNoTracking()
                .Include(x => x.Suppliers)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            trendLabels.Clear();
            trendBatchCounts.Clear();
            trendSupplierCounts.Clear();

            var groups = allBatchesGrouped
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .TakeLast(6);

            foreach (var g in groups)
            {
                trendLabels.Add("T" + g.Key.Month + "/" + (g.Key.Year % 100));
                trendBatchCounts.Add(g.Count());
                trendSupplierCounts.Add(g.Sum(b => b.Suppliers?.Count ?? 0));
            }
        }

        model.EvaluationTrend = new DashboardTrendChartDto
        {
            MonthLabels = trendLabels,
            BatchCounts = trendBatchCounts,
            SupplierCounts = trendSupplierCounts
        };

        // =========================================================
        // 5. SYSTEM INFO & AUDIT LOGS
        // =========================================================
        model.SystemInfo = new DashboardSystemInfoDto
        {
            UserCount = await _context.Users.AsNoTracking().CountAsync(),
            RoleCount = 4,
            AppVersion = "v1.0.0 Enterprise",
            DatabaseType = "Microsoft SQL Server",
            SystemStatus = "Sẵn sàng (Online)"
        };

        model.RecentActivities = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.ActionTime)
            .Take(5)
            .Include(x => x.User)
            .Select(l => new DashboardActivityItemDto
            {
                LogId = l.LogId,
                ActionType = l.ActionType,
                Description = l.Description,
                ActionTime = l.ActionTime,
                UserName = l.User != null ? l.User.FullName : "Hệ thống"
            })
            .ToListAsync();

        return model;
    }
}
