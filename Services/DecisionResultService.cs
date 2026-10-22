using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class DecisionResultService
{
    private readonly ApplicationDbContext _context;
    private readonly TOPSISService _topsisService;

    public DecisionResultService(
        ApplicationDbContext context,
        TOPSISService topsisService)
    {
        _context = context;
        _topsisService = topsisService;
    }



    // =========================================================
    // GET RESULT
    // =========================================================

    public async Task<DecisionResultViewModel?>
        GetResultAsync(
            int batchId)
    {
        var batch =
            await _context.EvaluationBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.BatchId ==
                        batchId);


        if (batch == null)
        {
            return null;
        }


        // =====================================================
        // TOPSIS RANKING
        // =====================================================

        var results =
            await _context.TopsisResults
                .AsNoTracking()
                .Include(
                    x =>
                        x.Supplier)
                .Where(
                    x =>
                        x.BatchId ==
                        batchId)
                .OrderBy(
                    x =>
                        x.RankNo)
                .ToListAsync();


        var model =
            new DecisionResultViewModel
            {
                BatchId =
                    batch.BatchId,

                BatchCode =
                    batch.BatchCode,

                BatchName =
                    batch.BatchName,

                BatchStatus =
                    batch.Status
            };


        foreach (
            var result
            in results)
        {
            model.Ranking.Add(
                new DecisionRankingViewModel
                {
                    SupplierId =
                        result.SupplierId,

                    SupplierCode =
                        result.Supplier.SupplierCode,

                    SupplierName =
                        result.Supplier.SupplierName,

                    DistanceToPIS =
                        result.DistanceToPis,

                    DistanceToNIS =
                        result.DistanceToNis,

                    ClosenessCoefficient =
                        result.ClosenessCoefficient,

                    RankNo =
                        result.RankNo
                });
        }


        // =====================================================
        // LATEST APPROVAL
        // =====================================================

        var approval =
            await _context.Approvals
                .AsNoTracking()
                .Include(
                    x =>
                        x.ApprovedByNavigation)
                .Include(
                    x =>
                        x.SelectedSupplier)
                .Where(
                    x =>
                        x.BatchId ==
                        batchId)
                .OrderByDescending(
                    x =>
                        x.ApprovalDate)
                .ThenByDescending(
                    x =>
                        x.ApprovalId)
                .FirstOrDefaultAsync();


        // =====================================================
        // LATEST TOPSIS CALCULATION
        // =====================================================

        var latestTopsisCalculatedAt =
            await _context.TopsisResults
                .AsNoTracking()
                .Where(
                    x =>
                        x.BatchId ==
                        batchId)
                .Select(
                    x =>
                        (DateTime?)x.CalculatedAt)
                .OrderByDescending(
                    x =>
                        x)
                .FirstOrDefaultAsync();


        // =====================================================
        // APPROVAL STATE
        // =====================================================

        if (approval != null)
        {
            /*
             * PendingReevaluation chỉ còn hiệu lực cho đến khi
             * một bộ TOPSIS mới được tính sau thời điểm
             * yêu cầu đánh giá lại.
             */

            var reevaluationCompleted =
                string.Equals(
                    approval.Status,
                    ApprovalStatus.PendingReevaluation,
                    StringComparison.OrdinalIgnoreCase)
                &&
                latestTopsisCalculatedAt.HasValue
                &&
                latestTopsisCalculatedAt.Value >
                    approval.ApprovalDate;


            if (!reevaluationCompleted)
            {
                model.HasApproval =
                    true;

                model.SelectedSupplierId =
                    approval.SelectedSupplierId;

                model.SelectedSupplierCode =
                    approval.SelectedSupplier?
                        .SupplierCode;

                model.SelectedSupplierName =
                    approval.SelectedSupplier?
                        .SupplierName;

                model.ApprovalStatus =
                    approval.Status;

                model.ApprovalNotes =
                    approval.Notes;

                model.IsFinal =
                    approval.IsFinal;

                model.ApprovalDate =
                    approval.ApprovalDate;

                model.ApprovedByName =
                    approval.ApprovedByNavigation?
                        .FullName;
            }
        }

        if (model.Ranking.Any())
        {
            var topsisWs = await _topsisService.GetWorkspaceAsync(batchId);
            model.SensitivityAnalysis = topsisWs?.SensitivityAnalysis;
        }

        return model;
    }



    // =========================================================
    // APPROVE
    // =========================================================

    public async Task<int> ApproveAsync(
        int batchId,
        int supplierId,
        int approvedBy,
        string? notes)
    {
        return await CreateApprovalAsync(
            batchId,
            supplierId,
            approvedBy,
            notes,
            ApprovalStatus.Approved,
            true);
    }


    // =========================================================
    // REJECT
    // =========================================================

    public async Task<int> RejectAsync(
        int batchId,
        string? notes,
        int approvedBy)
    {
        return await CreateApprovalAsync(
            batchId,
            null,
            approvedBy,
            notes,
            ApprovalStatus.Rejected,
            false);
    }


    // =========================================================
    // RE-EVALUATION
    // =========================================================

    public async Task<int> RequestReevaluationAsync(
        int batchId,
        string? notes,
        int approvedBy)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                // =================================================
                // BATCH
                // =================================================

                var batch =
                    await _context.EvaluationBatches
                        .FirstOrDefaultAsync(
                            x =>
                                x.BatchId ==
                                batchId);

                if (batch == null)
                {
                    throw new InvalidOperationException(
                        "Evaluation Batch không tồn tại.");
                }

                if (batch.Status == BatchStatus.Cancelled)
                {
                    throw new InvalidOperationException(
                        "Batch đã bị huỷ và không thể yêu cầu đánh giá lại.");
                }

                if (batch.Status == BatchStatus.Completed)
                {
                    throw new InvalidOperationException(
                        "Batch đã hoàn tất và không thể yêu cầu đánh giá lại.");
                }

                if (batch.Status != BatchStatus.Evaluating)
                {
                    throw new InvalidOperationException(
                        "Batch chưa ở trạng thái Evaluating.");
                }

                // =================================================
                // TOPSIS RESULT
                // =================================================

                var topsisResults =
                    await _context.TopsisResults
                        .Where(
                            x =>
                                x.BatchId ==
                                batchId)
                        .ToListAsync();

                if (topsisResults.Any())
                {
                    _context.TopsisResults
                        .RemoveRange(
                            topsisResults);
                }

                // =================================================
                // TOPSIS IDEAL SOLUTIONS
                // =================================================

                var idealSolutions =
                    await _context.TopsisIdealSolutions
                        .Where(
                            x =>
                                x.BatchId ==
                                batchId)
                        .ToListAsync();

                if (idealSolutions.Any())
                {
                    _context.TopsisIdealSolutions
                        .RemoveRange(
                            idealSolutions);
                }

                // =================================================
                // AHP RESULT
                // =================================================

                var ahpResult =
                    await _context.Ahpresults
                        .FirstOrDefaultAsync(
                            x =>
                                x.BatchId ==
                                batchId);

                if (ahpResult != null)
                {
                    _context.Ahpresults.Remove(
                        ahpResult);
                }

                // =================================================
                // AHP WEIGHTS
                // =================================================

                var ahpWeights =
                    await _context.AhpcriteriaWeights
                        .Where(
                            x =>
                                x.BatchId ==
                                batchId)
                        .ToListAsync();

                if (ahpWeights.Any())
                {
                    _context.AhpcriteriaWeights
                        .RemoveRange(
                            ahpWeights);
                }

                // =================================================
                // AHP PAIRWISE COMPARISONS
                // =================================================

                var ahpComparisons =
                    await _context.AhppairwiseComparisons
                        .Where(
                            x =>
                                x.BatchId ==
                                batchId)
                        .ToListAsync();

                if (ahpComparisons.Any())
                {
                    _context.AhppairwiseComparisons
                        .RemoveRange(
                            ahpComparisons);
                }

                // =================================================
                // EVALUATION SCORES
                // =================================================

                var evaluationScores =
                    await _context.EvaluationScores
                        .Where(
                            x =>
                                x.BatchId ==
                                batchId)
                        .ToListAsync();

                if (evaluationScores.Any())
                {
                    _context.EvaluationScores.RemoveRange(
                        evaluationScores);
                }

                // =================================================
                // APPROVAL HISTORY
                // =================================================

                var approval =
                    new Approval
                    {
                        BatchId =
                            batchId,

                        SelectedSupplierId =
                            null,

                        ApprovedBy =
                            approvedBy,

                        ApprovalDate =
                            DateTime.UtcNow,

                        Notes =
                            string.IsNullOrWhiteSpace(
                                notes)
                                ? null
                                : notes.Trim(),

                        Status =
                            ApprovalStatus.PendingReevaluation,

                        IsFinal =
                            false
                    };

                _context.Approvals.Add(
                    approval);

                // =================================================
                // KEEP BATCH IN EVALUATING
                // =================================================

                batch.Status =
                    BatchStatus.Evaluating;

                batch.CompletedAt =
                    null;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return approval.ApprovalId;
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        });
    }


    // =========================================================
    // CREATE APPROVAL
    // =========================================================

    private async Task<int> CreateApprovalAsync(
        int batchId,
        int? supplierId,
        int approvedBy,
        string? notes,
        string status,
        bool isFinal)
    {
        // =====================================================
        // BATCH
        // =====================================================

        var batch =
            await _context.EvaluationBatches
                .Include(
                    x =>
                        x.Suppliers)
                .FirstOrDefaultAsync(
                    x =>
                        x.BatchId ==
                        batchId);


        if (batch == null)
        {
            throw new InvalidOperationException(
                "Evaluation Batch không tồn tại.");
        }


        // =====================================================
        // STATUS
        // =====================================================

        if (batch.Status == BatchStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Batch đã bị huỷ và không thể xử lý kết quả.");
        }


        if (batch.Status == BatchStatus.Completed)
        {
            throw new InvalidOperationException(
                "Batch đã hoàn tất và không thể tạo quyết định mới.");
        }


        if (batch.Status != BatchStatus.Evaluating)
        {
            throw new InvalidOperationException(
                "Batch chưa ở trạng thái đánh giá và chưa thể xử lý kết quả.");
        }


        if (
            status != ApprovalStatus.Approved &&
            status != ApprovalStatus.Rejected &&
            status != ApprovalStatus.PendingReevaluation)
        {
            throw new InvalidOperationException(
                "Trạng thái Approval không hợp lệ.");
        }


        // =====================================================
        // APPROVAL / REJECT
        // =====================================================

        if (
            status == ApprovalStatus.Rejected ||
            status == ApprovalStatus.Approved)
        {
            await EnsureTopsisCompleteAsync(
                batchId,
                batch.Suppliers,
                status);
        }


        // =====================================================
        // APPROVED SUPPLIER
        // =====================================================

        if (
            status == ApprovalStatus.Approved &&
            supplierId.HasValue)
        {
            // -------------------------------------------------
            // Supplier thuộc Batch
            // -------------------------------------------------

            var supplierInBatch =
                batch.Suppliers
                    .Any(
                        x =>
                            x.SupplierId ==
                            supplierId.Value);


            if (!supplierInBatch)
            {
                throw new InvalidOperationException(
                    "Nhà cung cấp không thuộc Evaluation Batch này.");
            }


            // -------------------------------------------------
            // Supplier có TOPSIS
            // -------------------------------------------------

            var topsisResult =
                await _context.TopsisResults
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.BatchId ==
                                batchId
                            &&
                            x.SupplierId ==
                                supplierId.Value);


            if (topsisResult == null)
            {
                throw new InvalidOperationException(
                    "Nhà cung cấp không có kết quả TOPSIS trong Batch.");
            }


            if (topsisResult.RankNo <= 0)
            {
                throw new InvalidOperationException(
                    "Kết quả TOPSIS của nhà cung cấp không hợp lệ.");
            }
        }


        // =====================================================
        // CREATE APPROVAL RECORD
        // =====================================================

        var approval =
            new Approval
            {
                BatchId =
                    batchId,

                SelectedSupplierId =
                    supplierId,

                ApprovedBy =
                    approvedBy,

                ApprovalDate =
                    DateTime.UtcNow,

                Notes =
                    string.IsNullOrWhiteSpace(notes)
                        ? null
                        : notes.Trim(),

                Status =
                    status,

                IsFinal =
                    isFinal
            };


        _context.Approvals.Add(
            approval);


        // =====================================================
        // COMPLETE BATCH
        // =====================================================

        if (
            isFinal &&
            status == ApprovalStatus.Approved)
        {
            batch.Status =
                BatchStatus.Completed;

            batch.CompletedAt =
                DateTime.UtcNow;
        }


        await _context.SaveChangesAsync();


        return approval.ApprovalId;
    }


    // =========================================================
    // VALIDATE TOPSIS COMPLETENESS
    // =========================================================

    private async Task EnsureTopsisCompleteAsync(
        int batchId,
        ICollection<Supplier> batchSuppliers,
        string action)
    {
        var supplierIds =
            batchSuppliers
                .Select(
                    x =>
                        x.SupplierId)
                .ToHashSet();


        if (supplierIds.Count < 2)
        {
            throw new InvalidOperationException(
                "Batch phải có ít nhất 2 nhà cung cấp.");
        }


        var topsisSupplierIds =
            await _context.TopsisResults
                .AsNoTracking()
                .Where(
                    x =>
                        x.BatchId ==
                        batchId)
                .Select(
                    x =>
                        x.SupplierId)
                .ToListAsync();


        var topsisSet =
            topsisSupplierIds
                .ToHashSet();


        if (
            topsisSet.Count !=
            supplierIds.Count ||
            !supplierIds.SetEquals(
                topsisSet))
        {
            throw new InvalidOperationException(
                $"Không thể {GetDecisionVerb(action)} " +
                "vì kết quả TOPSIS chưa hoàn tất " +
                "cho toàn bộ nhà cung cấp trong Batch.");
        }


        var invalidRank =
            await _context.TopsisResults
                .AsNoTracking()
                .Where(
                    x =>
                        x.BatchId ==
                            batchId
                        &&
                        x.RankNo <= 0)
                .AnyAsync();


        if (invalidRank)
        {
            throw new InvalidOperationException(
                "Kết quả TOPSIS chứa thứ hạng không hợp lệ.");
        }
    }


    // =========================================================
    // DECISION VERB
    // =========================================================

    private static string GetDecisionVerb(
        string action)
    {
        return action switch
        {
            ApprovalStatus.Approved =>
                "phê duyệt",

            ApprovalStatus.Rejected =>
                "từ chối",

            _ =>
                "xử lý"
        };
    }
}
