using FluentAssertions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Calculation;

public class TOPSISCalculationServiceTests
{
    private readonly TOPSISCalculationService _service = new();

    [Fact]
    public void Calculate_NullInputs_ThrowArgumentNullException()
    {
        var matrix = new double[2, 2] { { 1, 2 }, { 3, 4 } };
        var weights = new[] { 0.5, 0.5 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        Assert.Throws<ArgumentNullException>(() =>
            _service.Calculate(null!, weights, types));

        Assert.Throws<ArgumentNullException>(() =>
            _service.Calculate(matrix, null!, types));

        Assert.Throws<ArgumentNullException>(() =>
            _service.Calculate(matrix, weights, null!));
    }

    [Fact]
    public void Calculate_WeightCountMismatch_Throws()
    {
        var matrix = new double[2, 2] { { 1, 2 }, { 3, 4 } };
        var weights = new[] { 1.0 }; // sai số lượng
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        Action act = () => _service.Calculate(matrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*trọng số*");
    }

    [Fact]
    public void Calculate_WeightsNotSumToOne_Throws()
    {
        var matrix = new double[2, 2] { { 1, 2 }, { 3, 4 } };
        var weights = new[] { 0.7, 0.5 }; // tổng = 1.2
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        Action act = () => _service.Calculate(matrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*xấp xỉ 1*");
    }

    [Fact]
    public void Calculate_InvalidCriteriaType_Throws()
    {
        var matrix = new double[2, 2] { { 1, 2 }, { 3, 4 } };
        var weights = new[] { 0.5, 0.5 };
        var types = new[] { "Benefit", "InvalidType" };

        Action act = () => _service.Calculate(matrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Benefit*Cost*");
    }

    [Fact]
    public void Calculate_ValidInput_ReturnsValidRankingAndCC()
    {
        // 3 supplier, 2 criteria
        var decisionMatrix = new double[3, 2]
        {
            { 8, 300 },  // A
            { 7, 280 },  // B
            { 9, 320 }   // C
        };

        var weights = new[] { 0.6, 0.4 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        var result = _service.Calculate(decisionMatrix, weights, types);

        result.Distances.Should().HaveCount(3);
        result.RankByIndex.Should().HaveCount(3);
        result.PISValues.Should().HaveCount(2);
        result.NISValues.Should().HaveCount(2);

        // Closeness Coefficient trong [0, 1]
        foreach (var d in result.Distances)
        {
            d.ClosenessCoefficient.Should().BeInRange(0.0, 1.0);
            d.DistanceToPIS.Should().BeGreaterThanOrEqualTo(0);
            d.DistanceToNIS.Should().BeGreaterThanOrEqualTo(0);
        }

        // Rank phải là 1, 2, 3
        result.RankByIndex.OrderBy(x => x)
            .Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Calculate_HigherBenefitLowerCost_ShouldRankBetter()
    {
        // Supplier 0: tốt hơn rõ (điểm cao, chi phí thấp)
        var decisionMatrix = new double[2, 2]
        {
            { 10, 100 },  // tốt
            { 5,  200 }   // kém
        };

        var weights = new[] { 0.5, 0.5 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        var result = _service.Calculate(decisionMatrix, weights, types);

        // Supplier 0 phải rank 1
        result.RankByIndex[0].Should().Be(1);
        result.RankByIndex[1].Should().Be(2);

        result.Distances[0].ClosenessCoefficient
            .Should().BeGreaterThan(result.Distances[1].ClosenessCoefficient);
    }

    [Fact]
    public void Calculate_AllZeroColumn_Throws()
    {
        var decisionMatrix = new double[2, 2]
        {
            { 0, 10 },
            { 0, 20 }
        };

        var weights = new[] { 0.5, 0.5 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Benefit };

        Action act = () => _service.Calculate(decisionMatrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*bằng 0*");
    }
}