using FluentAssertions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Calculation;

public class AhpTopsisIntegrationTests
{
    private readonly AHPCalculationService _ahpService = new();
    private readonly TOPSISCalculationService _topsisService = new();

    [Fact]
    public void AhpWeights_FedDirectlyIntoTopsis_MaintainsCriteriaOrderAndProducesExpectedRanking()
    {
        // 1. AHP Pairwise Matrix cho 3 tiêu chí:
        // C1: Chi phí (Cost)
        // C2: Chất lượng (Benefit)
        // C3: Thời gian giao hàng (Cost)
        // Giả sử Chất lượng (C2) quan trọng gấp 3 lần Chi phí (C1), Chi phí (C1) gấp 2 lần Thời gian (C3)
        var ahpMatrix = new double[3, 3]
        {
            //        C1        C2       C3
            /* C1 */ { 1.0,     1.0/3.0, 2.0 },
            /* C2 */ { 3.0,     1.0,     5.0 },
            /* C3 */ { 1.0/2.0, 1.0/5.0, 1.0 }
        };

        var ahpResult = _ahpService.Calculate(ahpMatrix);

        ahpResult.IsConsistent.Should().BeTrue();
        ahpResult.CRValue.Should().BeLessThan(0.10);
        ahpResult.Weights.Should().HaveCount(3);

        // Thứ tự trọng số kỳ vọng: C2 (Chất lượng) > C1 (Chi phí) > C3 (Giao hàng)
        ahpResult.Weights[1].Should().BeGreaterThan(ahpResult.Weights[0]);
        ahpResult.Weights[0].Should().BeGreaterThan(ahpResult.Weights[2]);

        // 2. Ma trận quyết định của 3 Nhà cung cấp cho đúng 3 tiêu chí theo thứ tự [C1, C2, C3]
        var criteriaTypes = new[] { CriteriaType.Cost, CriteriaType.Benefit, CriteriaType.Cost };
        var decisionMatrix = new double[3, 3]
        {
            //         C1 (Cost)  C2 (Benefit)  C3 (Cost)
            /* SUP1 */ { 100.0,    9.0,          15.0 }, // Chất lượng cao nhất, chi phí thấp nhất
            /* SUP2 */ { 150.0,    7.5,          20.0 }, // Trung bình
            /* SUP3 */ { 200.0,    6.0,          25.0 }  // Kém nhất
        };

        // 3. Đưa trực tiếp trọng số AHP vào TOPSIS
        var topsisResult = _topsisService.Calculate(decisionMatrix, ahpResult.Weights, criteriaTypes);

        // Kiểm tra tính hợp lệ
        topsisResult.RankByIndex.Should().Equal(1, 2, 3);
        topsisResult.Distances[0].ClosenessCoefficient.Should().BeGreaterThan(topsisResult.Distances[1].ClosenessCoefficient);
        topsisResult.Distances[1].ClosenessCoefficient.Should().BeGreaterThan(topsisResult.Distances[2].ClosenessCoefficient);

        // Normalized matrix đúng kích thước 3x3
        topsisResult.NormalizedMatrix.GetLength(0).Should().Be(3);
        topsisResult.NormalizedMatrix.GetLength(1).Should().Be(3);
    }
}
