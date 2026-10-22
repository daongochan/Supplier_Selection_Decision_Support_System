using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class EvaluationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EvaluationService> _logger;

    public EvaluationService(
        ApplicationDbContext context,
        ILogger<EvaluationService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<EvaluationService>.Instance;
    }


    // =========================================================
    // GET EVALUATION MATRIX
    // =========================================================

    public async Task<EvaluationViewModel?> GetEvaluationAsync(
        int batchId)
    {
        var batch = await _context.EvaluationBatches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Group)
            .Include(x => x.Suppliers)
            .Include(x => x.Criteria)
            .FirstOrDefaultAsync(
                x => x.BatchId == batchId);

        if (batch == null)
        {
            return null;
        }


        var scores = await _context.EvaluationScores
            .AsNoTracking()
            .Where(x => x.BatchId == batchId)
            .ToListAsync();

        var scoreLookup = scores.ToDictionary(
            x => (x.SupplierId, x.CriteriaId));


        var model = new EvaluationViewModel
        {
            BatchId = batch.BatchId,

            BatchCode = batch.BatchCode,

            BatchName = batch.BatchName,

            GroupCode = batch.Group.GroupCode,

            GroupName = batch.Group.GroupName,

            Status = batch.Status
        };


        // =====================================================
        // CRITERIA COLUMNS
        // =====================================================

        model.Criteria = batch.Criteria
            .OrderBy(x => x.CriteriaCode)
            .Select(x =>
                new EvaluationCriteriaColumnViewModel
                {
                    CriteriaId = x.CriteriaId,

                    CriteriaCode = x.CriteriaCode,

                    CriteriaName = x.CriteriaName,

                    CriteriaType = x.CriteriaType,

                    Unit = x.Unit
                })
            .ToList();


        // =====================================================
        // SUPPLIER ROWS
        // =====================================================

        foreach (var supplier in batch.Suppliers
                     .OrderBy(x => x.SupplierCode))
        {
            var row =
                new EvaluationSupplierRowViewModel
                {
                    SupplierId =
                        supplier.SupplierId,

                    SupplierCode =
                        supplier.SupplierCode,

                    SupplierName =
                        supplier.SupplierName
                };


            foreach (var criterion in model.Criteria)
            {
                scoreLookup.TryGetValue(
                    (supplier.SupplierId, criterion.CriteriaId),
                    out var score);


                row.Scores[criterion.CriteriaId] =
                    new EvaluationCellViewModel
                    {
                        Score =
                            score?.ScoreValue,

                        EvaluationScoreId =
                            score?.ScoreId
                    };
            }


            model.Suppliers.Add(row);
        }


        return model;
    }


    // =========================================================
    // SAVE EVALUATION
    // =========================================================

    public async Task SaveAsync(
        EvaluationScoreInputViewModel model,
        int evaluatedBy)
    {
        var batch =
            await _context.EvaluationBatches
                .AsSplitQuery()
                .Include(x => x.Suppliers)
                .Include(x => x.Criteria)
                .FirstOrDefaultAsync(
                    x => x.BatchId == model.BatchId);


        if (batch == null)
        {
            throw new BusinessRuleException(
                "Evaluation batch không tồn tại.");
        }


        // Chỉ cho phép nhập điểm khi Batch đang ở trạng thái Evaluating.
        if (!string.Equals(
                batch.Status,
                BatchStatus.Evaluating.ToString(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(
                "Evaluation batch chưa ở trạng thái đánh giá hoặc đã khóa, không thể nhập dữ liệu.");
        }


        var supplierIds =
            batch.Suppliers
                .Select(x => x.SupplierId)
                .ToHashSet();


        var criteria =
            batch.Criteria
                .OrderBy(x => x.CriteriaCode)
                .ToList();


        var criteriaIds =
            criteria
                .Select(x => x.CriteriaId)
                .ToHashSet();

        if (model.Rows == null || model.Rows.Count == 0)
        {
            throw new BusinessRuleException(
                "Không có dữ liệu đánh giá để lưu.");
        }

        var duplicateSupplierIds = model.Rows
            .GroupBy(x => x.SupplierId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateSupplierIds.Count > 0)
        {
            throw new BusinessRuleException(
                "Dữ liệu đánh giá chứa Supplier bị trùng.");
        }

        // Tải toàn bộ score hiện tại một lần thay vì truy vấn DB cho từng ô.
        var existingScores = await _context.EvaluationScores
            .Where(x => x.BatchId == model.BatchId)
            .ToListAsync();

        var existingLookup = existingScores
            .ToDictionary(x => (x.SupplierId, x.CriteriaId));


        // =====================================================
        // VALIDATE + SAVE
        // =====================================================

        foreach (var row in model.Rows)
        {
            // -------------------------------------------------
            // Supplier phải thuộc Batch
            // -------------------------------------------------

            if (!supplierIds.Contains(row.SupplierId))
            {
                throw new BusinessRuleException(
                    "Supplier không thuộc Evaluation Batch.");
            }


            foreach (var criterion in criteria)
            {
                var criterionId =
                    criterion.CriteriaId;


                // -------------------------------------------------
                // Lấy score
                // -------------------------------------------------

                row.Scores.TryGetValue(
                    criterionId,
                    out var score);


                // -------------------------------------------------
                // Tìm record hiện tại
                // -------------------------------------------------

                existingLookup.TryGetValue(
                    (row.SupplierId, criterionId),
                    out var existing);


                // -------------------------------------------------
                // Không nhập giá trị
                // -------------------------------------------------

                if (!score.HasValue)
                {
                    /*
                     * Nếu trước đó đã có score nhưng người dùng
                     * xóa ô nhập liệu thì xóa record.
                     */

                    if (existing != null)
                    {
                        _context.EvaluationScores
                            .Remove(existing);

                        existingLookup.Remove(
                            (row.SupplierId, criterionId));
                    }

                    continue;
                }


                // -------------------------------------------------
                // Validation theo loại Criteria
                // -------------------------------------------------

                ValidateScore(
                    criterion,
                    score.Value);


                // -------------------------------------------------
                // INSERT
                // -------------------------------------------------

                if (existing == null)
                {
                    var entity =
                        new EvaluationScore
                        {
                            BatchId =
                                model.BatchId,

                            SupplierId =
                                row.SupplierId,

                            CriteriaId =
                                criterionId,

                            ScoreValue =
                                score.Value,

                            EvaluatedBy =
                                evaluatedBy,

                            EvaluatedAt =
                                DateTime.UtcNow
                        };


                    _context.EvaluationScores
                        .Add(entity);

                    existingLookup[(row.SupplierId, criterionId)] = entity;
                }


                // -------------------------------------------------
                // UPDATE
                // -------------------------------------------------

                else
                {
                    existing.ScoreValue =
                        score.Value;

                    existing.EvaluatedBy =
                        evaluatedBy;

                    existing.EvaluatedAt =
                        DateTime.UtcNow;
                }
            }
        }

        // -------------------------------------------------
        // Invalidate stale TOPSIS results & ideals (scores changed)
        // -------------------------------------------------
        var staleTopsisResults = await _context.TopsisResults
            .Where(x => x.BatchId == model.BatchId)
            .ToListAsync();
        if (staleTopsisResults.Count > 0)
        {
            _context.TopsisResults.RemoveRange(staleTopsisResults);
        }

        var staleTopsisIdeals = await _context.TopsisIdealSolutions
            .Where(x => x.BatchId == model.BatchId)
            .ToListAsync();
        if (staleTopsisIdeals.Count > 0)
        {
            _context.TopsisIdealSolutions.RemoveRange(staleTopsisIdeals);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Lưu bảng điểm đánh giá cho đợt ID {BatchId} bởi User {UserId} thành công ({RowCount} nhà cung cấp).",
            model.BatchId,
            evaluatedBy,
            model.Rows?.Count ?? 0);
    }


    // =========================================================
    // SCORE VALIDATION
    // =========================================================

    private static void ValidateScore(
        Criterion criterion,
        decimal score)
    {
        var unit =
            criterion.Unit?.Trim();


        // -----------------------------------------------------
        // Rating Criteria
        // -----------------------------------------------------

        if (CriteriaUnits.IsRatingScale(unit))
        {
            if (score < 0m ||
                score > 10m)
            {
                throw new BusinessRuleException(
                    $"Tiêu chí '{criterion.CriteriaName}' " +
                    "sử dụng thang điểm 0–10.");
            }

            return;
        }


        // -----------------------------------------------------
        // Numeric Criteria
        // -----------------------------------------------------

        /*
         * Các tiêu chí như:
         * - VND
         * - Ngày
         * - %
         * - kg
         * - km
         *
         * đều lưu giá trị thực tế.
         *
         * Chưa đặt upper bound vì database hiện tại
         * chưa có MaxValue.
         */

        if (score < 0m)
        {
            throw new BusinessRuleException(
                $"Giá trị của tiêu chí '{criterion.CriteriaName}' " +
                "không được âm.");
        }
    }


    // =========================================================
    // CHECK COMPLETENESS
    // =========================================================

    public async Task<bool> HasCompleteScoresAsync(
        int batchId)
    {
        var batch =
            await _context.EvaluationBatches
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Suppliers)
                .Include(x => x.Criteria)
                .FirstOrDefaultAsync(
                    x => x.BatchId == batchId);


        if (batch == null)
        {
            return false;
        }


        var supplierIds = batch.Suppliers
            .Select(x => x.SupplierId)
            .ToHashSet();

        var criteriaIds = batch.Criteria
            .Select(x => x.CriteriaId)
            .ToHashSet();

        if (supplierIds.Count == 0 || criteriaIds.Count == 0)
        {
            return false;
        }

        var scores = await _context.EvaluationScores
            .AsNoTracking()
            .Where(x => x.BatchId == batchId)
            .Select(x => new { x.SupplierId, x.CriteriaId })
            .ToListAsync();

        var validScores = scores
            .Where(x => supplierIds.Contains(x.SupplierId)
                     && criteriaIds.Contains(x.CriteriaId))
            .Select(x => (x.SupplierId, x.CriteriaId))
            .ToHashSet();

        return validScores.Count == supplierIds.Count * criteriaIds.Count;
    }
}
