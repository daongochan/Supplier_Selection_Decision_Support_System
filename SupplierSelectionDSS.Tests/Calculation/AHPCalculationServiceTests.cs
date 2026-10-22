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
    public void Calculate_EmptyMatrix_Throws()
    {
        var matrix = new double[0, 0];
        Action act = () => _service.Calculate(matrix);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rỗng*");
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
            { 1.0 / 3, 1 }
        };

        Action act = () => _service.Calculate(matrix);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*đường chéo*");
    }

    [Fact]
    public void Calculate_NonPositiveValue_Throws()
    {
        var matrix = new double[2, 2]
        {
            { 1, 0 },
            { 0, 1 }
        };

        Action act = () => _service.Calculate(matrix);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*lớn hơn 0*");
    }

    [Fact]
    public void Calculate_NonReciprocal_Throws()
    {
        var matrix = new double[2, 2]
        {
            { 1, 3 },
            { 0.5, 1 } // Đúng ra phải là 1/3 = 0.3333
        };

        Action act = () => _service.Calculate(matrix);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*nghịch đảo*");
    }

    [Fact]
    public void Calculate_NonFiniteValue_Throws()
    {
        var matrix = new double[2, 2]
        {
            { 1, double.PositiveInfinity },
            { 0, 1 }
        };

        Action act = () => _service.Calculate(matrix);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*hữu hạn*");
    }

    [Fact]
    public void Calculate_Consistent2x2_ShouldBeConsistent_AndWeightsSumToOne()
    {
        var matrix = new double[2, 2]
        {
            { 1, 3 },
            { 1.0 / 3, 1 }
        };

        var result = _service.Calculate(matrix);

        result.N.Should().Be(2);
        result.IsConsistent.Should().BeTrue();
        result.CRValue.Should().Be(0.0);
        result.Weights.Should().HaveCount(2);
        result.Weights.Sum().Should().BeApproximately(1.0, 1e-6);
        result.Weights[0].Should().BeApproximately(0.75, 1e-4);
        result.Weights[1].Should().BeApproximately(0.25, 1e-4);
    }

    [Fact]
    public void Calculate_Consistent3x3_ShouldBeConsistent_CRZero()
    {
        var matrix = new double[3, 3]
        {
            { 1.0, 2.0, 4.0 },
            { 0.5, 1.0, 2.0 },
            { 0.25, 0.5, 1.0 }
        };

        var result = _service.Calculate(matrix);

        result.N.Should().Be(3);
        result.IsConsistent.Should().BeTrue();
        result.CRValue.Should().BeApproximately(0.0, 1e-4);
        result.Weights.Sum().Should().BeApproximately(1.0, 1e-6);

        // Đối chiếu với kiểm thử Python
        result.Weights[0].Should().BeApproximately(0.57142857, 1e-4);
        result.Weights[1].Should().BeApproximately(0.28571429, 1e-4);
        result.Weights[2].Should().BeApproximately(0.14285714, 1e-4);
    }

    [Fact]
    public void Calculate_StandardSaaty4x4_MatchesPythonEigenvectorWithinTolerance()
    {
        // Ma trận 4x4 chuẩn trong giáo trình của Thomas Saaty
        var matrix = new double[4, 4]
        {
            { 1.0,       3.0,       5.0, 7.0 },
            { 1.0 / 3.0, 1.0,       3.0, 5.0 },
            { 1.0 / 5.0, 1.0 / 3.0, 1.0, 3.0 },
            { 1.0 / 7.0, 1.0 / 5.0, 1.0 / 3.0, 1.0 }
        };

        var result = _service.Calculate(matrix);

        result.N.Should().Be(4);
        result.IsConsistent.Should().BeTrue();
        result.CRValue.Should().BeApproximately(0.0433, 1e-3); // CR < 0.10

        // Đối chiếu với trọng số Geometric Mean và Eigenvector tính từ NumPy
        result.Weights[0].Should().BeApproximately(0.5638, 1e-3);
        result.Weights[1].Should().BeApproximately(0.2634, 1e-3);
        result.Weights[2].Should().BeApproximately(0.1178, 1e-3);
        result.Weights[3].Should().BeApproximately(0.0550, 1e-3);

        // Chênh lệch giữa Geometric Mean và Principal Eigenvector của NumPy luôn < 0.0015
        var numpyEigenweights = new[] { 0.5650, 0.2622, 0.1175, 0.0553 };
        for (int i = 0; i < 4; i++)
        {
            Math.Abs(result.Weights[i] - numpyEigenweights[i]).Should().BeLessThan(0.002);
        }
    }

    [Fact]
    public void Calculate_Inconsistent3x3_ShouldNotBeConsistent()
    {
        var matrix = new double[3, 3]
        {
            { 1.0, 9.0, 1.0 },
            { 1.0 / 9.0, 1.0, 9.0 },
            { 1.0, 1.0 / 9.0, 1.0 }
        };

        var result = _service.Calculate(matrix);

        result.IsConsistent.Should().BeFalse();
        result.CRValue.Should().BeGreaterThan(0.10);
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
        result.CRValue.Should().BeApproximately(0.0, 1e-4);
        result.Weights.Sum().Should().BeApproximately(1.0, 1e-4);
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