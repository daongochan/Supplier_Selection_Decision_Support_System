using FluentAssertions;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Calculation;

public class AHPCalculationServiceTests
{
    private readonly AHPCalculationService _service = new();

    [Fact]
    public void Calculate_NullMatrix_ThrowsArgumentNullException()
    {
        Action act = () => _service.Calculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Calculate_NonSquareMatrix_Throws()
    {
        var matrix = new double[2, 3]
        {
            { 1, 2, 3 },
            { 0.5, 1, 2 }
        };

        Action act = () => _service.Calculate(matrix);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*vuông*");
    }

    [Fact]
    public void Calculate_DiagonalNotOne_Throws()
    {
        var matrix = new double[2, 2]
        {
            { 2, 3 },
            { 1.0/3, 1 }
        };

        Action act = () => _service.Calculate(matrix);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*đường chéo*");
    }

    [Fact]
    public void Calculate_Consistent2x2_ShouldBeConsistent_AndWeightsSumToOne()
    {
        // A so với B = 3
        var matrix = new double[2, 2]
        {
            { 1, 3 },
            { 1.0/3, 1 }
        };

        var result = _service.Calculate(matrix);

        result.N.Should().Be(2);
        result.IsConsistent.Should().BeTrue();
        result.CRValue.Should().BeLessThan(0.1);
        result.Weights.Should().HaveCount(2);
        result.Weights.Sum().Should().BeApproximately(1.0, 0.0001);
        result.Weights.Should().OnlyContain(w => w > 0);
        // Tiêu chí 1 quan trọng hơn → trọng số lớn hơn
        result.Weights[0].Should().BeGreaterThan(result.Weights[1]);
    }

    [Fact]
    public void Calculate_Consistent3x3_ShouldBeConsistent()
    {
        // Ma trận nhất quán phổ biến
        var matrix = new double[3, 3]
        {
            { 1,     2,     3 },
            { 0.5,   1,     2 },
            { 1.0/3, 0.5,   1 }
        };

        var result = _service.Calculate(matrix);

        result.N.Should().Be(3);
        result.IsConsistent.Should().BeTrue();
        result.CRValue.Should().BeLessThan(0.1);
        result.Weights.Sum().Should().BeApproximately(1.0, 0.0001);
        result.LambdaMax.Should().BeGreaterThan(3.0 - 0.1);
    }

    [Fact]
    public void Calculate_Inconsistent3x3_ShouldNotBeConsistent()
    {
        // Cố ý làm mất tính nhất quán
        var matrix = new double[3, 3]
        {
            { 1, 9, 1 },
            { 1.0/9, 1, 9 },
            { 1, 1.0/9, 1 }
        };

        var result = _service.Calculate(matrix);

        result.IsConsistent.Should().BeFalse();
        result.CRValue.Should().BeGreaterThanOrEqualTo(0.1);
    }

    [Fact]
    public void Calculate_15Criteria_Succeeds()
    {
        var n = 15;
        var matrix = new double[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                matrix[i, j] = 1.0;
            }
        }

        var result = _service.Calculate(matrix);
        result.N.Should().Be(15);
        result.IsConsistent.Should().BeTrue();
        result.CRValue.Should().BeApproximately(0.0, 0.0001);
        result.Weights.Sum().Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void Calculate_MoreThan15Criteria_Throws()
    {
        var n = 16;
        var matrix = new double[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                matrix[i, j] = i == j ? 1.0 : (i < j ? 2.0 : 0.5);
            }
        }

        Action act = () => _service.Calculate(matrix);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*15 tiêu chí*");
    }
}