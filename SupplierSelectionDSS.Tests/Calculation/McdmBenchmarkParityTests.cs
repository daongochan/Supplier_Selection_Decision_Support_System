using System.Text.Json;
using FluentAssertions;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Calculation;

public class McdmBenchmarkParityTests
{
    private readonly AHPCalculationService _ahp = new();
    private readonly TOPSISCalculationService _topsis = new();

    private static JsonElement LoadBenchmarkRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        var jsonPath = Path.Combine(repoRoot, "tools", "verify", "mcdm_benchmarks.json");
        File.Exists(jsonPath).Should().BeTrue($"Benchmark JSON file must exist at {jsonPath}");
        var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        return doc.RootElement.Clone();
    }

    [Fact]
    public void Ahp_AllScenarios_MatchPythonReferenceJson()
    {
        var root = LoadBenchmarkRoot();

        // 1. ahp_consistent_3x3
        VerifyAhpScenario(
            root.GetProperty("ahp_consistent_3x3"),
            new double[,]
            {
                { 1.0, 2.0, 4.0 },
                { 0.5, 1.0, 2.0 },
                { 0.25, 0.5, 1.0 }
            });

        // 2. ahp_saaty_4x4
        VerifyAhpScenario(
            root.GetProperty("ahp_saaty_4x4"),
            new double[,]
            {
                { 1.0, 3.0, 5.0, 7.0 },
                { 1.0 / 3.0, 1.0, 3.0, 5.0 },
                { 1.0 / 5.0, 1.0 / 3.0, 1.0, 3.0 },
                { 1.0 / 7.0, 1.0 / 5.0, 1.0 / 3.0, 1.0 }
            });

        // 3. ahp_inconsistent_3x3
        VerifyAhpScenario(
            root.GetProperty("ahp_inconsistent_3x3"),
            new double[,]
            {
                { 1.0, 9.0, 1.0 },
                { 1.0 / 9.0, 1.0, 9.0 },
                { 1.0, 1.0 / 9.0, 1.0 }
            });

        // 4. ahp_2x2
        VerifyAhpScenario(
            root.GetProperty("ahp_2x2"),
            new double[,]
            {
                { 1.0, 3.0 },
                { 1.0 / 3.0, 1.0 }
            });
    }

    [Fact]
    public void Topsis_AllScenarios_MatchPythonReferenceJson()
    {
        var root = LoadBenchmarkRoot();

        // 1. topsis_3x4_benchmark
        VerifyTopsisScenario(
            root.GetProperty("topsis_3x4_benchmark"),
            new double[,]
            {
                { 250.0, 8.5, 14.0, 9.0 },
                { 200.0, 7.0, 21.0, 8.0 },
                { 300.0, 9.5, 7.0, 7.5 }
            },
            new[] { 0.35, 0.30, 0.20, 0.15 },
            new[] { "Cost", "Benefit", "Cost", "Benefit" });

        // 2. topsis_tie_1224
        VerifyTopsisScenario(
            root.GetProperty("topsis_tie_1224"),
            new double[,]
            {
                { 10.0, 100.0 },
                { 8.0, 150.0 },
                { 8.0, 150.0 },
                { 5.0, 250.0 }
            },
            new[] { 0.5, 0.5 },
            new[] { "Benefit", "Cost" });

        // 3. topsis_zero_column
        VerifyTopsisScenario(
            root.GetProperty("topsis_zero_column"),
            new double[,]
            {
                { 0.0, 8.0, 15.0 },
                { 0.0, 6.0, 20.0 },
                { 0.0, 9.0, 10.0 }
            },
            new[] { 0.2, 0.4, 0.4 },
            new[] { "Cost", "Benefit", "Cost" });

        // 4. topsis_identical_suppliers
        VerifyTopsisScenario(
            root.GetProperty("topsis_identical_suppliers"),
            new double[,]
            {
                { 10.0, 100.0 },
                { 10.0, 100.0 },
                { 10.0, 100.0 }
            },
            new[] { 0.5, 0.5 },
            new[] { "Benefit", "Cost" });
    }

    private void VerifyAhpScenario(JsonElement expected, double[,] matrix)
    {
        var actual = _ahp.Calculate(matrix);

        actual.N.Should().Be(expected.GetProperty("n").GetInt32());
        actual.IsConsistent.Should().Be(expected.GetProperty("is_consistent_gm").GetBoolean());
        actual.LambdaMax.Should().BeApproximately(expected.GetProperty("lambda_max_gm").GetDouble(), 1e-6);
        actual.CIValue.Should().BeApproximately(expected.GetProperty("ci_gm").GetDouble(), 1e-6);
        actual.RIValue.Should().BeApproximately(expected.GetProperty("ri").GetDouble(), 1e-6);
        actual.CRValue.Should().BeApproximately(expected.GetProperty("cr_gm").GetDouble(), 1e-6);

        var expectedWeights = expected.GetProperty("weights_geometric_mean").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        actual.Weights.Should().HaveCount(expectedWeights.Length);
        for (int i = 0; i < expectedWeights.Length; i++)
        {
            actual.Weights[i].Should().BeApproximately(expectedWeights[i], 1e-6);
        }
    }

    private void VerifyTopsisScenario(JsonElement expected, double[,] decisionMatrix, double[] weights, string[] criteriaTypes)
    {
        var actual = _topsis.Calculate(decisionMatrix, weights, criteriaTypes);

        var m = expected.GetProperty("m_suppliers").GetInt32();
        var n = expected.GetProperty("n_criteria").GetInt32();

        var expectedPis = expected.GetProperty("pis").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        var expectedNis = expected.GetProperty("nis").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        var expectedDPlus = expected.GetProperty("d_plus").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        var expectedDMinus = expected.GetProperty("d_minus").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        var expectedCc = expected.GetProperty("closeness_coefficients").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        var expectedRanks = expected.GetProperty("ranks").EnumerateArray().Select(x => x.GetInt32()).ToArray();

        for (int j = 0; j < n; j++)
        {
            actual.PISValues[j].Should().BeApproximately(expectedPis[j], 1e-6);
            actual.NISValues[j].Should().BeApproximately(expectedNis[j], 1e-6);
        }

        for (int i = 0; i < m; i++)
        {
            actual.Distances[i].DistanceToPIS.Should().BeApproximately(expectedDPlus[i], 1e-6);
            actual.Distances[i].DistanceToNIS.Should().BeApproximately(expectedDMinus[i], 1e-6);
            actual.Distances[i].ClosenessCoefficient.Should().BeApproximately(expectedCc[i], 1e-6);
            actual.RankByIndex[i].Should().Be(expectedRanks[i]);
        }
    }
}
