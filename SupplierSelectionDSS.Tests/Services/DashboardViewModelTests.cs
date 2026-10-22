using System;
using System.Collections.Generic;
using FluentAssertions;
using SupplierSelectionDSS.Models;
using Xunit;

namespace SupplierSelectionDSS.Tests.Services;

public class DashboardViewModelTests
{
    [Fact]
    public void DashboardViewModel_InitializesWithDefaultValues()
    {
        var vm = new DashboardViewModel();

        vm.AvailableBatches.Should().NotBeNull();
        vm.TopTopsisRankings.Should().NotBeNull();
        vm.AhpCriteriaWeights.Should().NotBeNull();
        vm.RecentActivities.Should().NotBeNull();
        vm.TotalSuppliers.Should().Be(0);
        vm.TotalBatches.Should().Be(0);
        vm.TotalProductGroups.Should().Be(0);
        vm.TotalCriteria.Should().Be(0);
        vm.AhpCrValue.Should().BeNull();
        vm.Top1SupplierName.Should().BeNull();
    }

    [Theory]
    [InlineData(10, 8, 80)]
    [InlineData(10, 0, 0)]
    [InlineData(5, 5, 100)]
    [InlineData(0, 0, 0)]
    public void DashboardCurrentBatchViewModel_CalculatesProgressPercent(int total, int scored, int expectedPct)
    {
        var batch = new DashboardCurrentBatchViewModel
        {
            TotalSuppliers = total,
            ScoredSuppliers = scored
        };

        batch.ProgressPercent.Should().Be(expectedPct);
    }

    [Theory]
    [InlineData(1, false, 10)]
    [InlineData(1, true, 20)]
    [InlineData(2, false, 30)]
    [InlineData(2, true, 40)]
    [InlineData(3, false, 50)]
    [InlineData(3, true, 60)]
    [InlineData(4, false, 70)]
    [InlineData(4, true, 80)]
    [InlineData(5, false, 90)]
    [InlineData(5, true, 100)]
    public void DashboardWorkflowProgressDto_CalculatesOverallPercentCorrectly(int step, bool isStepDone, int expectedPercent)
    {
        var workflow = new DashboardWorkflowProgressDto
        {
            CurrentStepNumber = step,
            Step1DataInputDone = step > 1 || isStepDone,
            Step2AhpDone = step > 2 || (step == 2 && isStepDone),
            Step3TopsisDone = step > 3 || (step == 3 && isStepDone),
            Step4ComparisonDone = step > 4 || (step == 4 && isStepDone),
            Step5ApprovalDone = step == 5 && isStepDone
        };

        workflow.OverallPercent.Should().Be(expectedPercent);
    }

    [Fact]
    public void DashboardAhpWeightItemDto_CalculatesPercentageCorrectly()
    {
        var item = new DashboardAhpWeightItemDto
        {
            CriteriaCode = "C01",
            CriteriaName = "Chất lượng sản phẩm",
            WeightValue = 0.3245m
        };

        item.Percentage.Should().Be(32.5);
    }

    [Fact]
    public void DashboardTopsisRankingItemDto_HoldsValuesCorrectly()
    {
        var rank = new DashboardTopsisRankingItemDto
        {
            RankNo = 1,
            SupplierId = 10,
            SupplierCode = "SUP-01",
            SupplierName = "Công ty TNHH Nhựa Tiền Phong",
            ClosenessCoefficient = 0.823456m,
            DistanceToPis = 0.052m,
            DistanceToNis = 0.185m,
            Status = "Active"
        };

        rank.RankNo.Should().Be(1);
        rank.ClosenessCoefficient.Should().Be(0.823456m);
        rank.SupplierName.Should().Contain("Nhựa Tiền Phong");
    }

    [Fact]
    public void DashboardDonutChartDto_MaintainsLabelsAndValues()
    {
        var donut = new DashboardDonutChartDto
        {
            TotalCount = 10,
            Labels = new List<string> { "Hoàn thành", "Đang đánh giá", "Bản nháp", "Đã hủy" },
            Values = new List<int> { 6, 2, 1, 1 },
            Colors = new List<string> { "#16a34a", "#2563eb", "#f59e0b", "#ef4444" }
        };

        donut.TotalCount.Should().Be(10);
        donut.Labels.Should().HaveCount(4);
        donut.Values.Should().HaveCount(4);
        donut.Values.Sum().Should().Be(10);
    }

    [Fact]
    public void DashboardRadarComparisonDto_MaintainsNormalizedAndRawScores()
    {
        var radar = new DashboardRadarComparisonDto
        {
            CriteriaLabels = new List<string> { "Giá thành", "Chất lượng", "Thời gian giao hàng" },
            CriteriaTypes = new List<string> { "Cost", "Benefit", "Cost" },
            CriteriaUnits = new List<string> { "VND", "Điểm", "Ngày" },
            Datasets = new List<DashboardRadarDatasetDto>
            {
                new DashboardRadarDatasetDto
                {
                    SupplierName = "Nhà cung cấp A",
                    Color = "#2563eb",
                    Scores = new List<double> { 100.0, 88.9, 60.0 },
                    RawScores = new List<double> { 120.0, 8.0, 5.0 },
                    FormattedScores = new List<string> { "120 VND", "8 Điểm", "5 Ngày" }
                },
                new DashboardRadarDatasetDto
                {
                    SupplierName = "Nhà cung cấp B",
                    Color = "#10b981",
                    Scores = new List<double> { 80.0, 100.0, 75.0 },
                    RawScores = new List<double> { 150.0, 9.0, 4.0 },
                    FormattedScores = new List<string> { "150 VND", "9 Điểm", "4 Ngày" }
                }
            }
        };

        radar.CriteriaLabels.Should().HaveCount(3);
        radar.CriteriaTypes.Should().ContainInOrder("Cost", "Benefit", "Cost");
        radar.CriteriaUnits.Should().ContainInOrder("VND", "Điểm", "Ngày");
        radar.Datasets.Should().HaveCount(2);

        var suppA = radar.Datasets[0];
        suppA.Scores.Should().AllSatisfy(s => s.Should().BeInRange(0.0, 100.0));
        suppA.Scores[0].Should().Be(100.0); // Cost: lowest price gets 100
        suppA.RawScores[0].Should().Be(120.0);
        suppA.FormattedScores[0].Should().Be("120 VND");

        var suppB = radar.Datasets[1];
        suppB.Scores[1].Should().Be(100.0); // Benefit: highest quality gets 100
        suppB.RawScores[1].Should().Be(9.0);
        suppB.FormattedScores[1].Should().Be("9 Điểm");
    }
}
