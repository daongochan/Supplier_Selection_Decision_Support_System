using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Services;
using Xunit;
using Xunit.Abstractions;

namespace SupplierSelectionDSS.Tests.Services;

[Collection("SqlServerDatabaseCollection")]
[Trait("Category", "SqlServer")]
public class ReadinessQuerySqlTest
{
    private readonly ITestOutputHelper _output;

    public ReadinessQuerySqlTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetReadinessAsync_ExecutesFastOnSqlServer()
    {
        var connStr = "Server=lpc:.;Database=SupplierSelectionDSS;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=15";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connStr)
            .Options;

        using var context = new ApplicationDbContext(options);
        var readinessService = new BatchWorkflowReadinessService(context);

        // Cold run
        await readinessService.GetReadinessAsync(19);

        // Warm run
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var res = await readinessService.GetReadinessAsync(19);
        sw.Stop();

        _output.WriteLine($"Warm GetReadinessAsync finished in {sw.ElapsedMilliseconds} ms: Step={res?.CurrentStepNumber}, Key={res?.CurrentStepKey}");
        Assert.NotNull(res);
        Assert.True(sw.ElapsedMilliseconds < 500, $"Warm query took {sw.ElapsedMilliseconds} ms");
    }
}
