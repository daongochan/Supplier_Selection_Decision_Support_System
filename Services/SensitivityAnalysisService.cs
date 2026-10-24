using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Models;

namespace SupplierSelectionDSS.Services;

/// <summary>
/// Dịch vụ phân tích độ nhạy (Sensitivity Analysis) cho hệ thống hỗ trợ ra quyết định chọn nhà cung cấp.
/// Đánh giá độ vững bền của kết quả xếp hạng TOPSIS khi trọng số AHP của từng tiêu chí thay đổi ±10% và ±20%.
/// </summary>
public class SensitivityAnalysisService
{
    private readonly TOPSISCalculationService _calculationService;
    private readonly ILogger<SensitivityAnalysisService> _logger;

    /// <summary>
    /// Các mức độ biến thiên trọng số tiêu chí (%).
    /// </summary>
    public static readonly int[] DeltaPercentages = [-20, -10, 10, 20];

    public SensitivityAnalysisService(
        TOPSISCalculationService calculationService,
        ILogger<SensitivityAnalysisService>? logger = null)
    {
        _calculationService = calculationService;
        _logger = logger ?? NullLogger<SensitivityAnalysisService>.Instance;
    }

    /// <summary>
    /// Thực hiện phân tích độ nhạy trên ma trận quyết định và vector trọng số gốc.
    /// </summary>
    public SensitivityAnalysisResultViewModel Analyze(
        int batchId,
        string batchCode,
        double[,] decisionMatrix,
        double[] baseWeights,
        string[] criteriaTypes,
        IReadOnlyList<TOPSISCriteriaViewModel> criteria,
        IReadOnlyList<TOPSISEvaluationRowViewModel> suppliers,
        string baseTopSupplierCode,
        string baseTopSupplierName,
        decimal baseTopSupplierCloseness)
    {
        ArgumentNullException.ThrowIfNull(decisionMatrix);
        ArgumentNullException.ThrowIfNull(baseWeights);
        ArgumentNullException.ThrowIfNull(criteriaTypes);
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(suppliers);

        var criteriaCount = criteria.Count;
        var supplierCount = suppliers.Count;

        var result = new SensitivityAnalysisResultViewModel
        {
            BatchId = batchId,
            BatchCode = batchCode,
            BaseTopSupplierCode = baseTopSupplierCode,
            BaseTopSupplierName = baseTopSupplierName,
            BaseTopSupplierCloseness = baseTopSupplierCloseness
        };

        _logger.LogInformation(
            "Bắt đầu phân tích độ nhạy cho đợt {BatchCode} (ID: {BatchId}) với {CriteriaCount} tiêu chí và {SupplierCount} nhà cung cấp.",
            batchCode,
            batchId,
            criteriaCount,
            supplierCount);

        if (criteriaCount < 2 || supplierCount < 2 || string.IsNullOrWhiteSpace(baseTopSupplierCode))
        {
            result.SummaryMessage = "Minimum conditions not met to perform sensitivity analysis.";
            _logger.LogWarning("Không đủ điều kiện tối thiểu để thực hiện phân tích độ nhạy cho đợt {BatchCode}.", batchCode);
            return result;
        }

        var totalRuns = 0;
        var stableRuns = 0;
        var sensitiveCriteria = new HashSet<string>();

        for (var j = 0; j < criteriaCount; j++)
        {
            var criterion = criteria[j];
            var baseW = baseWeights[j];

            var scenario = new CriterionSensitivityScenarioViewModel
            {
                CriteriaId = criterion.CriteriaId,
                CriteriaCode = criterion.CriteriaCode,
                CriteriaName = criterion.CriteriaName,
                BaseWeight = (decimal)Math.Round(baseW, 6)
            };

            foreach (var delta in DeltaPercentages)
            {
                totalRuns++;

                // 1. Tính trọng số mới cho tiêu chí j
                var newWj = baseW * (1.0 + (delta / 100.0));
                newWj = Math.Clamp(newWj, 0.001, 0.999);

                // 2. Tái phân phối phần trọng số còn lại cho các tiêu chí khác (k != j) để tổng luôn bằng 1.0
                var perturbedWeights = new double[criteriaCount];
                perturbedWeights[j] = newWj;

                var remainingBase = 1.0 - baseW;
                var remainingNew = 1.0 - newWj;

                for (var k = 0; k < criteriaCount; k++)
                {
                    if (k == j) continue;

                    if (remainingBase > 1e-9)
                    {
                        perturbedWeights[k] = baseWeights[k] * (remainingNew / remainingBase);
                    }
                    else
                    {
                        perturbedWeights[k] = remainingNew / (criteriaCount - 1);
                    }
                }

                // 3. Chạy thuật toán TOPSIS với bộ trọng số mới
                var calcResult = _calculationService.Calculate(decisionMatrix, perturbedWeights, criteriaTypes);

                // 4. Tìm nhà cung cấp đứng đầu (RankNo = 1)
                var topSupplierIndex = -1;
                var topRank = int.MaxValue;
                for (var i = 0; i < supplierCount; i++)
                {
                    if (calcResult.RankByIndex[i] < topRank)
                    {
                        topRank = calcResult.RankByIndex[i];
                        topSupplierIndex = i;
                    }
                }

                if (topSupplierIndex < 0)
                {
                    topSupplierIndex = 0;
                }

                var topSupplier = suppliers[topSupplierIndex];
                var topDistance = calcResult.Distances[topSupplierIndex];
                var isStable = string.Equals(topSupplier.SupplierCode, baseTopSupplierCode, StringComparison.OrdinalIgnoreCase);

                if (isStable)
                {
                    stableRuns++;
                }
                else
                {
                    sensitiveCriteria.Add(criterion.CriteriaCode);
                }

                scenario.Runs.Add(new SensitivityRunViewModel
                {
                    DeltaPercent = delta,
                    NewWeight = (decimal)Math.Round(newWj, 6),
                    TopSupplierCode = topSupplier.SupplierCode,
                    TopSupplierName = topSupplier.SupplierName,
                    TopSupplierCloseness = (decimal)Math.Round(topDistance.ClosenessCoefficient, 6),
                    IsStable = isStable
                });
            }

            result.Scenarios.Add(scenario);
        }

        result.IsOverallRobust = (stableRuns == totalRuns && totalRuns > 0);
        result.RobustnessPercentage = totalRuns > 0
            ? Math.Round(((double)stableRuns / totalRuns) * 100.0, 1)
            : 0;

        if (result.IsOverallRobust)
        {
            result.SummaryMessage = $"The top-ranked supplier ({baseTopSupplierCode} - {baseTopSupplierName}) maintains position #1 across 100% of weight variation scenarios (±10%, ±20%), demonstrating high decision model robustness and reliability.";
        }
        else
        {
            var sensitiveList = string.Join(", ", sensitiveCriteria);
            result.SummaryMessage = $"The ranking exhibits {result.RobustnessPercentage:0.0}% robustness. Position #1 shifts under weight variations for the following criteria: {sensitiveList}. These criteria should be carefully reviewed prior to final decision approval.";
        }

        _logger.LogInformation(
            "Hoàn tất phân tích độ nhạy cho đợt {BatchCode}: Độ vững bền = {Robustness:0.0}%, Tổng kịch bản = {TotalRuns}.",
            batchCode,
            result.RobustnessPercentage,
            totalRuns);

        return result;
    }
}
