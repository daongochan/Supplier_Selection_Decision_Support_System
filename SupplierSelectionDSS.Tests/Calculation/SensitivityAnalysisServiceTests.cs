using FluentAssertions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Calculation;

public class SensitivityAnalysisServiceTests
{
    private readonly TOPSISCalculationService _calculationService;
    private readonly SensitivityAnalysisService _service;

    public SensitivityAnalysisServiceTests()
    {
        _calculationService = new TOPSISCalculationService();
        _service = new SensitivityAnalysisService(_calculationService);
    }

    [Fact]
    public void Analyze_WhenTopSupplierIsDominant_Reports100PercentRobust()
    {
        // 3 nhà cung cấp, 3 tiêu chí: S1 vượt trội hoàn toàn trên các tiêu chí Benefit
        var decisionMatrix = new double[,]
        {
            { 90.0, 85.0, 10.0 }, // S1: tốt nhất
            { 50.0, 40.0, 30.0 }, // S2
            { 30.0, 20.0, 50.0 }  // S3
        };

        var baseWeights = new double[] { 0.50, 0.30, 0.20 };
        var criteriaTypes = new string[] { CriteriaType.Benefit, CriteriaType.Benefit, CriteriaType.Cost };

        var criteria = new List<TOPSISCriteriaViewModel>
        {
            new() { CriteriaId = 1, CriteriaCode = "C1", CriteriaName = "Chất lượng", Weight = 0.50m, CriteriaType = CriteriaType.Benefit },
            new() { CriteriaId = 2, CriteriaCode = "C2", CriteriaName = "Thời gian", Weight = 0.30m, CriteriaType = CriteriaType.Benefit },
            new() { CriteriaId = 3, CriteriaCode = "C3", CriteriaName = "Chi phí", Weight = 0.20m, CriteriaType = CriteriaType.Cost }
        };

        var suppliers = new List<TOPSISEvaluationRowViewModel>
        {
            new() { SupplierId = 101, SupplierCode = "NCC-01", SupplierName = "Nhà cung cấp 1" },
            new() { SupplierId = 102, SupplierCode = "NCC-02", SupplierName = "Nhà cung cấp 2" },
            new() { SupplierId = 103, SupplierCode = "NCC-03", SupplierName = "Nhà cung cấp 3" }
        };

        var result = _service.Analyze(
            batchId: 1,
            batchCode: "EB01",
            decisionMatrix: decisionMatrix,
            baseWeights: baseWeights,
            criteriaTypes: criteriaTypes,
            criteria: criteria,
            suppliers: suppliers,
            baseTopSupplierCode: "NCC-01",
            baseTopSupplierName: "Nhà cung cấp 1",
            baseTopSupplierCloseness: 0.95m);

        result.Should().NotBeNull();
        result.IsOverallRobust.Should().BeTrue();
        result.RobustnessPercentage.Should().Be(100.0);
        result.Scenarios.Should().HaveCount(3);
        result.SummaryMessage.Should().Contain("100%");

        foreach (var scenario in result.Scenarios)
        {
            scenario.Runs.Should().HaveCount(4); // -20%, -10%, +10%, +20%
            foreach (var run in scenario.Runs)
            {
                run.IsStable.Should().BeTrue();
                run.TopSupplierCode.Should().Be("NCC-01");
            }
        }
    }

    [Fact]
    public void Analyze_WhenDecisionIsSensitive_DetectsShiftAndIdentifiesSensitiveCriteria()
    {
        // Hai nhà cung cấp rất suýt soát nhau:
        // S1 mạnh về C1 (Benefit), S2 rất mạnh về C2 (Benefit)
        var decisionMatrix = new double[,]
        {
            { 100.0, 10.0 }, // S1: C1 rất cao, C2 thấp
            { 10.0, 100.0 }  // S2: C1 thấp, C2 rất cao
        };

        // Trọng số gốc nghiêng nhẹ về C1 (0.51 vs 0.49) -> S1 thắng sít sao
        var baseWeights = new double[] { 0.51, 0.49 };
        var criteriaTypes = new string[] { CriteriaType.Benefit, CriteriaType.Benefit };

        var criteria = new List<TOPSISCriteriaViewModel>
        {
            new() { CriteriaId = 1, CriteriaCode = "C1", CriteriaName = "Tiêu chí 1", Weight = 0.51m, CriteriaType = CriteriaType.Benefit },
            new() { CriteriaId = 2, CriteriaCode = "C2", CriteriaName = "Tiêu chí 2", Weight = 0.49m, CriteriaType = CriteriaType.Benefit }
        };

        var suppliers = new List<TOPSISEvaluationRowViewModel>
        {
            new() { SupplierId = 1, SupplierCode = "NCC-A", SupplierName = "Nhà cung cấp A" },
            new() { SupplierId = 2, SupplierCode = "NCC-B", SupplierName = "Nhà cung cấp B" }
        };

        var result = _service.Analyze(
            batchId: 2,
            batchCode: "EB02",
            decisionMatrix: decisionMatrix,
            baseWeights: baseWeights,
            criteriaTypes: criteriaTypes,
            criteria: criteria,
            suppliers: suppliers,
            baseTopSupplierCode: "NCC-A",
            baseTopSupplierName: "Nhà cung cấp A",
            baseTopSupplierCloseness: 0.51m);

        result.Should().NotBeNull();
        result.IsOverallRobust.Should().BeFalse();
        result.RobustnessPercentage.Should().BeLessThan(100.0);
        result.SummaryMessage.Should().ContainAny("C1", "C2");
    }

    [Fact]
    public void Analyze_WhenInputsInvalid_ThrowsOrHandlesGracefully()
    {
        var criteria = new List<TOPSISCriteriaViewModel>();
        var suppliers = new List<TOPSISEvaluationRowViewModel>();

        var result = _service.Analyze(
            1, "EB", new double[0, 0], Array.Empty<double>(), Array.Empty<string>(), criteria, suppliers, "A", "Name", 0.5m);

        result.SummaryMessage.Should().Contain("Không đủ điều kiện");
    }
}
