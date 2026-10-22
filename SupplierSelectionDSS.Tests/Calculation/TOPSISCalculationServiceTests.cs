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
    public void Calculate_LessThanTwoSuppliers_Throws()
    {
        var singleSupplierMatrix = new double[1, 2] { { 10, 200 } };
        var weights = new[] { 0.5, 0.5 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        Action act = () => _service.Calculate(singleSupplierMatrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tối thiểu 2 nhà cung cấp*");
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
    public void Calculate_CriteriaTypesCountMismatch_Throws()
    {
        var matrix = new double[2, 2] { { 1, 2 }, { 3, 4 } };
        var weights = new[] { 0.5, 0.5 };
        var types = new[] { CriteriaType.Benefit }; // sai số lượng

        Action act = () => _service.Calculate(matrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CriteriaType*");
    }

    [Fact]
    public void Calculate_EmptyCriteria_Throws()
    {
        var matrix = new double[2, 0];
        var weights = Array.Empty<double>();
        var types = Array.Empty<string>();

        Action act = () => _service.Calculate(matrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rỗng tiêu chí*");
    }

    [Fact]
    public void Calculate_EmptyWeights_Throws()
    {
        var matrix = new double[2, 2] { { 1, 2 }, { 3, 4 } };
        var weights = Array.Empty<double>();
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        Action act = () => _service.Calculate(matrix, weights, types);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Calculate_NegativeWeight_Throws()
    {
        var matrix = new double[2, 2] { { 1, 2 }, { 3, 4 } };
        var weights = new[] { -0.5, 1.5 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        Action act = () => _service.Calculate(matrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*lớn hơn 0*");
    }

    [Fact]
    public void Calculate_EmptyCriteriaType_Throws()
    {
        var matrix = new double[2, 2] { { 1, 2 }, { 3, 4 } };
        var weights = new[] { 0.5, 0.5 };
        var types = new[] { "  ", CriteriaType.Cost };

        Action act = () => _service.Calculate(matrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*không được để trống*");
    }

    [Fact]
    public void Calculate_NonFiniteMatrix_Throws()
    {
        var matrix = new double[2, 2] { { double.NaN, 2 }, { 3, 4 } };
        var weights = new[] { 0.5, 0.5 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        Action act = () => _service.Calculate(matrix, weights, types);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*hữu hạn*");
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

        result.RankByIndex[0].Should().Be(1);
        result.RankByIndex[1].Should().Be(2);

        result.Distances[0].ClosenessCoefficient
            .Should().BeGreaterThan(result.Distances[1].ClosenessCoefficient);
    }

    [Fact]
    public void Calculate_AllZeroColumn_NormalizesToZero_DoesNotThrow()
    {
        // Cột 0 toàn 0 (ví dụ: tỷ lệ lỗi của tất cả NCC đều bằng 0)
        var decisionMatrix = new double[3, 3]
        {
            { 0, 8.0, 15.0 },
            { 0, 6.0, 20.0 },
            { 0, 9.0, 10.0 }
        };

        var weights = new[] { 0.2, 0.4, 0.4 };
        var types = new[] { CriteriaType.Cost, CriteriaType.Benefit, CriteriaType.Cost };

        var result = _service.Calculate(decisionMatrix, weights, types);

        // Cột 0 được chuẩn hóa bằng 0 và ghi nhận vào ZeroVarianceCriteriaIndices
        result.ZeroVarianceCriteriaIndices.Should().Contain(0);
        result.NormalizedMatrix[0, 0].Should().Be(0.0);
        result.NormalizedMatrix[1, 0].Should().Be(0.0);
        result.NormalizedMatrix[2, 0].Should().Be(0.0);

        // Thuật toán vẫn tính toán bình thường
        result.Distances.Should().HaveCount(3);
        result.RankByIndex.Should().Equal(2, 3, 1);
    }

    [Fact]
    public void Calculate_TiedSuppliers_AppliesStandardCompetitionRanking_1224()
    {
        var decisionMatrix = new double[4, 2]
        {
            { 10.0, 100.0 }, // SUP 1: Best (CC = 1.0)
            { 8.0,  150.0 }, // SUP 2: Tied with 3
            { 8.0,  150.0 }, // SUP 3: Tied with 2
            { 5.0,  250.0 }  // SUP 4: Worst (CC = 0.0)
        };

        var weights = new[] { 0.5, 0.5 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        var result = _service.Calculate(decisionMatrix, weights, types);

        result.HasTies.Should().BeTrue();
        result.RankByIndex.Should().Equal(1, 2, 2, 4);
    }

    [Fact]
    public void Calculate_AllSuppliersIdentical_FlagsIndistinguishable_AllRank1()
    {
        var decisionMatrix = new double[3, 2]
        {
            { 8.0, 150.0 },
            { 8.0, 150.0 },
            { 8.0, 150.0 }
        };

        var weights = new[] { 0.5, 0.5 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        var result = _service.Calculate(decisionMatrix, weights, types);

        result.IsIndistinguishable.Should().BeTrue();
        result.HasTies.Should().BeTrue();
        result.RankByIndex.Should().Equal(1, 1, 1);
        result.Distances.Should().OnlyContain(d => d.ClosenessCoefficient == 0.0);
    }

    [Fact]
    public void Calculate_3x4Benchmark_MatchesIndependentPythonCalculation()
    {
        // 3 Nhà cung cấp × 4 Tiêu chí (Giá thành Cost, Chất lượng Benefit, Giao hàng Cost, Năng lực Benefit)
        var decisionMatrix = new double[3, 4]
        {
            { 250.0, 8.5, 14.0, 9.0 }, // SUP 1
            { 200.0, 7.0, 21.0, 8.0 }, // SUP 2
            { 300.0, 9.5, 7.0,  7.5 }  // SUP 3
        };

        var weights = new[] { 0.35, 0.30, 0.20, 0.15 };
        var types = new[] { CriteriaType.Cost, CriteriaType.Benefit, CriteriaType.Cost, CriteriaType.Benefit };

        var result = _service.Calculate(decisionMatrix, weights, types);

        // Đối chiếu với số liệu độc lập từ verify_mcdm.py
        result.RankByIndex.Should().Equal(2, 3, 1);

        result.Distances[0].ClosenessCoefficient.Should().BeApproximately(0.518626, 1e-4);
        result.Distances[1].ClosenessCoefficient.Should().BeApproximately(0.401522, 1e-4);
        result.Distances[2].ClosenessCoefficient.Should().BeApproximately(0.593386, 1e-4);

        result.PISValues[0].Should().BeApproximately(0.159545, 1e-4);
        result.PISValues[1].Should().BeApproximately(0.195970, 1e-4);
        result.PISValues[2].Should().BeApproximately(0.053452, 1e-4);
        result.PISValues[3].Should().BeApproximately(0.095163, 1e-4);
    }

    [Fact]
    public void Calculate_ScaleInvariance_MultiplyingCriterionColumn_DoesNotAffectResults()
    {
        var matrix1 = new double[3, 2]
        {
            { 8, 300 },
            { 7, 280 },
            { 9, 320 }
        };

        // Nhân cột thứ hai (giá trị 300, 280, 320) với 1000 (đổi từ nghìn sang triệu)
        var matrix2 = new double[3, 2]
        {
            { 8, 300_000 },
            { 7, 280_000 },
            { 9, 320_000 }
        };

        var weights = new[] { 0.6, 0.4 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        var res1 = _service.Calculate(matrix1, weights, types);
        var res2 = _service.Calculate(matrix2, weights, types);

        // Chuẩn hóa vector triệt tiêu hệ số nhân dương: kết quả CC và thứ hạng phải giống hệt
        for (int i = 0; i < 3; i++)
        {
            res2.Distances[i].ClosenessCoefficient
                .Should().BeApproximately(res1.Distances[i].ClosenessCoefficient, 1e-6);
            res2.RankByIndex[i].Should().Be(res1.RankByIndex[i]);
        }
    }

    [Fact]
    public void Calculate_RowPermutationInvariance_DoesNotChangeSupplierCC()
    {
        var matrixOriginal = new double[3, 2]
        {
            { 8, 300 }, // Supplier A
            { 7, 280 }, // Supplier B
            { 9, 320 }  // Supplier C
        };

        // Đổi thứ tự hàng: C, A, B
        var matrixPermuted = new double[3, 2]
        {
            { 9, 320 }, // Supplier C
            { 8, 300 }, // Supplier A
            { 7, 280 }  // Supplier B
        };

        var weights = new[] { 0.6, 0.4 };
        var types = new[] { CriteriaType.Benefit, CriteriaType.Cost };

        var resOrig = _service.Calculate(matrixOriginal, weights, types);
        var resPerm = _service.Calculate(matrixPermuted, weights, types);

        // Supplier C: orig index 2, perm index 0
        resPerm.Distances[0].ClosenessCoefficient
            .Should().BeApproximately(resOrig.Distances[2].ClosenessCoefficient, 1e-6);

        // Supplier A: orig index 0, perm index 1
        resPerm.Distances[1].ClosenessCoefficient
            .Should().BeApproximately(resOrig.Distances[0].ClosenessCoefficient, 1e-6);

        // Supplier B: orig index 1, perm index 2
        resPerm.Distances[2].ClosenessCoefficient
            .Should().BeApproximately(resOrig.Distances[1].ClosenessCoefficient, 1e-6);
    }

    [Fact]
    public void Calculate_VeryLargeValues_DoesNotOverflow()
    {
        // Thử nghiệm với các giá trị cực lớn (~ 1e12 VND)
        var decisionMatrix = new double[2, 2]
        {
            { 1e12, 10 },
            { 2e12, 20 }
        };

        var weights = new[] { 0.5, 0.5 };
        var types = new[] { CriteriaType.Cost, CriteriaType.Benefit };

        var result = _service.Calculate(decisionMatrix, weights, types);

        result.Distances.Should().HaveCount(2);
        result.Distances[0].ClosenessCoefficient.Should().BeInRange(0.0, 1.0);
        result.Distances[1].ClosenessCoefficient.Should().BeInRange(0.0, 1.0);
    }
}