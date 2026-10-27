using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Tools.DemoSeeder;
using Xunit;
using Xunit.Abstractions;

namespace SupplierSelectionDSS.Tests.Integration;

public class DemoDatasetVerificationTests
{
    private readonly ITestOutputHelper _output;

    public DemoDatasetVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("Server=.;Database=SupplierSelectionDSS;Trusted_Connection=True;")]
    [InlineData("Server=localhost;Database=SupplierSelectionDSS;User Id=sa;Password=123;")]
    [InlineData("Server=.;Database=SupplierSelectionDSS_Perf;Trusted_Connection=True;")]
    [InlineData("Server=.;Database=master;Trusted_Connection=True;")]
    [InlineData("Server=.;Database=msdb;Trusted_Connection=True;")]
    [InlineData("Server=.;Database=ProductionDB;Trusted_Connection=True;")]
    public void SafetyGuard_StrictlyRejects_NonDemoDatabases(string prohibitedConnectionString)
    {
        var act = () => DatabaseSafetyGuard.AssertSafeDemoConnection(prohibitedConnectionString);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SAFETY*");
    }

    [Fact]
    public void SafetyGuard_Accepts_DedicatedDemoDatabase()
    {
        var validConnectionString = "Server=.;Database=SupplierSelectionDSS_Demo;Trusted_Connection=True;TrustServerCertificate=True;";
        var act = () => DatabaseSafetyGuard.AssertSafeDemoConnection(validConnectionString);
        act.Should().NotThrow();
    }

    [Fact]
    public void MCDM_DemoAHP_ConsistentMatrices_ProduceValidCR()
    {
        var calculationService = new AHPCalculationService();

        // Matrix for DEMO-B01 (4 criteria: C01, C02, C03, C05)
        // C01 vs C02: 1/2, C01 vs C03: 3, C01 vs C05: 4
        // C02 vs C03: 4, C02 vs C05: 5, C03 vs C05: 2
        var matrixB01 = new double[,]
        {
            { 1.0,    0.5,    3.0,    4.0 },
            { 2.0,    1.0,    4.0,    5.0 },
            { 1.0/3.0, 0.25,   1.0,    2.0 },
            { 0.25,   0.2,    0.5,    1.0 }
        };

        var resultB01 = calculationService.Calculate(matrixB01);
        resultB01.IsConsistent.Should().BeTrue("DEMO-B01 AHP matrix must be consistent");
        resultB01.CRValue.Should().BeLessThanOrEqualTo(0.10, "CR must be <= 0.10");
        resultB01.Weights.Sum().Should().BeApproximately(1.0, 1e-4);

        // Matrix for DEMO-B05 (Intentionally inconsistent: C01 >> C02, C02 >> C04, C04 >> C01)
        var matrixB05 = new double[,]
        {
            { 1.0,    7.0,    1.0/9.0, 5.0 },
            { 1.0/7.0, 1.0,    8.0,    0.2 },
            { 9.0,    1.0/8.0, 1.0,    6.0 },
            { 0.2,    5.0,    1.0/6.0, 1.0 }
        };

        var resultB05 = calculationService.Calculate(matrixB05);
        resultB05.IsConsistent.Should().BeFalse("DEMO-B05 AHP matrix must demonstrate inconsistency warning");
        resultB05.CRValue.Should().BeGreaterThan(0.10, "CR must exceed 0.10 for inconsistent demo scenario");
    }

    [Fact]
    public void MCDM_DemoTOPSIS_CalculatesValidRankingsAndClosenessCoefficients()
    {
        var topsisCalc = new TOPSISCalculationService();

        // 3 suppliers x 4 criteria (C01 Cost, C02 Benefit, C03 Cost, C06 Benefit)
        var decisionMatrix = new double[,]
        {
            { 45000000.0, 9.0, 2.0, 45.0 },
            { 42000000.0, 8.5, 3.0, 30.0 },
            { 48000000.0, 8.8, 2.0, 60.0 }
        };

        var weights = new double[] { 0.35, 0.35, 0.15, 0.15 };
        var criteriaTypes = new string[] { CriteriaType.Cost, CriteriaType.Benefit, CriteriaType.Cost, CriteriaType.Benefit };

        var result = topsisCalc.Calculate(decisionMatrix, weights, criteriaTypes);

        result.Should().NotBeNull();
        result.Distances.Should().HaveCount(3);
        result.RankByIndex.Should().HaveCount(3);

        foreach (var d in result.Distances)
        {
            d.ClosenessCoefficient.Should().BeInRange(0.0, 1.0, "Closeness coefficient must be normalized in [0, 1]");
        }

        result.RankByIndex.Should().Contain(1, "There must be a top-ranked alternative");
    }

    [Fact]
    public async Task LiveDemoDatabase_PassesAllVerificationChecks_WhenDatabaseAvailable()
    {
        var connStr = Environment.GetEnvironmentVariable("DSS_DEMO_CONNECTION_STRING")
            ?? "Server=.;Database=SupplierSelectionDSS_Demo;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        // Check if SQL Server is accessible and SupplierSelectionDSS_Demo exists
        try
        {
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Skipping live database verification: SQL Server or database not reachable. {ex.Message}");
            return;
        }

        // Build DI container for verifier
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlServer(connStr));
        services.AddScoped<BatchWorkflowReadinessService>();
        services.AddScoped<TOPSISCalculationService>();
        services.AddScoped<SensitivityAnalysisService>();
        services.AddScoped<TOPSISService>();

        var repoRoot = FindRepoRoot() ?? Directory.GetCurrentDirectory();
        services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(new DemoHostEnvironment(repoRoot));
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Reports:Directory", System.IO.Path.Combine(repoRoot, "App_Data", "reports") }
        });
        services.AddSingleton<IConfiguration>(configBuilder.Build());
        services.AddScoped<AuditLogService>();
        services.AddScoped<ReportService>();

        var sp = services.BuildServiceProvider();
        var verifier = new DemoDataVerifier(sp, repoRoot);

        var verifyResult = await verifier.VerifyAsync("Demo@123456");

        foreach (var pass in verifyResult.ChecksPassed)
        {
            _output.WriteLine($"[PASS] {pass}");
        }

        foreach (var fail in verifyResult.Violations)
        {
            _output.WriteLine($"[FAIL] {fail}");
        }

        verifyResult.Success.Should().BeTrue("Demo dataset verification in database must pass 100%");
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (System.IO.File.Exists(System.IO.Path.Combine(dir, "SupplierSelectionDSS.csproj")))
            {
                return dir;
            }
            dir = System.IO.Path.GetDirectoryName(dir);
        }
        return null;
    }
}
