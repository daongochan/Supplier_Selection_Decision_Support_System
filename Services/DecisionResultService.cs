using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class DecisionResultService
{
    private readonly ApplicationDbContext _context;
    private readonly TOPSISService _topsisService;
    private readonly ILogger<DecisionResultService> _logger;

    private static readonly System.Text.Json.JsonSerializerOptions SnapshotJsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public DecisionResultService(
        ApplicationDbContext context,
        TOPSISService topsisService,
        ILogger<DecisionResultService>? logger = null)
    {
        _context = context;
        _topsisService = topsisService;
        _logger = logger ?? NullLogger<DecisionResultService>.Instance;
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
        _logger.LogInformation(
            "Phê duyệt kết quả đánh giá cho đợt ID {BatchId} bởi User {UserId}, chọn nhà cung cấp ID {SupplierId}.",
            batchId,
            approvedBy,
            supplierId);

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
        _logger.LogInformation(
            "Từ chối kết quả đánh giá cho đợt ID {BatchId} bởi User {UserId} (Ghi chú: {Notes}).",
            batchId,
            approvedBy,
            notes);

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
        _logger.LogInformation(
            "Yêu cầu đánh giá lại cho đợt ID {BatchId} bởi User {UserId}.",
            batchId,
            approvedBy);

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
                    throw new BusinessRuleException(
                        "Evaluation Batch không tồn tại.");
                }

                if (batch.Status == BatchStatus.Cancelled)
                {
                    throw new BusinessRuleException(
                        "Batch đã bị huỷ và không thể yêu cầu đánh giá lại.");
                }

                if (batch.Status == BatchStatus.Completed)
                {
                    throw new BusinessRuleException(
                        "Batch đã hoàn tất và không thể yêu cầu đánh giá lại.");
                }

                if (batch.Status != BatchStatus.Evaluating)
                {
                    throw new BusinessRuleException(
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

                // =================================================
                // AHP RESULT
                // =================================================

                var ahpResult =
                    await _context.Ahpresults
                        .FirstOrDefaultAsync(
                            x =>
                                x.BatchId ==
                                batchId);

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

                // =================================================
                // SAVE BATCH ROUND SNAPSHOT (PRESERVE OLD ROUND DATA)
                // =================================================

                var snapshotPayload = new
                {
                    BatchId = batchId,
                    BatchCode = batch.BatchCode,
                    BatchName = batch.BatchName,
                    RequestedBy = approvedBy,
                    RequestedAt = DateTime.UtcNow,
                    Notes = notes,
                    TopsisResults = topsisResults.Select(r => new { r.SupplierId, r.ClosenessCoefficient, r.RankNo }).ToList(),
                    TopsisIdeals = idealSolutions.Select(i => new { i.CriteriaId, i.Pisvalue, i.Nisvalue }).ToList(),
                    AhpResult = ahpResult == null ? null : new { ahpResult.LambdaMax, ahpResult.Civalue, ahpResult.Crvalue, ahpResult.IsConsistent },
                    AhpWeights = ahpWeights.Select(w => new { w.CriteriaId, w.WeightValue }).ToList(),
                    AhpComparisons = ahpComparisons.Select(c => new { c.CriteriaRowId, c.CriteriaColId, c.ComparisonValue }).ToList(),
                    EvaluationScores = evaluationScores.Select(s => new { s.SupplierId, s.CriteriaId, s.ScoreValue }).ToList()
                };

                _context.BatchRoundSnapshots.Add(new BatchRoundSnapshot
                {
                    BatchId = batchId,
                    SnapshotType = "Reevaluation",
                    SnapshotData = System.Text.Json.JsonSerializer.Serialize(snapshotPayload, SnapshotJsonOptions),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = approvedBy
                });

                if (topsisResults.Any())
                {
                    _context.TopsisResults.RemoveRange(topsisResults);
                }

                if (idealSolutions.Any())
                {
                    _context.TopsisIdealSolutions.RemoveRange(idealSolutions);
                }

                if (ahpResult != null)
                {
                    _context.Ahpresults.Remove(ahpResult);
                }

                if (ahpWeights.Any())
                {
                    _context.AhpcriteriaWeights.RemoveRange(ahpWeights);
                }

                if (ahpComparisons.Any())
                {
                    _context.AhppairwiseComparisons.RemoveRange(ahpComparisons);
                }

                if (evaluationScores.Any())
                {
                    _context.EvaluationScores.RemoveRange(evaluationScores);
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
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Xung đột đồng thời khi yêu cầu đánh giá lại cho đợt ID {BatchId}.", batchId);
                throw new BusinessRuleException("Dữ liệu đợt đánh giá vừa được người khác thay đổi, vui lòng tải lại trang.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(
                    ex,
                    "Lỗi khi yêu cầu đánh giá lại cho đợt ID {BatchId}.",
                    batchId);
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
            throw new BusinessRuleException(
                "Evaluation Batch không tồn tại.");
        }


        // =====================================================
        // STATUS
        // =====================================================

        if (batch.Status == BatchStatus.Cancelled)
        {
            throw new BusinessRuleException(
                "Batch đã bị huỷ và không thể xử lý kết quả.");
        }


        if (batch.Status == BatchStatus.Completed)
        {
            throw new BusinessRuleException(
                "Batch đã hoàn tất và không thể tạo quyết định mới.");
        }


        if (batch.Status != BatchStatus.Evaluating)
        {
            throw new BusinessRuleException(
                "Batch chưa ở trạng thái đánh giá và chưa thể xử lý kết quả.");
        }


        if (
            status != ApprovalStatus.Approved &&
            status != ApprovalStatus.Rejected &&
            status != ApprovalStatus.PendingReevaluation)
        {
            throw new BusinessRuleException(
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
                throw new BusinessRuleException(
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
                throw new BusinessRuleException(
                    "Nhà cung cấp không có kết quả TOPSIS trong Batch.");
            }


            if (topsisResult.RankNo <= 0)
            {
                throw new BusinessRuleException(
                    "Kết quả TOPSIS của nhà cung cấp không hợp lệ.");
            }

            if (topsisResult.RankNo != 1 && string.IsNullOrWhiteSpace(notes))
            {
                throw new BusinessRuleException(
                    $"Nhà cung cấp được chọn xếp hạng #{topsisResult.RankNo} (không phải Hạng 1). Bắt buộc phải nhập lý do giải trình phê duyệt.");
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

            var finalTopsisResults = await _context.TopsisResults
                .Where(x => x.BatchId == batchId)
                .Select(r => new { r.SupplierId, r.ClosenessCoefficient, r.RankNo })
                .ToListAsync();

            var finalWeights = await _context.AhpcriteriaWeights
                .Where(x => x.BatchId == batchId)
                .Select(w => new { w.CriteriaId, w.WeightValue })
                .ToListAsync();

            var finalSnapshotPayload = new
            {
                BatchId = batchId,
                BatchCode = batch.BatchCode,
                BatchName = batch.BatchName,
                SelectedSupplierId = supplierId,
                ApprovedBy = approvedBy,
                ApprovedAt = DateTime.UtcNow,
                Notes = notes,
                TopsisResults = finalTopsisResults,
                AhpWeights = finalWeights
            };

            _context.BatchRoundSnapshots.Add(new BatchRoundSnapshot
            {
                BatchId = batchId,
                SnapshotType = "FinalApproval",
                SnapshotData = System.Text.Json.JsonSerializer.Serialize(finalSnapshotPayload, SnapshotJsonOptions),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = approvedBy
            });
        }


        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Xung đột đồng thời khi cập nhật quyết định phê duyệt cho đợt {BatchId}.", batchId);
            throw new BusinessRuleException("Dữ liệu đợt đánh giá vừa được người khác thay đổi, vui lòng tải lại trang.");
        }

        _logger.LogInformation(
            "Đã ghi nhận quyết định phê duyệt ID {ApprovalId} cho đợt {BatchId} với trạng thái {Status}.",
            approval.ApprovalId,
            batchId,
            status);

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
            throw new BusinessRuleException(
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
            throw new BusinessRuleException(
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
            throw new BusinessRuleException(
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
