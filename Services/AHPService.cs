using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;

namespace SupplierSelectionDSS.Services;

public class AHPService
{
    private readonly ApplicationDbContext _context;
    private readonly AHPCalculationService _calculationService;

    // =========================================================
    // CONSTANTS
    // =========================================================

    private const int MinimumCriteriaCount = 2;

    // =========================================================
    // SAATY SCALE
    // =========================================================

    private static readonly HashSet<decimal> AllowedSaatyValues =
        new()
        {
            1m,
            2m,
            3m,
            4m,
            5m,
            6m,
            7m,
            8m,
            9m,

            0.5m,
            0.3333m,
            0.25m,
            0.2m,
            0.1667m,
            0.1429m,
            0.125m,
            0.1111m
        };

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public AHPService(
        ApplicationDbContext context,
        AHPCalculationService calculationService)
    {
        _context = context;
        _calculationService = calculationService;
    }

    // =========================================================
    // GET COMPARISON MATRIX + RESULT
    // =========================================================

    public async Task<AHPComparisonViewModel?>
        GetComparisonMatrixAsync(
            int batchId)
    {
        var batch =
            await _context.EvaluationBatches
                .AsNoTracking()
                .Include(x => x.Criteria)
                .FirstOrDefaultAsync(
                    x => x.BatchId == batchId);

        if (batch == null)
        {
            return null;
        }

        var criteria =
            batch.Criteria
                .OrderBy(x => x.CriteriaCode)
                .Select(x =>
                    new AHPComparisonCriteriaViewModel
                    {
                        CriteriaId = x.CriteriaId,
                        CriteriaCode = x.CriteriaCode,
                        CriteriaName = x.CriteriaName
                    })
                .ToList();

        var comparisons =
            await _context.AhppairwiseComparisons
                .AsNoTracking()
                .Where(x => x.BatchId == batchId)
                .ToListAsync();

        var model =
            new AHPComparisonViewModel
            {
                BatchId = batch.BatchId,
                BatchCode = batch.BatchCode,
                BatchName = batch.BatchName,
                Status = batch.Status,
                Criteria = criteria
            };

        foreach (var comparison in comparisons)
        {
            model.Values[
                BuildKey(
                    comparison.CriteriaRowId,
                    comparison.CriteriaColId)] =
                comparison.ComparisonValue;
        }

        // -----------------------------------------------------
        // Diagonal = 1
        // -----------------------------------------------------

        foreach (var criterion in criteria)
        {
            model.Values[
                BuildKey(
                    criterion.CriteriaId,
                    criterion.CriteriaId)] = 1m;
        }

        // -----------------------------------------------------
        // Previous calculation
        // -----------------------------------------------------

        model.Result =
            await GetResultAsync(batchId);

        return model;
    }

    // =========================================================
    // SAVE PAIRWISE COMPARISONS
    // =========================================================

    public async Task SaveComparisonsAsync(
        AHPComparisonInputViewModel model,
        int evaluatedBy)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (evaluatedBy <= 0)
        {
            throw new InvalidOperationException(
                "Người đánh giá không hợp lệ.");
        }

        var batch =
            await _context.EvaluationBatches
                .Include(x => x.Criteria)
                .FirstOrDefaultAsync(
                    x => x.BatchId == model.BatchId);

        if (batch == null)
        {
            throw new InvalidOperationException(
                "Evaluation Batch không tồn tại.");
        }

        EnsureBatchIsEvaluating(batch.Status);

        var criteria =
            batch.Criteria
                .OrderBy(x => x.CriteriaCode)
                .ToList();

        ValidateCriteriaCount(criteria.Count);

        if (model.Comparisons == null ||
            model.Comparisons.Count == 0)
        {
            throw new InvalidOperationException(
                "Danh sách cặp so sánh AHP không được để trống.");
        }

        var criteriaIds =
            criteria
                .Select(x => x.CriteriaId)
                .ToHashSet();

        ValidateSubmittedComparisons(
            model.Comparisons,
            criteriaIds,
            criteria.Count);

        var evaluationTimestamp =
            DateTime.UtcNow;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // Remove previous matrix
            // -------------------------------------------------

            var existingComparisons =
                await _context.AhppairwiseComparisons
                    .Where(x =>
                        x.BatchId == model.BatchId)
                    .ToListAsync();

            if (existingComparisons.Count > 0)
            {
                _context.AhppairwiseComparisons
                    .RemoveRange(existingComparisons);
            }

            // -------------------------------------------------
            // Insert diagonal
            // -------------------------------------------------

            foreach (var criterion in criteria)
            {
                _context.AhppairwiseComparisons.Add(
                    new AhppairwiseComparison
                    {
                        BatchId =
                            model.BatchId,

                        CriteriaRowId =
                            criterion.CriteriaId,

                        CriteriaColId =
                            criterion.CriteriaId,

                        ComparisonValue =
                            1m,

                        EvaluatedBy =
                            evaluatedBy,

                        EvaluatedAt =
                            evaluationTimestamp
                    });
            }

            // -------------------------------------------------
            // Insert direct + inverse values
            // -------------------------------------------------

            foreach (var item in model.Comparisons)
            {
                var value =
                    Math.Round(
                        item.Value,
                        4,
                        MidpointRounding.AwayFromZero);

                var inverseValue =
                    Math.Round(
                        1m / value,
                        4,
                        MidpointRounding.AwayFromZero);

                _context.AhppairwiseComparisons.Add(
                    new AhppairwiseComparison
                    {
                        BatchId =
                            model.BatchId,

                        CriteriaRowId =
                            item.CriteriaRowId,

                        CriteriaColId =
                            item.CriteriaColId,

                        ComparisonValue =
                            value,

                        EvaluatedBy =
                            evaluatedBy,

                        EvaluatedAt =
                            evaluationTimestamp
                    });

                _context.AhppairwiseComparisons.Add(
                    new AhppairwiseComparison
                    {
                        BatchId =
                            model.BatchId,

                        CriteriaRowId =
                            item.CriteriaColId,

                        CriteriaColId =
                            item.CriteriaRowId,

                        ComparisonValue =
                            inverseValue,

                        EvaluatedBy =
                            evaluatedBy,

                        EvaluatedAt =
                            evaluationTimestamp
                    });
            }

            await _context.SaveChangesAsync();

            // -------------------------------------------------
            // Invalidate previous AHP result
            // -------------------------------------------------

            var oldResult =
                await _context.Ahpresults
                    .FirstOrDefaultAsync(
                        x => x.BatchId == model.BatchId);

            if (oldResult != null)
            {
                _context.Ahpresults.Remove(oldResult);
            }

            // -------------------------------------------------
            // Invalidate previous AHP weights
            // -------------------------------------------------

            var oldWeights =
                await _context.AhpcriteriaWeights
                    .Where(x =>
                        x.BatchId == model.BatchId)
                    .ToListAsync();

            if (oldWeights.Count > 0)
            {
                _context.AhpcriteriaWeights
                    .RemoveRange(oldWeights);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // CALCULATE AHP
    // =========================================================

    public async Task<AHPResultViewModel>
        CalculateAsync(
            int batchId)
    {
        var batch =
            await _context.EvaluationBatches
                .AsNoTracking()
                .Include(x => x.Criteria)
                .FirstOrDefaultAsync(
                    x => x.BatchId == batchId);

        if (batch == null)
        {
            throw new InvalidOperationException(
                "Evaluation Batch không tồn tại.");
        }

        EnsureBatchIsEvaluating(batch.Status);

        var criteria =
            batch.Criteria
                .OrderBy(x => x.CriteriaCode)
                .ToList();

        ValidateCriteriaCount(criteria.Count);

        var n = criteria.Count;

        // =====================================================
        // LOAD PAIRWISE MATRIX
        // =====================================================

        var comparisons =
            await _context.AhppairwiseComparisons
                .AsNoTracking()
                .Where(x => x.BatchId == batchId)
                .ToListAsync();

        var expectedCount = n * n;

        if (comparisons.Count != expectedCount)
        {
            throw new InvalidOperationException(
                $"Ma trận AHP chưa đầy đủ. " +
                $"Cần {expectedCount} giá trị nhưng hiện có {comparisons.Count}.");
        }

        var comparisonMap =
            comparisons.ToDictionary(
                x => (x.CriteriaRowId, x.CriteriaColId),
                x => x.ComparisonValue);

        var matrix =
            new double[n, n];

        for (var i = 0; i < n; i++)
        {
            var rowId =
                criteria[i].CriteriaId;

            for (var j = 0; j < n; j++)
            {
                var colId =
                    criteria[j].CriteriaId;

                if (!comparisonMap.TryGetValue(
                        (rowId, colId),
                        out var value))
                {
                    throw new InvalidOperationException(
                        $"Thiếu giá trị AHP cho " +
                        $"{criteria[i].CriteriaCode} / " +
                        $"{criteria[j].CriteriaCode}.");
                }

                matrix[i, j] =
                    (double)value;
            }
        }

        // =====================================================
        // CALCULATE
        // =====================================================

        var calculation =
            _calculationService.Calculate(matrix);

        ValidateCalculationResult(
            calculation,
            n);

        // =====================================================
        // PERSIST RESULT
        // =====================================================

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // Remove old result
            // -------------------------------------------------

            var oldResult =
                await _context.Ahpresults
                    .FirstOrDefaultAsync(
                        x => x.BatchId == batchId);

            if (oldResult != null)
            {
                _context.Ahpresults.Remove(oldResult);
            }

            // -------------------------------------------------
            // Remove old weights
            // -------------------------------------------------

            var oldWeights =
                await _context.AhpcriteriaWeights
                    .Where(x => x.BatchId == batchId)
                    .ToListAsync();

            if (oldWeights.Count > 0)
            {
                _context.AhpcriteriaWeights
                    .RemoveRange(oldWeights);
            }

            var calculatedAt =
                DateTime.UtcNow;

            // -------------------------------------------------
            // Insert AHP result
            // -------------------------------------------------

            var resultEntity =
                new Ahpresult
                {
                    BatchId =
                        batchId,

                    LambdaMax =
                        ToDecimal(
                            calculation.LambdaMax),

                    Civalue =
                        ToDecimal(
                            calculation.CIValue),

                    Rivalue =
                        ToDecimal(
                            calculation.RIValue),

                    Crvalue =
                        ToDecimal(
                            calculation.CRValue),

                    IsConsistent =
                        calculation.IsConsistent,

                    N =
                        calculation.N,

                    CalculatedAt =
                        calculatedAt
                };

            _context.Ahpresults
                .Add(resultEntity);

            // -------------------------------------------------
            // Insert weights
            // -------------------------------------------------

            for (var i = 0; i < n; i++)
            {
                _context.AhpcriteriaWeights.Add(
                    new AhpcriteriaWeight
                    {
                        BatchId =
                            batchId,

                        CriteriaId =
                            criteria[i].CriteriaId,

                        WeightValue =
                            ToDecimal(
                                calculation.Weights[i])
                    });
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // -------------------------------------------------
            // Return ViewModel
            // -------------------------------------------------

            return new AHPResultViewModel
            {
                HasResult = true,

                BatchId =
                    batchId,

                CriteriaCount =
                    n,

                LambdaMax =
                    calculation.LambdaMax,

                CIValue =
                    calculation.CIValue,

                RIValue =
                    calculation.RIValue,

                CRValue =
                    calculation.CRValue,

                IsConsistent =
                    calculation.IsConsistent,

                CalculatedAt =
                    calculatedAt,

                Weights =
                    criteria
                        .Select(
                            (criterion, index) =>
                                new AHPWeightViewModel
                                {
                                    CriteriaId =
                                        criterion.CriteriaId,

                                    CriteriaCode =
                                        criterion.CriteriaCode,

                                    CriteriaName =
                                        criterion.CriteriaName,

                                    WeightValue =
                                        calculation.Weights[index]
                                })
                        .ToList()
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // =========================================================
    // GET STORED RESULT
    // =========================================================

    private async Task<AHPResultViewModel?>
        GetResultAsync(
            int batchId)
    {
        var result =
            await _context.Ahpresults
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.BatchId == batchId);

        if (result == null)
        {
            return null;
        }

        var weights =
            await (
                from weight
                    in _context.AhpcriteriaWeights.AsNoTracking()

                join criterion
                    in _context.Criteria.AsNoTracking()

                    on weight.CriteriaId
                        equals criterion.CriteriaId

                where
                    weight.BatchId == batchId

                orderby
                    criterion.CriteriaCode

                select new AHPWeightViewModel
                {
                    CriteriaId =
                        criterion.CriteriaId,

                    CriteriaCode =
                        criterion.CriteriaCode,

                    CriteriaName =
                        criterion.CriteriaName,

                    WeightValue =
                        (double)weight.WeightValue
                })
                .ToListAsync();

        return new AHPResultViewModel
        {
            HasResult = true,

            BatchId =
                batchId,

            CriteriaCount =
                result.N ?? 0,

            LambdaMax =
                (double)result.LambdaMax,

            CIValue =
                (double)result.Civalue,

            RIValue =
                (double)result.Rivalue,

            CRValue =
                (double)result.Crvalue,

            IsConsistent =
                result.IsConsistent,

            CalculatedAt =
                result.CalculatedAt,

            Weights =
                weights
        };
    }

    // =========================================================
    // VALIDATE SUBMITTED COMPARISONS
    // =========================================================

    private static void ValidateSubmittedComparisons(
        IReadOnlyCollection<AHPComparisonInputItemViewModel> comparisons,
        HashSet<int> criteriaIds,
        int criteriaCount)
    {
        var expectedPairCount =
            criteriaCount *
            (criteriaCount - 1) /
            2;

        if (comparisons.Count != expectedPairCount)
        {
            throw new InvalidOperationException(
                $"Cần nhập đủ {expectedPairCount} cặp so sánh.");
        }

        var directedPairs =
            new HashSet<(int RowId, int ColId)>();

        var unorderedPairs =
            new HashSet<(int MinId, int MaxId)>();

        foreach (var item in comparisons)
        {
            if (!criteriaIds.Contains(item.CriteriaRowId) ||
                !criteriaIds.Contains(item.CriteriaColId))
            {
                throw new InvalidOperationException(
                    "Criteria không thuộc Evaluation Batch.");
            }

            if (item.CriteriaRowId == item.CriteriaColId)
            {
                throw new InvalidOperationException(
                    "Không cần nhập so sánh Criteria với chính nó.");
            }

            ValidateComparisonValue(item.Value);

            if (!directedPairs.Add(
                    (item.CriteriaRowId,
                     item.CriteriaColId)))
            {
                throw new InvalidOperationException(
                    "Ma trận chứa cặp tiêu chí bị trùng.");
            }

            var minId =
                Math.Min(
                    item.CriteriaRowId,
                    item.CriteriaColId);

            var maxId =
                Math.Max(
                    item.CriteriaRowId,
                    item.CriteriaColId);

            if (!unorderedPairs.Add(
                    (minId, maxId)))
            {
                throw new InvalidOperationException(
                    "Mỗi cặp Criteria chỉ được nhập một lần.");
            }
        }
    }

    // =========================================================
    // VALIDATE SAATY VALUE
    // =========================================================

    private static void ValidateComparisonValue(
        decimal value)
    {
        var rounded =
            Math.Round(
                value,
                4,
                MidpointRounding.AwayFromZero);

        if (!AllowedSaatyValues.Contains(rounded))
        {
            throw new InvalidOperationException(
                "Giá trị AHP phải thuộc thang Saaty " +
                "1–9 hoặc nghịch đảo tương ứng.");
        }
    }

    // =========================================================
    // VALIDATE CRITERIA COUNT
    // =========================================================

    private static void ValidateCriteriaCount(
        int criteriaCount)
    {
        if (criteriaCount < MinimumCriteriaCount)
        {
            throw new InvalidOperationException(
                "Evaluation Batch phải có ít nhất 2 tiêu chí để thực hiện AHP.");
        }
    }

    // =========================================================
    // VALIDATE BATCH STATUS
    // =========================================================

    private static void EnsureBatchIsEvaluating(
        string? status)
    {
        if (!string.Equals(
                status,
                BatchStatus.Evaluating.ToString(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Chỉ Evaluation Batch đang ở trạng thái Evaluating " +
                "mới được phép thực hiện AHP.");
        }
    }

    // =========================================================
    // VALIDATE CALCULATION RESULT
    // =========================================================

    private static void ValidateCalculationResult(
        AHPCalculationResult calculation,
        int criteriaCount)
    {
        ArgumentNullException.ThrowIfNull(calculation);

        if (calculation.N != criteriaCount)
        {
            throw new InvalidOperationException(
                "Kích thước kết quả AHP không khớp số lượng Criteria.");
        }

        if (calculation.Weights == null ||
            calculation.Weights.Length != criteriaCount)
        {
            throw new InvalidOperationException(
                "Số lượng trọng số AHP không khớp số lượng Criteria.");
        }

        for (var i = 0;
             i < calculation.Weights.Length;
             i++)
        {
            var weight =
                calculation.Weights[i];

            if (!double.IsFinite(weight) ||
                weight <= 0)
            {
                throw new InvalidOperationException(
                    $"Trọng số AHP tại vị trí {i + 1} không hợp lệ.");
            }
        }

        if (!double.IsFinite(calculation.LambdaMax) ||
            !double.IsFinite(calculation.CIValue) ||
            !double.IsFinite(calculation.RIValue) ||
            !double.IsFinite(calculation.CRValue))
        {
            throw new InvalidOperationException(
                "Kết quả tính toán AHP chứa giá trị không hợp lệ.");
        }
    }

    // =========================================================
    // DECIMAL CONVERSION
    // =========================================================

    private static decimal ToDecimal(
        double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException(
                "Không thể lưu giá trị AHP không hữu hạn.");
        }

        return Math.Round(
            Convert.ToDecimal(value),
            6,
            MidpointRounding.AwayFromZero);
    }

    // =========================================================
    // KEY
    // =========================================================

    private static string BuildKey(
        int rowId,
        int colId)
    {
        return $"{rowId}_{colId}";
    }
}