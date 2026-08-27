using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class EvaluationBatchService
{
    private readonly ApplicationDbContext _context;

    public EvaluationBatchService(
        ApplicationDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // LIST
    // =========================================================

    public async Task<List<EvaluationBatchListItemViewModel>>
        SearchAsync(
            string? keyword,
            string? status)
    {
        keyword = keyword?.Trim();


        var query =
            _context.EvaluationBatches
                .AsNoTracking()
                .Select(batch =>
                    new EvaluationBatchListItemViewModel
                    {
                        BatchId =
                            batch.BatchId,

                        BatchCode =
                            batch.BatchCode,

                        BatchName =
                            batch.BatchName,

                        GroupCode =
                            batch.Group.GroupCode,

                        GroupName =
                            batch.Group.GroupName,

                        Status =
                            batch.Status,

                        CreatedAt =
                            batch.CreatedAt,

                        StartDate =
                            batch.StartDate,

                        EndDate =
                            batch.EndDate,

                        SupplierCount =
                            batch.Suppliers.Count,

                        CriteriaCount =
                            batch.Criteria.Count
                    });


        // =========================================================
        // SEARCH
        // =========================================================

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query =
                query.Where(x =>
                    x.BatchCode.Contains(keyword) ||
                    x.BatchName.Contains(keyword));
        }


        // =========================================================
        // STATUS
        // =========================================================

        if (!string.IsNullOrWhiteSpace(status) &&
            status != "All")
        {
            query =
                query.Where(
                    x => x.Status == status);
        }


        // =========================================================
        // RESULT
        // =========================================================

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
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
            throw new InvalidOperationException(
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
            throw new InvalidOperationException(
                "Phải chọn ít nhất 2 nhà cung cấp.");
        }


        if (
            selectedSupplierIds.Any(
                x =>
                    !eligibleSupplierIds.Contains(x)))
        {
            throw new InvalidOperationException(
                "Có nhà cung cấp không thuộc Product Group đã chọn.");
        }


        var selectedCriteriaIds =
            model.SelectedCriteriaIds
                .Distinct()
                .ToList();


        if (selectedCriteriaIds.Count < 2)
        {
            throw new InvalidOperationException(
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
            throw new InvalidOperationException(
                "Danh sách tiêu chí không hợp lệ.");
        }


        if (
            model.StartDate.HasValue &&
            model.EndDate.HasValue &&
            model.EndDate < model.StartDate)
        {
            throw new InvalidOperationException(
                "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        var effectiveStartDate =
            model.StartDate ??
            DateOnly.FromDateTime(DateTime.Today);

        if (
            model.EndDate.HasValue &&
            model.EndDate.Value < effectiveStartDate)
        {
            throw new InvalidOperationException(
                "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu đánh giá.");
        }


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
                throw new InvalidOperationException(
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


            return batch.BatchId;
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }


    // =========================================================
    // START EVALUATION BATCH
    // =========================================================

    public async Task StartAsync(
        int batchId)
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
            throw new InvalidOperationException(
                "Evaluation Batch không tồn tại.");
        }


        // =====================================================
        // STATUS
        // =====================================================

        if (batch.Status != BatchStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Không thể bắt đầu Batch " +
                $"{batch.BatchCode} vì trạng thái hiện tại " +
                $"là {batch.Status}.");
        }


        // =====================================================
        // PRODUCT GROUP
        // =====================================================

        if (batch.Group == null)
        {
            throw new InvalidOperationException(
                "Batch chưa có Product Group.");
        }


        if (batch.Group.Status != EntityStatus.Active)
        {
            throw new InvalidOperationException(
                $"Product Group " +
                $"{batch.Group.GroupCode} " +
                "không còn Active.");
        }


        // =====================================================
        // SUPPLIERS
        // =====================================================

        if (batch.Suppliers.Count < 2)
        {
            throw new InvalidOperationException(
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
            throw new InvalidOperationException(
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
            throw new InvalidOperationException(
                $"Nhà cung cấp " +
                $"{invalidSupplier.SupplierCode} " +
                "không thuộc Product Group của Batch.");
        }


        // =====================================================
        // CRITERIA
        // =====================================================

        if (batch.Criteria.Count < 2)
        {
            throw new InvalidOperationException(
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
            throw new InvalidOperationException(
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
            throw new InvalidOperationException(
                "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu đánh giá.");
        }

        batch.Status =
            BatchStatus.Evaluating;

        if (!batch.StartDate.HasValue)
        {
            batch.StartDate =
                effectiveStartDate;
        }

        await _context.SaveChangesAsync();
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