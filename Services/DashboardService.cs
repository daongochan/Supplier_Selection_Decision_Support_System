using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // GET DASHBOARD
    // =========================================================

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var model = new DashboardViewModel();

        // =====================================================
        // SUPPLIERS
        // =====================================================

        var supplierStats =
            await _context.Suppliers
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),

                    Active = g.Count(
                        x => x.Status == EntityStatus.Active)
                })
                .SingleOrDefaultAsync();

        model.SupplierCount =
            supplierStats?.Total ?? 0;

        model.ActiveSupplierCount =
            supplierStats?.Active ?? 0;


        // =====================================================
        // CRITERIA
        // =====================================================

        var criteriaStats =
            await _context.Criteria
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),

                    Active = g.Count(
                        x => x.Status == EntityStatus.Active)
                })
                .SingleOrDefaultAsync();

        model.CriteriaCount =
            criteriaStats?.Total ?? 0;

        model.ActiveCriteriaCount =
            criteriaStats?.Active ?? 0;


        // =====================================================
        // EVALUATION BATCHES
        // =====================================================

        var batchStats =
            await _context.EvaluationBatches
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),

                    Draft = g.Count(
                        x => x.Status == BatchStatus.Draft),

                    Evaluating = g.Count(
                        x => x.Status == BatchStatus.Evaluating),

                    Completed = g.Count(
                        x => x.Status == BatchStatus.Completed)
                })
                .SingleOrDefaultAsync();

        model.EvaluationBatchCount =
            batchStats?.Total ?? 0;

        model.DraftBatchCount =
            batchStats?.Draft ?? 0;

        model.EvaluatingBatchCount =
            batchStats?.Evaluating ?? 0;

        model.CompletedBatchCount =
            batchStats?.Completed ?? 0;


        // =====================================================
        // RECENT EVALUATION BATCHES
        // =====================================================

        model.RecentBatches =
            await _context.EvaluationBatches
                .AsNoTracking()
                .OrderByDescending(
                    x => x.CreatedAt)
                .Take(8)
                .Select(
                    x =>
                        new EvaluationBatchSummaryViewModel
                        {
                            BatchId =
                                x.BatchId,

                            BatchCode =
                                x.BatchCode,

                            BatchName =
                                x.BatchName,

                            GroupCode =
                                x.Group.GroupCode,

                            GroupName =
                                x.Group.GroupName,

                            Status =
                                x.Status,

                            CreatedAt =
                                x.CreatedAt
                        })
                .ToListAsync();


        // =====================================================
        // LATEST TOPSIS BATCH
        // =====================================================

        /*
         * TOPSIS lưu nhiều TopsisResult cho cùng một Batch.
         *
         * Mỗi supplier có thể có CalculatedAt hơi khác nhau.
         *
         * Vì vậy:
         *
         * 1. Tìm BatchId của TOPSIS result mới nhất.
         * 2. Lấy toàn bộ ranking của Batch đó.
         */

        var latestTopsisBatchId =
            await _context.TopsisResults
                .AsNoTracking()
                .OrderByDescending(
                    x => x.CalculatedAt)
                .Select(
                    x => (int?)x.BatchId)
                .FirstOrDefaultAsync();


        if (latestTopsisBatchId.HasValue)
        {
            model.TopRankings =
                await _context.TopsisResults
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.BatchId ==
                            latestTopsisBatchId.Value)
                    .OrderBy(
                        x => x.RankNo)
                    .Take(10)
                    .Select(
                        x =>
                            new DashboardRankingViewModel
                            {
                                BatchId =
                                    x.BatchId,

                                BatchCode =
                                    x.Batch.BatchCode,

                                BatchName =
                                    x.Batch.BatchName,

                                SupplierId =
                                    x.SupplierId,

                                SupplierCode =
                                    x.Supplier.SupplierCode,

                                SupplierName =
                                    x.Supplier.SupplierName,

                                ClosenessCoefficient =
                                    x.ClosenessCoefficient,

                                RankNo =
                                    x.RankNo,

                                CalculatedAt =
                                    x.CalculatedAt
                            })
                    .ToListAsync();
        }


        // =====================================================
        // RETURN
        // =====================================================

        return model;
    }
}