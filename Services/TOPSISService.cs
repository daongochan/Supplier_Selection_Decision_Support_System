using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class TOPSISService
{
    private readonly ApplicationDbContext _context;

    private readonly TOPSISCalculationService
        _calculationService;


    public TOPSISService(
        ApplicationDbContext context,
        TOPSISCalculationService calculationService)
    {
        _context = context;

        _calculationService =
            calculationService;
    }


    // =========================================================
    // GET TOPSIS WORKSPACE
    // =========================================================

    public async Task<TOPSISViewModel?>
        GetWorkspaceAsync(
            int batchId)
    {
        var batch =
            await _context.EvaluationBatches
                .AsNoTracking()
                .Include(x => x.Suppliers)
                .Include(x => x.Criteria)
                .FirstOrDefaultAsync(
                    x => x.BatchId == batchId);


        if (batch == null)
        {
            return null;
        }


        var model =
            new TOPSISViewModel
            {
                BatchId =
                    batch.BatchId,

                BatchCode =
                    batch.BatchCode,

                BatchName =
                    batch.BatchName,

                Status =
                    batch.Status
            };


        // =====================================================
        // AHP RESULT
        // =====================================================

        var ahpResult =
            await _context.Ahpresults
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.BatchId == batchId);


        model.HasAHPResult =
            ahpResult != null;


        if (ahpResult != null)
        {
            model.IsAHPConsistent =
                ahpResult.IsConsistent;

            model.AHPConsistencyRatio =
                (double)ahpResult.Crvalue;
        }


        // =====================================================
        // AHP WEIGHTS
        // =====================================================

        var weightRecords =
            await _context.AhpcriteriaWeights
                .AsNoTracking()
                .Where(
                    x => x.BatchId == batchId)
                .ToListAsync();


        var weightMap =
            weightRecords
                .ToDictionary(
                    x => x.CriteriaId,
                    x => x.WeightValue);


        // =====================================================
        // CRITERIA
        // =====================================================

        model.Criteria =
            batch.Criteria
                .OrderBy(
                    x => x.CriteriaCode)
                .Select(
                    x =>
                        new TOPSISCriteriaViewModel
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
                                x.Unit,

                            Weight =
                                weightMap.TryGetValue(
                                    x.CriteriaId,
                                    out var weight)
                                    ? weight
                                    : 0m
                        })
                .ToList();


        // =====================================================
        // EVALUATION SCORES
        // =====================================================

        var scores =
            await _context.EvaluationScores
                .AsNoTracking()
                .Where(
                    x => x.BatchId == batchId)
                .ToListAsync();

        var scoreMap =
            scores.ToDictionary(
                x => (x.SupplierId, x.CriteriaId),
                x => x.ScoreValue);


        // =====================================================
        // SUPPLIERS
        // =====================================================

        foreach (
            var supplier
            in batch.Suppliers
                .OrderBy(
                    x => x.SupplierCode))
        {
            var row =
                new TOPSISEvaluationRowViewModel
                {
                    SupplierId =
                        supplier.SupplierId,

                    SupplierCode =
                        supplier.SupplierCode,

                    SupplierName =
                        supplier.SupplierName
                };


            foreach (
                var criterion
                in model.Criteria)
            {
                if (scoreMap.TryGetValue(
                    (supplier.SupplierId, criterion.CriteriaId),
                    out var scoreValue))
                {
                    row.Scores[
                        criterion.CriteriaId] =
                        scoreValue;
                }
            }


            model.Suppliers.Add(row);
        }


        // =====================================================
        // PREVIOUS PIS / NIS
        // =====================================================

        var ideals =
            await _context.TopsisIdealSolutions
                .AsNoTracking()
                .Where(
                    x => x.BatchId == batchId)
                .ToListAsync();


        var idealMap =
            ideals.ToDictionary(
                x => x.CriteriaId);


        foreach (var criterion
            in model.Criteria)
        {
            if (!idealMap.TryGetValue(
                criterion.CriteriaId,
                out var ideal))
            {
                continue;
            }


            model.IdealSolutions.Add(
                new TOPSISIdealViewModel
                {
                    CriteriaId =
                        criterion.CriteriaId,

                    CriteriaCode =
                        criterion.CriteriaCode,

                    CriteriaName =
                        criterion.CriteriaName,

                    CriteriaType =
                        criterion.CriteriaType,

                    Unit =
                        criterion.Unit,

                    PISValue =
                        ideal.Pisvalue,

                    NISValue =
                        ideal.Nisvalue
                });
        }


        // =====================================================
        // PREVIOUS RANKING
        // =====================================================

        var results =
            await _context.TopsisResults
                .AsNoTracking()
                .Include(
                    x => x.Supplier)
                .Where(
                    x => x.BatchId == batchId)
                .OrderBy(
                    x => x.RankNo)
                .ToListAsync();


        foreach (var result
            in results)
        {
            model.Ranking.Add(
                new TOPSISRankingViewModel
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


        return model;
    }


    // =========================================================
    // CALCULATE TOPSIS
    // =========================================================

    public async Task CalculateAsync(
        int batchId)
    {
        var batch =
            await _context.EvaluationBatches
                .Include(x => x.Suppliers)
                .Include(x => x.Criteria)
                .FirstOrDefaultAsync(
                    x => x.BatchId == batchId);


        if (batch == null)
        {
            throw new InvalidOperationException(
                "Evaluation Batch không tồn tại.");
        }


        if (!string.Equals(
            batch.Status,
            BatchStatus.Evaluating.ToString(),
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Chỉ được tính TOPSIS khi Batch đang ở trạng thái Evaluating.");
        }


        // =====================================================
        // 1. AHP MUST EXIST
        // =====================================================

        var ahpResult =
            await _context.Ahpresults
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.BatchId == batchId);


        if (ahpResult == null)
        {
            throw new InvalidOperationException(
                "Chưa có kết quả AHP. " +
                "Hãy thực hiện Calculate AHP trước khi chạy TOPSIS.");
        }


        // =====================================================
        // 2. AHP MUST BE CONSISTENT
        // =====================================================

        if (!ahpResult.IsConsistent)
        {
            throw new InvalidOperationException(
                $"AHP chưa đạt tính nhất quán. " +
                $"CR = {ahpResult.Crvalue:0.0000}. " +
                "Hãy xem lại ma trận AHP trước khi chạy TOPSIS.");
        }


        // =====================================================
        // 3. CRITERIA
        // =====================================================

        var criteria =
            batch.Criteria
                .OrderBy(
                    x => x.CriteriaCode)
                .ToList();


        if (!criteria.Any())
        {
            throw new InvalidOperationException(
                "Batch chưa có Criteria.");
        }


        // =====================================================
        // 4. SUPPLIERS
        // =====================================================

        var suppliers =
            batch.Suppliers
                .OrderBy(
                    x => x.SupplierCode)
                .ToList();


        if (suppliers.Count < 2)
        {
            throw new InvalidOperationException(
                "TOPSIS yêu cầu ít nhất 2 Supplier trong Batch.");
        }


        var criteriaCount =
            criteria.Count;

        var supplierCount =
            suppliers.Count;


        // =====================================================
        // 5. AHP WEIGHTS
        // =====================================================

        var weightRecords =
            await _context.AhpcriteriaWeights
                .AsNoTracking()
                .Where(
                    x => x.BatchId == batchId)
                .ToListAsync();


        if (weightRecords.Count !=
            criteriaCount)
        {
            throw new InvalidOperationException(
                "AHPCriteriaWeights chưa đầy đủ. " +
                "Hãy Calculate AHP lại.");
        }


        var weightMap =
            weightRecords
                .GroupBy(x => x.CriteriaId)
                .ToDictionary(
                    g => g.Key,
                    g => (double)g.Single().WeightValue);


        if (weightMap.Keys.Any(
            criteriaId =>
                criteria.All(c => c.CriteriaId != criteriaId)))
        {
            throw new InvalidOperationException(
                "Danh sách trọng số AHP chứa Criteria không thuộc Batch.");
        }


        var weights =
            new double[
                criteriaCount];


        for (var j = 0;
             j < criteriaCount;
             j++)
        {
            if (!weightMap.TryGetValue(
                criteria[j].CriteriaId,
                out var weight))
            {
                throw new InvalidOperationException(
                    $"Thiếu trọng số AHP cho " +
                    $"{criteria[j].CriteriaCode}.");
            }


            if (double.IsNaN(weight) ||
                double.IsInfinity(weight) ||
                weight <= 0)
            {
                throw new InvalidOperationException(
                    $"Trọng số AHP của {criteria[j].CriteriaCode} không hợp lệ.");
            }


            weights[j] =
                weight;
        }


        // =====================================================
        // 6. EVALUATION SCORES
        // =====================================================

        var scores =
            await _context.EvaluationScores
                .AsNoTracking()
                .Where(
                    x => x.BatchId == batchId)
                .ToListAsync();


        var scoreMap =
            scores
                .GroupBy(x => (x.SupplierId, x.CriteriaId))
                .ToDictionary(
                    g => g.Key,
                    g => g.Single().ScoreValue);


        var expectedScoreCount =
            supplierCount *
            criteriaCount;


        /*
         * Count không đủ chưa chắc đã chỉ ra
         * chính xác ô nào thiếu.
         * Vì vậy phía dưới vẫn kiểm tra từng cell.
         */

        if (scores.Count !=
            expectedScoreCount)
        {
            throw new InvalidOperationException(
                $"Ma trận đánh giá chưa đầy đủ. " +
                $"Cần {expectedScoreCount} giá trị " +
                $"nhưng hiện có {scores.Count}.");
        }


        // =====================================================
        // 7. BUILD DECISION MATRIX
        // =====================================================

        var decisionMatrix =
            new double[
                supplierCount,
                criteriaCount];


        for (var i = 0;
             i < supplierCount;
             i++)
        {
            for (var j = 0;
                 j < criteriaCount;
                 j++)
            {
                if (!scoreMap.TryGetValue(
                    (suppliers[i].SupplierId, criteria[j].CriteriaId),
                    out var scoreValue))
                {
                    throw new InvalidOperationException(
                        $"Thiếu Evaluation Score cho " +
                        $"{suppliers[i].SupplierCode} / " +
                        $"{criteria[j].CriteriaCode}.");
                }


                if (scoreValue < 0 ||
                    scoreValue == decimal.MaxValue)
                {
                    throw new InvalidOperationException(
                        $"Evaluation Score của " +
                        $"{suppliers[i].SupplierCode} / " +
                        $"{criteria[j].CriteriaCode} không hợp lệ.");
                }


                decisionMatrix[i, j] =
                    (double)scoreValue;
            }
        }


        // =====================================================
        // 8. CRITERIA TYPES
        // =====================================================

        var criteriaTypes =
            criteria
                .Select(
                    x =>
                        x.CriteriaType?.Trim()
                        ?? string.Empty)
                .ToArray();


        // =====================================================
        // 9. CALCULATE
        // =====================================================

        var calculation =
            _calculationService
                .Calculate(
                    decisionMatrix,
                    weights,
                    criteriaTypes);


        // =====================================================
        // 10. SAVE RESULT
        // =====================================================

        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                // -------------------------------------------------
                // DELETE OLD IDEALS
                // -------------------------------------------------

                var oldIdeals =
                    await _context.TopsisIdealSolutions
                        .Where(
                            x => x.BatchId == batchId)
                        .ToListAsync();

                if (oldIdeals.Any())
                {
                    _context.TopsisIdealSolutions
                        .RemoveRange(oldIdeals);
                }

                // -------------------------------------------------
                // DELETE OLD RANKING
                // -------------------------------------------------

                var oldResults =
                    await _context.TopsisResults
                        .Where(
                            x => x.BatchId == batchId)
                        .ToListAsync();

                if (oldResults.Any())
                {
                    _context.TopsisResults
                        .RemoveRange(oldResults);
                }

                await _context.SaveChangesAsync();

                // -------------------------------------------------
                // SAVE PIS / NIS
                // -------------------------------------------------

                for (var j = 0;
                     j < criteriaCount;
                     j++)
                {
                    var ideal =
                        new TopsisIdealSolution
                        {
                            BatchId =
                                batchId,

                            CriteriaId =
                                criteria[j].CriteriaId,

                            // IMPORTANT:
                            // Entity thực tế dùng Pisvalue
                            Pisvalue =
                                (decimal)
                                Math.Round(
                                    calculation.PISValues[j],
                                    6),

                            // IMPORTANT:
                            // Entity thực tế dùng Nisvalue
                            Nisvalue =
                                (decimal)
                                Math.Round(
                                    calculation.NISValues[j],
                                    6)
                        };

                    _context.TopsisIdealSolutions
                        .Add(ideal);
                }

                // -------------------------------------------------
                // SAVE SUPPLIER RESULTS
                // -------------------------------------------------

                for (var i = 0;
                     i < supplierCount;
                     i++)
                {
                    var distance =
                        calculation.Distances[i];

                    var result =
                        new TopsisResult
                        {
                            BatchId =
                                batchId,

                            SupplierId =
                                suppliers[i].SupplierId,

                            // IMPORTANT:
                            // Entity dùng DistanceToPis
                            DistanceToPis =
                                (decimal)
                                Math.Round(
                                    distance.DistanceToPIS,
                                    6),

                            // IMPORTANT:
                            // Entity dùng DistanceToNis
                            DistanceToNis =
                                (decimal)
                                Math.Round(
                                    distance.DistanceToNIS,
                                    6),

                            ClosenessCoefficient =
                                (decimal)
                                Math.Round(
                                    distance.ClosenessCoefficient,
                                    6),

                            RankNo =
                                calculation.RankByIndex[i],

                            CalculatedAt =
                                DateTime.UtcNow
                        };

                    _context.TopsisResults
                        .Add(result);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        });
    }
}