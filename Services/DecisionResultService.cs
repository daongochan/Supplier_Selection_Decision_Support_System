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
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
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
                    batch.Status,

                RowVersion =
                    BatchConcurrencyHelper.ToBase64(batch.RowVersion)
            };

        // =====================================================
        // SNAPSHOTS & ROUND HISTORY
        // =====================================================
        var rawSnapshots = await _context.BatchRoundSnapshots
            .AsNoTracking()
            .Include(x => x.CreatedByNavigation)
            .Where(x => x.BatchId == batchId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.SnapshotId)
            .ToListAsync();

        BatchSnapshotPayload? finalSnapshotPayload = null;
        var reevalCount = 0;

        foreach (var snap in rawSnapshots)
        {
            BatchSnapshotPayload? parsed = null;
            try
            {
                parsed = System.Text.Json.JsonSerializer.Deserialize<BatchSnapshotPayload>(
                    snap.SnapshotData,
                    SnapshotJsonOptions);
            }
            catch
            {
                // Ignore malformed legacy JSON
            }

            if (string.Equals(snap.SnapshotType, "Reevaluation", StringComparison.OrdinalIgnoreCase))
            {
                reevalCount++;
            }

            var roundNo = parsed?.RoundNumber > 0
                ? parsed.RoundNumber
                : (string.Equals(snap.SnapshotType, "Reevaluation", StringComparison.OrdinalIgnoreCase)
                    ? reevalCount
                    : reevalCount + 1);

            if (string.Equals(snap.SnapshotType, "FinalApproval", StringComparison.OrdinalIgnoreCase) && parsed != null)
            {
                finalSnapshotPayload = parsed;
            }

            var rankingList = parsed?.TopsisResults?
                .OrderBy(r => r.RankNo)
                .Select(r => new DecisionRankingViewModel
                {
                    SupplierId = r.SupplierId,
                    SupplierCode = r.SupplierCode ?? $"SUP-{r.SupplierId}",
                    SupplierName = r.SupplierName ?? $"Supplier #{r.SupplierId}",
                    DistanceToPIS = r.DistanceToPis,
                    DistanceToNIS = r.DistanceToNis,
                    ClosenessCoefficient = r.ClosenessCoefficient,
                    RankNo = r.RankNo
                })
                .ToList() ?? new List<DecisionRankingViewModel>();

            var topEntry = rankingList.FirstOrDefault();
            model.RoundSnapshots.Add(new RoundSnapshotSummaryViewModel
            {
                SnapshotId = snap.SnapshotId,
                RoundNumber = roundNo,
                SnapshotType = snap.SnapshotType,
                CreatedAt = snap.CreatedAt,
                ActorName = parsed?.ActorName ?? snap.CreatedByNavigation?.FullName,
                Notes = parsed?.Notes,
                TopSupplierCode = topEntry?.SupplierCode,
                TopSupplierName = topEntry?.SupplierName,
                TopClosenessCoefficient = topEntry?.ClosenessCoefficient,
                SelectedSupplierCode = parsed?.SelectedSupplierCode,
                SelectedSupplierName = parsed?.SelectedSupplierName,
                AhpCrValue = parsed?.AhpResult?.Crvalue,
                MethodVersion = parsed?.MethodVersion ?? "AHP-PrincipalEigenvector-v1+TOPSIS-EuclideanL2-v1",
                Ranking = rankingList,
                Weights = parsed?.AhpWeights ?? new List<BatchSnapshotWeightItem>()
            });
        }

        model.RoundSnapshots = model.RoundSnapshots
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.SnapshotId)
            .ToList();

        model.CurrentRoundNumber = string.Equals(batch.Status, BatchStatus.Completed, StringComparison.OrdinalIgnoreCase)
            ? Math.Max(1, reevalCount + 1)
            : reevalCount + 1;

        // =====================================================
        // TOPSIS RANKING (USE FROZEN SNAPSHOT IF COMPLETED)
        // =====================================================

        if (string.Equals(batch.Status, BatchStatus.Completed, StringComparison.OrdinalIgnoreCase)
            && finalSnapshotPayload != null
            && finalSnapshotPayload.TopsisResults.Count > 0
            && finalSnapshotPayload.TopsisResults.All(r => !string.IsNullOrWhiteSpace(r.SupplierCode)))
        {
            model.IsFrozenFromSnapshot = true;
            model.SnapshotMethodVersion = finalSnapshotPayload.MethodVersion;
            model.BatchCode = string.IsNullOrWhiteSpace(finalSnapshotPayload.BatchCode) ? batch.BatchCode : finalSnapshotPayload.BatchCode;
            model.BatchName = string.IsNullOrWhiteSpace(finalSnapshotPayload.BatchName) ? batch.BatchName : finalSnapshotPayload.BatchName;

            foreach (var r in finalSnapshotPayload.TopsisResults.OrderBy(x => x.RankNo))
            {
                model.Ranking.Add(new DecisionRankingViewModel
                {
                    SupplierId = r.SupplierId,
                    SupplierCode = r.SupplierCode ?? string.Empty,
                    SupplierName = r.SupplierName ?? string.Empty,
                    DistanceToPIS = r.DistanceToPis,
                    DistanceToNIS = r.DistanceToNis,
                    ClosenessCoefficient = r.ClosenessCoefficient,
                    RankNo = r.RankNo
                });
            }

            model.SensitivityAnalysis = finalSnapshotPayload.SensitivityAnalysis;
        }
        else
        {
            var topsisWs = await _topsisService.GetWorkspaceAsync(batchId);
            if (topsisWs != null && topsisWs.Ranking.Count > 0)
            {
                foreach (var result in topsisWs.Ranking.OrderBy(x => x.RankNo))
                {
                    model.Ranking.Add(new DecisionRankingViewModel
                    {
                        SupplierId = result.SupplierId,
                        SupplierCode = result.SupplierCode,
                        SupplierName = result.SupplierName,
                        DistanceToPIS = result.DistanceToPIS,
                        DistanceToNIS = result.DistanceToNIS,
                        ClosenessCoefficient = result.ClosenessCoefficient,
                        RankNo = result.RankNo
                    });
                }

                model.SensitivityAnalysis = topsisWs.SensitivityAnalysis;
            }
        }


        // =====================================================
        // ALL APPROVALS & IMMUTABLE DECISION HISTORY
        // =====================================================

        var allApprovalsQuery = _context.Approvals
            .AsNoTracking()
            .Where(x => x.BatchId == batchId);

        if (_context.Database.IsRelational())
        {
            allApprovalsQuery = allApprovalsQuery
                .Include(x => x.ApprovedByNavigation)
                .Include(x => x.SelectedSupplier);
        }

        var allApprovals = await allApprovalsQuery
            .OrderBy(x => x.ApprovalDate)
            .ThenBy(x => x.ApprovalId)
            .ToListAsync();

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

        var latestScoreEvaluatedAt =
            await _context.EvaluationScores
                .AsNoTracking()
                .Where(x => x.BatchId == batchId)
                .Select(x => (DateTime?)x.EvaluatedAt)
                .OrderByDescending(x => x)
                .FirstOrDefaultAsync();

        var latestAhpCalculatedAt =
            await _context.Ahpresults
                .AsNoTracking()
                .Where(x => x.BatchId == batchId)
                .Select(x => (DateTime?)x.CalculatedAt)
                .OrderByDescending(x => x)
                .FirstOrDefaultAsync();

        var currentRoundCounter = 1;
        for (var idx = 0; idx < allApprovals.Count; idx++)
        {
            var app = allApprovals[idx];
            var isLatest = idx == allApprovals.Count - 1;
            var isSuperseded = !isLatest;

            if (isLatest && !app.IsFinal)
            {
                if (string.Equals(app.Status, ApprovalStatus.PendingReevaluation, StringComparison.OrdinalIgnoreCase))
                {
                    if (latestTopsisCalculatedAt.HasValue && latestTopsisCalculatedAt.Value > app.ApprovalDate)
                    {
                        isSuperseded = true;
                    }
                }
                else if (string.Equals(app.Status, ApprovalStatus.Rejected, StringComparison.OrdinalIgnoreCase))
                {
                    var mutatedSinceReject =
                        (latestTopsisCalculatedAt.HasValue && latestTopsisCalculatedAt.Value > app.ApprovalDate)
                        || (latestScoreEvaluatedAt.HasValue && latestScoreEvaluatedAt.Value > app.ApprovalDate)
                        || (latestAhpCalculatedAt.HasValue && latestAhpCalculatedAt.Value > app.ApprovalDate);
                    if (mutatedSinceReject)
                    {
                        isSuperseded = true;
                    }
                }
            }

            var frozenSelectedCode = app.IsFinal && finalSnapshotPayload?.SelectedSupplierCode != null
                ? finalSnapshotPayload.SelectedSupplierCode
                : app.SelectedSupplier?.SupplierCode;
            var frozenSelectedName = app.IsFinal && finalSnapshotPayload?.SelectedSupplierName != null
                ? finalSnapshotPayload.SelectedSupplierName
                : app.SelectedSupplier?.SupplierName;

            model.DecisionHistory.Add(new DecisionHistoryItemViewModel
            {
                ApprovalId = app.ApprovalId,
                RoundNumber = currentRoundCounter,
                Status = app.Status,
                IsFinal = app.IsFinal,
                IsSuperseded = isSuperseded,
                SelectedSupplierId = app.SelectedSupplierId,
                SelectedSupplierCode = frozenSelectedCode,
                SelectedSupplierName = frozenSelectedName,
                ApprovedByName = app.IsFinal && finalSnapshotPayload?.ActorName != null
                    ? finalSnapshotPayload.ActorName
                    : app.ApprovedByNavigation?.FullName,
                ApprovalDate = app.ApprovalDate,
                Notes = app.Notes
            });

            if (string.Equals(app.Status, ApprovalStatus.PendingReevaluation, StringComparison.OrdinalIgnoreCase))
            {
                currentRoundCounter++;
            }
        }

        model.DecisionHistory = model.DecisionHistory
            .OrderByDescending(x => x.ApprovalDate)
            .ThenByDescending(x => x.ApprovalId)
            .ToList();

        var activeDecision = model.DecisionHistory.FirstOrDefault(x => !x.IsSuperseded);
        if (activeDecision != null)
        {
            model.HasApproval = true;
            model.SelectedSupplierId = activeDecision.SelectedSupplierId;
            model.SelectedSupplierCode = activeDecision.SelectedSupplierCode;
            model.SelectedSupplierName = activeDecision.SelectedSupplierName;
            model.ApprovalStatus = activeDecision.Status;
            model.ApprovalNotes = activeDecision.Notes;
            model.IsFinal = activeDecision.IsFinal;
            model.ApprovalDate = activeDecision.ApprovalDate;
            model.ApprovedByName = activeDecision.ApprovedByName;
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
        string? notes,
        string? expectedRowVersion = null)
    {
        _logger.LogInformation(
            "Approving evaluation result for Batch ID {BatchId} by User {UserId}, selected Supplier ID {SupplierId}.",
            batchId,
            approvedBy,
            supplierId);

        return await CreateApprovalAsync(
            batchId,
            supplierId,
            approvedBy,
            notes,
            ApprovalStatus.Approved,
            true,
            expectedRowVersion);
    }


    // =========================================================
    // REJECT
    // =========================================================

    public async Task<int> RejectAsync(
        int batchId,
        string? notes,
        int approvedBy,
        string? expectedRowVersion = null)
    {
        _logger.LogInformation(
            "Rejecting evaluation result for Batch ID {BatchId} by User {UserId} (Notes: {Notes}).",
            batchId,
            approvedBy,
            notes);

        return await CreateApprovalAsync(
            batchId,
            null,
            approvedBy,
            notes,
            ApprovalStatus.Rejected,
            false,
            expectedRowVersion);
    }


    // =========================================================
    // RE-EVALUATION
    // =========================================================

    public async Task<int> RequestReevaluationAsync(
        int batchId,
        string? notes,
        int approvedBy,
        string? expectedRowVersion = null)
    {
        _logger.LogInformation(
            "Requesting re-evaluation for Batch ID {BatchId} by User {UserId}.",
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
                var batch =
                    await _context.EvaluationBatches
                        .Include(x => x.Suppliers)
                        .Include(x => x.Criteria)
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

                await BatchConcurrencyHelper.BumpAndVerifyBatchVersionAsync(
                    _context,
                    batch,
                    expectedRowVersion,
                    newStatus: BatchStatus.Evaluating,
                    clearCompletedAt: true);

                var topsisResults =
                    await _context.TopsisResults
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                var idealSolutions =
                    await _context.TopsisIdealSolutions
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                var ahpResult =
                    await _context.Ahpresults
                        .FirstOrDefaultAsync(x => x.BatchId == batchId);

                var ahpWeights =
                    await _context.AhpcriteriaWeights
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                var ahpComparisons =
                    await _context.AhppairwiseComparisons
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                var evaluationScores =
                    await _context.EvaluationScores
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                var snapshotPayload = await BuildCompleteSnapshotPayloadAsync(
                    batch,
                    "Reevaluation",
                    selectedSupplierId: null,
                    actorUserId: approvedBy,
                    notes: notes,
                    evaluationScores: evaluationScores,
                    ahpComparisons: ahpComparisons,
                    ahpResult: ahpResult,
                    ahpWeights: ahpWeights,
                    idealSolutions: idealSolutions,
                    topsisResults: topsisResults);

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

                var approval =
                    new Approval
                    {
                        BatchId = batchId,
                        SelectedSupplierId = null,
                        ApprovedBy = approvedBy,
                        ApprovalDate = DateTime.UtcNow,
                        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                        Status = ApprovalStatus.PendingReevaluation,
                        IsFinal = false
                    };

                _context.Approvals.Add(approval);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return approval.ApprovalId;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Concurrency conflict when requesting re-evaluation for Batch ID {BatchId}.", batchId);
                throw new BusinessRuleException(BatchConcurrencyHelper.ConcurrencyConflictMessage);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(
                    ex,
                    "Error requesting re-evaluation for Batch ID {BatchId}.",
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
        bool isFinal,
        string? expectedRowVersion = null)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var batch =
                    await _context.EvaluationBatches
                        .Include(x => x.Suppliers)
                        .Include(x => x.Criteria)
                        .FirstOrDefaultAsync(x => x.BatchId == batchId);

                if (batch == null)
                {
                    throw new BusinessRuleException(
                        "Evaluation Batch không tồn tại.");
                }

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

                if (
                    status == ApprovalStatus.Rejected ||
                    status == ApprovalStatus.Approved)
                {
                    await EnsureTopsisCompleteAsync(
                        batchId,
                        batch.Suppliers,
                        status);
                }

                if (
                    status == ApprovalStatus.Approved &&
                    supplierId.HasValue)
                {
                    var supplierInBatch =
                        batch.Suppliers
                            .Any(x => x.SupplierId == supplierId.Value);

                    if (!supplierInBatch)
                    {
                        throw new BusinessRuleException(
                            "Nhà cung cấp không thuộc Evaluation Batch này.");
                    }

                    var topsisResult =
                        await _context.TopsisResults
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x =>
                                    x.BatchId == batchId
                                    && x.SupplierId == supplierId.Value);

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

                var nowUtc = DateTime.UtcNow;

                if (isFinal && status == ApprovalStatus.Approved)
                {
                    await BatchConcurrencyHelper.BumpAndVerifyBatchVersionAsync(
                        _context,
                        batch,
                        expectedRowVersion,
                        newStatus: BatchStatus.Completed,
                        completedAt: nowUtc);

                    var evaluationScores = await _context.EvaluationScores
                        .AsNoTracking()
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                    var ahpComparisons = await _context.AhppairwiseComparisons
                        .AsNoTracking()
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                    var ahpResult = await _context.Ahpresults
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.BatchId == batchId);

                    var ahpWeights = await _context.AhpcriteriaWeights
                        .AsNoTracking()
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                    var idealSolutions = await _context.TopsisIdealSolutions
                        .AsNoTracking()
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                    var topsisResults = await _context.TopsisResults
                        .AsNoTracking()
                        .Where(x => x.BatchId == batchId)
                        .ToListAsync();

                    var finalSnapshotPayload = await BuildCompleteSnapshotPayloadAsync(
                        batch,
                        "FinalApproval",
                        selectedSupplierId: supplierId,
                        actorUserId: approvedBy,
                        notes: notes,
                        evaluationScores: evaluationScores,
                        ahpComparisons: ahpComparisons,
                        ahpResult: ahpResult,
                        ahpWeights: ahpWeights,
                        idealSolutions: idealSolutions,
                        topsisResults: topsisResults);

                    _context.BatchRoundSnapshots.Add(new BatchRoundSnapshot
                    {
                        BatchId = batchId,
                        SnapshotType = "FinalApproval",
                        SnapshotData = System.Text.Json.JsonSerializer.Serialize(finalSnapshotPayload, SnapshotJsonOptions),
                        CreatedAt = nowUtc,
                        CreatedBy = approvedBy
                    });
                }
                else
                {
                    await BatchConcurrencyHelper.BumpAndVerifyBatchVersionAsync(
                        _context,
                        batch,
                        expectedRowVersion);
                }

                var approval =
                    new Approval
                    {
                        BatchId = batchId,
                        SelectedSupplierId = supplierId,
                        ApprovedBy = approvedBy,
                        ApprovalDate = nowUtc,
                        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                        Status = status,
                        IsFinal = isFinal
                    };

                _context.Approvals.Add(approval);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Recorded approval decision ID {ApprovalId} for Batch {BatchId} with status {Status}.",
                    approval.ApprovalId,
                    batchId,
                    status);

                return approval.ApprovalId;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Concurrency conflict when updating approval decision for Batch {BatchId}.", batchId);
                throw new BusinessRuleException(BatchConcurrencyHelper.ConcurrencyConflictMessage);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    private async Task<BatchSnapshotPayload> BuildCompleteSnapshotPayloadAsync(
        EvaluationBatch batch,
        string snapshotType,
        int? selectedSupplierId,
        int actorUserId,
        string? notes,
        List<EvaluationScore> evaluationScores,
        List<AhppairwiseComparison> ahpComparisons,
        Ahpresult? ahpResult,
        List<AhpcriteriaWeight> ahpWeights,
        List<TopsisIdealSolution> idealSolutions,
        List<TopsisResult> topsisResults)
    {
        var supplierMap = batch.Suppliers
            .ToDictionary(s => s.SupplierId, s => s);
        var criteriaMap = batch.Criteria
            .ToDictionary(c => c.CriteriaId, c => c);

        var actorName = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == actorUserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        var groupInfo = batch.Group != null
            ? (batch.Group.GroupCode, batch.Group.GroupName)
            : await _context.ProductGroups
                .AsNoTracking()
                .Where(g => g.GroupId == batch.GroupId)
                .Select(g => ValueTuple.Create(g.GroupCode, g.GroupName))
                .FirstOrDefaultAsync();

        var existingReevalCount = await _context.BatchRoundSnapshots
            .AsNoTracking()
            .CountAsync(x => x.BatchId == batch.BatchId && x.SnapshotType == "Reevaluation");

        var topsisWs = await _topsisService.GetWorkspaceAsync(batch.BatchId);

        Supplier? selectedSupplier = null;
        if (selectedSupplierId.HasValue)
        {
            supplierMap.TryGetValue(selectedSupplierId.Value, out selectedSupplier);
        }

        var nowUtc = DateTime.UtcNow;

        return new BatchSnapshotPayload
        {
            BatchId = batch.BatchId,
            BatchCode = batch.BatchCode,
            BatchName = batch.BatchName,
            GroupCode = groupInfo.Item1,
            GroupName = groupInfo.Item2,
            RoundNumber = existingReevalCount + 1,
            SnapshotType = snapshotType,
            MethodVersion = "AHP-PrincipalEigenvector-v1+TOPSIS-EuclideanL2-v1",
            SelectedSupplierId = selectedSupplierId,
            SelectedSupplierCode = selectedSupplier?.SupplierCode,
            SelectedSupplierName = selectedSupplier?.SupplierName,
            ApprovedBy = string.Equals(snapshotType, "FinalApproval", StringComparison.OrdinalIgnoreCase) ? actorUserId : null,
            RequestedBy = string.Equals(snapshotType, "Reevaluation", StringComparison.OrdinalIgnoreCase) ? actorUserId : null,
            ActorName = actorName,
            ApprovedAt = string.Equals(snapshotType, "FinalApproval", StringComparison.OrdinalIgnoreCase) ? nowUtc : null,
            RequestedAt = string.Equals(snapshotType, "Reevaluation", StringComparison.OrdinalIgnoreCase) ? nowUtc : null,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Suppliers = batch.Suppliers
                .OrderBy(s => s.SupplierCode)
                .Select(s => new BatchSnapshotSupplierItem
                {
                    SupplierId = s.SupplierId,
                    SupplierCode = s.SupplierCode,
                    SupplierName = s.SupplierName
                })
                .ToList(),
            Criteria = batch.Criteria
                .OrderBy(c => c.CriteriaCode)
                .Select(c => new BatchSnapshotCriterionItem
                {
                    CriteriaId = c.CriteriaId,
                    CriteriaCode = c.CriteriaCode,
                    CriteriaName = c.CriteriaName,
                    CriteriaType = c.CriteriaType,
                    Unit = c.Unit
                })
                .ToList(),
            EvaluationScores = evaluationScores
                .OrderBy(s => supplierMap.TryGetValue(s.SupplierId, out var sup) ? sup.SupplierCode : string.Empty)
                .ThenBy(s => criteriaMap.TryGetValue(s.CriteriaId, out var crit) ? crit.CriteriaCode : string.Empty)
                .Select(s =>
                {
                    supplierMap.TryGetValue(s.SupplierId, out var sup);
                    criteriaMap.TryGetValue(s.CriteriaId, out var crit);
                    return new BatchSnapshotScoreItem
                    {
                        SupplierId = s.SupplierId,
                        SupplierCode = sup?.SupplierCode,
                        SupplierName = sup?.SupplierName,
                        CriteriaId = s.CriteriaId,
                        CriteriaCode = crit?.CriteriaCode,
                        CriteriaName = crit?.CriteriaName,
                        ScoreValue = s.ScoreValue
                    };
                })
                .ToList(),
            AhpComparisons = ahpComparisons
                .OrderBy(c => criteriaMap.TryGetValue(c.CriteriaRowId, out var r) ? r.CriteriaCode : string.Empty)
                .ThenBy(c => criteriaMap.TryGetValue(c.CriteriaColId, out var col) ? col.CriteriaCode : string.Empty)
                .Select(c =>
                {
                    criteriaMap.TryGetValue(c.CriteriaRowId, out var r);
                    criteriaMap.TryGetValue(c.CriteriaColId, out var col);
                    return new BatchSnapshotComparisonItem
                    {
                        CriteriaRowId = c.CriteriaRowId,
                        CriteriaRowCode = r?.CriteriaCode,
                        CriteriaColId = c.CriteriaColId,
                        CriteriaColCode = col?.CriteriaCode,
                        ComparisonValue = c.ComparisonValue
                    };
                })
                .ToList(),
            AhpResult = ahpResult == null
                ? null
                : new BatchSnapshotAhpResultItem
                {
                    LambdaMax = ahpResult.LambdaMax,
                    Civalue = ahpResult.Civalue,
                    Rivalue = ahpResult.Rivalue,
                    Crvalue = ahpResult.Crvalue,
                    IsConsistent = ahpResult.IsConsistent,
                    N = batch.Criteria.Count
                },
            AhpWeights = ahpWeights
                .OrderBy(w => criteriaMap.TryGetValue(w.CriteriaId, out var crit) ? crit.CriteriaCode : string.Empty)
                .Select(w =>
                {
                    criteriaMap.TryGetValue(w.CriteriaId, out var crit);
                    return new BatchSnapshotWeightItem
                    {
                        CriteriaId = w.CriteriaId,
                        CriteriaCode = crit?.CriteriaCode,
                        CriteriaName = crit?.CriteriaName,
                        WeightValue = w.WeightValue
                    };
                })
                .ToList(),
            TopsisIdeals = idealSolutions
                .OrderBy(i => criteriaMap.TryGetValue(i.CriteriaId, out var crit) ? crit.CriteriaCode : string.Empty)
                .Select(i =>
                {
                    criteriaMap.TryGetValue(i.CriteriaId, out var crit);
                    return new BatchSnapshotIdealItem
                    {
                        CriteriaId = i.CriteriaId,
                        CriteriaCode = crit?.CriteriaCode,
                        CriteriaName = crit?.CriteriaName,
                        CriteriaType = crit?.CriteriaType,
                        Unit = crit?.Unit,
                        Pisvalue = i.Pisvalue,
                        Nisvalue = i.Nisvalue
                    };
                })
                .ToList(),
            TopsisResults = topsisResults
                .OrderBy(r => r.RankNo)
                .Select(r =>
                {
                    supplierMap.TryGetValue(r.SupplierId, out var sup);
                    return new BatchSnapshotTopsisResultItem
                    {
                        SupplierId = r.SupplierId,
                        SupplierCode = sup?.SupplierCode,
                        SupplierName = sup?.SupplierName,
                        DistanceToPis = r.DistanceToPis,
                        DistanceToNis = r.DistanceToNis,
                        ClosenessCoefficient = r.ClosenessCoefficient,
                        RankNo = r.RankNo
                    };
                })
                .ToList(),
            NormalizedMatrix = topsisWs?.NormalizedMatrix ?? new List<TOPSISMatrixRowViewModel>(),
            WeightedMatrix = topsisWs?.WeightedMatrix ?? new List<TOPSISMatrixRowViewModel>(),
            SensitivityAnalysis = topsisWs?.SensitivityAnalysis
        };
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
