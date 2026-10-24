using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Domain.Rules;

namespace SupplierSelectionDSS.Services;

public class EvaluationBatchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EvaluationBatchService> _logger;

    public EvaluationBatchService(
        ApplicationDbContext context,
        ILogger<EvaluationBatchService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<EvaluationBatchService>.Instance;
    }


    // =========================================================
    // LIST
    // =========================================================

    public async Task<PagedResult<EvaluationBatchListItemViewModel>> SearchAsync(
        string? keyword,
        string? status,
        int? groupId = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        keyword = keyword?.Trim();

        var query = _context.EvaluationBatches
            .AsNoTracking()
            .AsQueryable();

        // =========================================================
        // SEARCH
        // =========================================================

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.BatchCode.Contains(keyword) ||
                x.BatchName.Contains(keyword));
        }

        // =========================================================
        // STATUS
        // =========================================================

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(x => x.Status == status);
        }

        // =========================================================
        // PRODUCT GROUP
        // =========================================================

        if (groupId.HasValue && groupId.Value > 0)
        {
            query = query.Where(x => x.GroupId == groupId.Value);
        }

        // =========================================================
        // RESULT WITH PAGINATION
        // =========================================================

        var projectedQuery = query.OrderByDescending(x => x.CreatedAt)
            .Select(batch => new EvaluationBatchListItemViewModel
            {
                BatchId = batch.BatchId,
                BatchCode = batch.BatchCode,
                BatchName = batch.BatchName,
                GroupCode = batch.Group.GroupCode,
                GroupName = batch.Group.GroupName,
                Status = batch.Status,
                CreatedAt = batch.CreatedAt,
                StartDate = batch.StartDate,
                EndDate = batch.EndDate,
                SupplierCount = batch.Suppliers.Count,
                CriteriaCount = batch.Criteria.Count
            });

        return await PagedResult<EvaluationBatchListItemViewModel>
            .CreateAsync(projectedQuery, pageNumber, pageSize);
    }

    public async Task<(int Total, int Evaluating, int Completed, int Draft)> GetKpiSummaryAsync()
    {
        var total = await _context.EvaluationBatches.CountAsync();
        var evaluating = await _context.EvaluationBatches.CountAsync(x => x.Status == BatchStatus.Evaluating);
        var completed = await _context.EvaluationBatches.CountAsync(x => x.Status == BatchStatus.Completed);
        var draft = await _context.EvaluationBatches.CountAsync(x => x.Status == BatchStatus.Draft);
        return (total, evaluating, completed, draft);
    }


    // =========================================================
    // BATCH CODE
    // =========================================================

    public async Task<bool> BatchCodeExistsAsync(
        string batchCode,
        int? excludeBatchId = null)
    {
        var query =
            _context.EvaluationBatches
                .Where(
                    x =>
                        x.BatchCode ==
                        batchCode);


        if (excludeBatchId.HasValue)
        {
            query =
                query.Where(
                    x =>
                        x.BatchId !=
                        excludeBatchId.Value);
        }


        return await query.AnyAsync();
    }


    // =========================================================
    // GET PRODUCT GROUPS
    // =========================================================

    public async Task<List<ProductGroup>>
        GetActiveProductGroupsAsync()
    {
        return await _context.ProductGroups
            .AsNoTracking()
            .Where(
                x =>
                    x.Status ==
                    EntityStatus.Active)
            .OrderBy(
                x =>
                    x.GroupCode)
            .ToListAsync();
    }


    // =========================================================
    // GET SUPPLIERS BY GROUP
    // =========================================================

    public async Task<List<BatchSupplierOptionViewModel>>
        GetSuppliersByGroupAsync(
            int groupId)
    {
        return await _context.ProductGroups
            .AsNoTracking()
            .Where(
                x =>
                    x.GroupId ==
                    groupId &&
                    x.Status ==
                    EntityStatus.Active)
            .SelectMany(
                x =>
                    x.Suppliers)
            .Where(
                x =>
                    x.Status ==
                    EntityStatus.Active)
            .OrderBy(
                x =>
                    x.SupplierCode)
            .Select(
                x =>
                    new BatchSupplierOptionViewModel
                    {
                        SupplierId =
                            x.SupplierId,

                        SupplierCode =
                            x.SupplierCode,

                        SupplierName =
                            x.SupplierName
                    })
            .ToListAsync();
    }


    // =========================================================
    // GET ACTIVE CRITERIA
    // =========================================================

    public async Task<List<BatchCriteriaOptionViewModel>>
        GetActiveCriteriaAsync()
    {
        return await _context.Criteria
            .AsNoTracking()
            .Where(
                x =>
                    x.Status ==
                    EntityStatus.Active)
            .OrderBy(
                x =>
                    x.CriteriaCode)
            .Select(
                x =>
                    new BatchCriteriaOptionViewModel
                    {
                        CriteriaId =
                            x.CriteriaId,

                        CriteriaCode =
                            x.CriteriaCode,

                        CriteriaName =
                            x.CriteriaName,

                        CriteriaType =
                            x.CriteriaType,

                        Unit =
                            x.Unit
                    })
            .ToListAsync();
    }


    // =========================================================
    // CREATE BATCH
    // =========================================================

    public async Task<int> CreateAsync(
        EvaluationBatchCreateViewModel model,
        int createdBy)
    {
        var group =
            await _context.ProductGroups
                .Include(
                    x =>
                        x.Suppliers)
                .FirstOrDefaultAsync(
                    x =>
                        x.GroupId ==
                        model.GroupId &&
                        x.Status ==
                        EntityStatus.Active);


        if (group == null)
        {
            throw new BusinessRuleException(
                "Product Group không tồn tại hoặc không hoạt động.");
        }


        var eligibleSupplierIds =
            group.Suppliers
                .Where(
                    x =>
                        x.Status ==
                        EntityStatus.Active)
                .Select(
                    x =>
                        x.SupplierId)
                .ToHashSet();


        var selectedSupplierIds =
            model.SelectedSupplierIds
                .Distinct()
                .ToList();


        if (selectedSupplierIds.Count < 2)
        {
            throw new BusinessRuleException(
                "Phải chọn ít nhất 2 nhà cung cấp.");
        }


        if (
            selectedSupplierIds.Any(
                x =>
                    !eligibleSupplierIds.Contains(x)))
        {
            throw new BusinessRuleException(
                "Có nhà cung cấp không thuộc Product Group đã chọn.");
        }


        var selectedCriteriaIds =
            model.SelectedCriteriaIds
                .Distinct()
                .ToList();


        if (selectedCriteriaIds.Count < 2)
        {
            throw new BusinessRuleException(
                "Phải chọn ít nhất 2 tiêu chí.");
        }


        var activeCriteriaIds =
            await _context.Criteria
                .Where(
                    x =>
                        x.Status ==
                        EntityStatus.Active
                        &&
                        selectedCriteriaIds.Contains(
                            x.CriteriaId))
                .Select(
                    x =>
                        x.CriteriaId)
                .ToListAsync();


        if (
            activeCriteriaIds.Count !=
            selectedCriteriaIds.Count)
        {
            throw new BusinessRuleException(
                "Danh sách tiêu chí không hợp lệ.");
        }


        if (
            model.StartDate.HasValue &&
            model.EndDate.HasValue &&
            model.EndDate < model.StartDate)
        {
            throw new BusinessRuleException(
                "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        var effectiveStartDate =
            model.StartDate ??
            DateOnly.FromDateTime(DateTime.Today);

        if (
            model.EndDate.HasValue &&
            model.EndDate.Value < effectiveStartDate)
        {
            throw new BusinessRuleException(
                "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu đánh giá.");
        }


        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var batch =
                    new EvaluationBatch
                    {
                        BatchCode =
                            model.BatchCode.Trim(),

                        BatchName =
                            model.BatchName.Trim(),

                        Description =
                            model.Description?.Trim(),

                        GroupId =
                            model.GroupId!.Value,

                        StartDate =
                            model.StartDate,

                        EndDate =
                            model.EndDate,

                        Status =
                            BatchStatus.Draft,

                        CreatedBy =
                            createdBy,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                if (!_context.Database.IsRelational())
                {
                    BatchConcurrencyHelper.EnsureInitializedForInMemory(batch);
                }

                _context.EvaluationBatches.Add(
                    batch);

                await _context.SaveChangesAsync();

                var selectedSuppliers =
                    await _context.Suppliers
                        .Where(x =>
                            selectedSupplierIds.Contains(x.SupplierId))
                        .ToListAsync();

                if (selectedSuppliers.Count != selectedSupplierIds.Count)
                {
                    throw new BusinessRuleException(
                        "Danh sách nhà cung cấp không hợp lệ.");
                }

                foreach (var supplier in selectedSuppliers)
                {
                    batch.Suppliers.Add(supplier);
                }

                var criteria =
                    await _context.Criteria
                        .Where(
                            x =>
                                selectedCriteriaIds.Contains(
                                    x.CriteriaId))
                        .ToListAsync();

                foreach (
                    var criterion
                    in criteria)
                {
                    batch.Criteria.Add(
                        criterion);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Tạo đợt đánh giá thành công: ID {BatchId}, Mã {BatchCode}, Tên {BatchName}.",
                    batch.BatchId,
                    batch.BatchCode,
                    batch.BatchName);

                return batch.BatchId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(
                    ex,
                    "Lỗi khi tạo đợt đánh giá mới với mã {BatchCode}.",
                    model.BatchCode);
                throw;
            }
        });
    }


    // =========================================================
    // START EVALUATION BATCH
    // =========================================================

    public async Task StartAsync(
        int batchId,
        string? expectedRowVersion = null)
    {
        var batch =
            await _context.EvaluationBatches

                // We need the actual scope
                .Include(
                    x =>
                        x.Group)

                .Include(
                    x =>
                        x.Suppliers)

                .Include(
                    x =>
                        x.Criteria)

                .FirstOrDefaultAsync(
                    x =>
                        x.BatchId ==
                        batchId);


        if (batch == null)
        {
            throw new BusinessRuleException(
                "Evaluation Batch không tồn tại.");
        }

        BatchConcurrencyHelper.VerifyExpectedRowVersion(batch, expectedRowVersion);


        // =====================================================
        // STATE RULES VALIDATION
        // =====================================================

        EvaluationBatchStateRules.EnsureCanStart(
            batch.Status,
            batch.Suppliers.Count,
            batch.Criteria.Count,
            batch.BatchCode);


        // =====================================================
        // PRODUCT GROUP
        // =====================================================

        if (batch.Group == null)
        {
            throw new BusinessRuleException(
                "Batch chưa có Product Group.");
        }


        if (batch.Group.Status != EntityStatus.Active)
        {
            throw new BusinessRuleException(
                $"Product Group " +
                $"{batch.Group.GroupCode} " +
                "không còn Active.");
        }


        // =====================================================
        // SUPPLIERS
        // =====================================================

        if (batch.Suppliers.Count < 2)
        {
            throw new BusinessRuleException(
                "Batch phải có ít nhất 2 nhà cung cấp " +
                "trước khi bắt đầu Evaluation.");
        }


        var inactiveSupplier =
            batch.Suppliers.FirstOrDefault(
                x =>
                    x.Status !=
                    EntityStatus.Active);


        if (inactiveSupplier != null)
        {
            throw new BusinessRuleException(
                $"Nhà cung cấp " +
                $"{inactiveSupplier.SupplierCode} " +
                "không còn Active.");
        }


        // =====================================================
        // SUPPLIER / PRODUCT GROUP VALIDATION
        // =====================================================

        var eligibleSupplierIds =
            await _context.ProductGroups
                .AsNoTracking()
                .Where(
                    x =>
                        x.GroupId ==
                        batch.GroupId)
                .SelectMany(
                    x =>
                        x.Suppliers)
                .Where(
                    x =>
                        x.Status ==
                        EntityStatus.Active)
                .Select(
                    x =>
                        x.SupplierId)
                .ToHashSetAsync();


        var invalidSupplier =
            batch.Suppliers.FirstOrDefault(
                x =>
                    !eligibleSupplierIds.Contains(
                        x.SupplierId));


        if (invalidSupplier != null)
        {
            throw new BusinessRuleException(
                $"Nhà cung cấp " +
                $"{invalidSupplier.SupplierCode} " +
                "không thuộc Product Group của Batch.");
        }


        // =====================================================
        // CRITERIA
        // =====================================================

        if (batch.Criteria.Count < 2)
        {
            throw new BusinessRuleException(
                "Batch phải có ít nhất 2 tiêu chí " +
                "trước khi bắt đầu Evaluation.");
        }


        var inactiveCriterion =
            batch.Criteria.FirstOrDefault(
                x =>
                    x.Status !=
                    EntityStatus.Active);


        if (inactiveCriterion != null)
        {
            throw new BusinessRuleException(
                $"Tiêu chí " +
                $"{inactiveCriterion.CriteriaCode} " +
                "không còn Active.");
        }


        // =====================================================
        // START
        // =====================================================

        var effectiveStartDate =
            batch.StartDate ??
            DateOnly.FromDateTime(DateTime.Today);

        if (
            batch.EndDate.HasValue &&
            batch.EndDate.Value < effectiveStartDate)
        {
            throw new BusinessRuleException(
                "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu đánh giá.");
        }

        batch.Status =
            BatchStatus.Evaluating;

        if (!batch.StartDate.HasValue)
        {
            batch.StartDate =
                effectiveStartDate;
        }

        try
        {
            await BatchConcurrencyHelper.BumpAndVerifyBatchVersionAsync(
                _context,
                batch,
                expectedRowVersion,
                newStatus: BatchStatus.Evaluating);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Xung đột đồng thời khi bắt đầu đợt đánh giá {BatchId}.", batchId);
            throw new BusinessRuleException(BatchConcurrencyHelper.ConcurrencyConflictMessage);
        }

        _logger.LogInformation(
            "Bắt đầu đánh giá cho đợt {BatchCode} (ID: {BatchId}).",
            batch.BatchCode,
            batch.BatchId);
    }


    // =========================================================
    // CANCEL BATCH
    // =========================================================

    public async Task CancelAsync(
        int batchId,
        string? reason = null,
        string? expectedRowVersion = null)
    {
        var batch = await _context.EvaluationBatches
            .FirstOrDefaultAsync(x => x.BatchId == batchId);

        if (batch == null)
        {
            throw new BusinessRuleException(
                "Evaluation Batch không tồn tại.");
        }

        BatchConcurrencyHelper.VerifyExpectedRowVersion(batch, expectedRowVersion);
        EvaluationBatchStateRules.EnsureCanCancel(batch.Status);

        var cancelledAt = DateTime.UtcNow;
        batch.Status = BatchStatus.Cancelled;
        batch.CancelledAt = cancelledAt;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var note = $"[Cancellation Reason]: {reason.Trim()}";
            batch.Description = string.IsNullOrWhiteSpace(batch.Description)
                ? note
                : $"{batch.Description}\n{note}";
        }

        try
        {
            await BatchConcurrencyHelper.BumpAndVerifyBatchVersionAsync(
                _context,
                batch,
                expectedRowVersion,
                newStatus: BatchStatus.Cancelled,
                cancelledAt: cancelledAt);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Xung đột đồng thời khi hủy đợt đánh giá {BatchId}.", batchId);
            throw new BusinessRuleException(BatchConcurrencyHelper.ConcurrencyConflictMessage);
        }

        _logger.LogInformation(
            "Hủy đợt đánh giá {BatchCode} (ID: {BatchId}). Lý do: {Reason}",
            batch.BatchCode,
            batch.BatchId,
            reason ?? "N/A");
    }


    // =========================================================
    // GET BY ID (LIGHTWEIGHT HEADER ONLY)
    // =========================================================

    public async Task<EvaluationBatch?> GetByIdAsync(int id)
    {
        return await _context.EvaluationBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BatchId == id);
    }

    // =========================================================
    // DETAIL
    // =========================================================

    public async Task<EvaluationBatch?>
        GetDetailAsync(
            int id)
    {
        return await _context.EvaluationBatches
            .AsNoTracking()
            .AsSplitQuery()

            // =====================================================
            // BASIC INFORMATION
            // =====================================================

            .Include(
                x =>
                    x.Group)

            .Include(
                x =>
                    x.CreatedByNavigation)

            // =====================================================
            // SCOPE
            // =====================================================

            .Include(
                x =>
                    x.Suppliers)

            .Include(
                x =>
                    x.Criteria)

            // =====================================================
            // WORKFLOW DATA
            // =====================================================

            .Include(
                x =>
                    x.EvaluationScores)

            .Include(
                x =>
                    x.AhppairwiseComparisons)

            .Include(
                x =>
                    x.Ahpresult)

            .Include(
                x =>
                    x.AhpcriteriaWeights)

            .Include(
                x =>
                    x.TopsisResults)

            .Include(
                x =>
                    x.Approvals)

            .FirstOrDefaultAsync(
                x =>
                    x.BatchId ==
                    id);
    }

}
