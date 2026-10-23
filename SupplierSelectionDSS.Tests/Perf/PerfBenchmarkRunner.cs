using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Services;
using Xunit;
using Xunit.Abstractions;

namespace SupplierSelectionDSS.Tests.Perf;

public class PerfBenchmarkRunner
{
    private readonly ITestOutputHelper _output;
    private const string PerfConnectionString =
        "Server=.;Database=SupplierSelectionDSS_Perf;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3";

    public PerfBenchmarkRunner(ITestOutputHelper output)
    {
        _output = output;
    }

    private static bool IsPerfBenchmarkEnabled()
    {
        var flag = Environment.GetEnvironmentVariable("DSS_RUN_PERF");
        return string.Equals(flag, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldWritePerfFiles()
    {
        var flag = Environment.GetEnvironmentVariable("DSS_WRITE_PERF_FILES");
        return string.Equals(flag, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPerfDbAvailable()
    {
        try
        {
            using var conn = new SqlConnection(PerfConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM EvaluationBatches";
            var count = (int)(cmd.ExecuteScalar() ?? 0);
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    public static double CalculatePercentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues == null || sortedValues.Count == 0)
        {
            throw new ArgumentException("Sample list cannot be empty.", nameof(sortedValues));
        }

        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        var clamped = Math.Clamp(percentile, 0.0, 1.0);
        var position = clamped * (sortedValues.Count - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        if (lowerIndex == upperIndex)
        {
            return sortedValues[lowerIndex];
        }

        var weight = position - lowerIndex;
        return sortedValues[lowerIndex] + (sortedValues[upperIndex] - sortedValues[lowerIndex]) * weight;
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void CalculatePercentile_ComputesLinearInterpolatedPercentiles()
    {
        var samples = new List<double> { 10.0, 20.0, 30.0, 40.0, 100.0 };
        samples.Sort();

        Assert.Equal(30.0, CalculatePercentile(samples, 0.50), 4);
        Assert.Equal(88.0, CalculatePercentile(samples, 0.95), 4);
        Assert.Equal(42.0, CalculatePercentile(new[] { 42.0 }, 0.95), 4);
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task RunBaselineBenchmark_OnPerfDatabase()
    {
        if (!IsPerfBenchmarkEnabled())
        {
            _output.WriteLine("Perf benchmark execution is disabled by default during unit tests. Set DSS_RUN_PERF=1 to run against SupplierSelectionDSS_Perf.");
            return;
        }

        if (!IsPerfDbAvailable())
        {
            throw new InvalidOperationException("DSS_RUN_PERF=1 was specified, but database SupplierSelectionDSS_Perf is unavailable or empty.");
        }

        var results = await ExecuteBenchmarkSuiteAsync("BASELINE", "baseline_results.json");
        Assert.Equal(8, results.Count);
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task RunPostOptimizationBenchmark_OnPerfDatabase()
    {
        if (!IsPerfBenchmarkEnabled())
        {
            _output.WriteLine("Perf benchmark execution is disabled by default during unit tests. Set DSS_RUN_PERF=1 to run against SupplierSelectionDSS_Perf.");
            return;
        }

        if (!IsPerfDbAvailable())
        {
            throw new InvalidOperationException("DSS_RUN_PERF=1 was specified, but database SupplierSelectionDSS_Perf is unavailable or empty.");
        }

        var results = await ExecuteBenchmarkSuiteAsync("POST-OPTIMIZATION", "post_results.json");
        Assert.Equal(8, results.Count);
    }

    private async Task<List<BenchmarkResultDto>> ExecuteBenchmarkSuiteAsync(string label, string outputFileName)
    {
        _output.WriteLine("================================================================================");
        _output.WriteLine($"STARTING {label} BENCHMARK ON SupplierSelectionDSS_Perf");
        _output.WriteLine("================================================================================");

        var interceptor = new CommandCountingInterceptor();
        using var factory = new PerfTestWebApplicationFactory(interceptor, PerfConnectionString);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.Timeout = TimeSpan.FromMinutes(5);

        var endpoints = new List<(string Name, string Url)>
        {
            ("Dashboard", "/Dashboard"),
            ("Batch List", "/EvaluationBatch/Index"),
            ("Score Entry (Batch 1)", "/Evaluation/Index?batchId=1"),
            ("AHP Comparison Matrix (Batch 1)", "/EvaluationBatch/1/AHP"),
            ("TOPSIS Ranking (Batch 1)", "/EvaluationBatch/1/TOPSIS"),
            ("Decision Result (Batch 1)", "/EvaluationBatch/1/Result"),
            ("Report History (Batch 1)", "/EvaluationBatch/1/Report"),
            ("Audit Log", "/AuditLog/Index")
        };

        var results = new List<BenchmarkResultDto>();

        foreach (var (name, url) in endpoints)
        {
            _output.WriteLine($"\n[MEASURE] {name} [{url}]");

            // Consistent warm-up across ALL endpoints (including AuditLog)
            var warmRes = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, warmRes.StatusCode);

            const int iterations = 5;
            var durations = new List<double>(iterations);
            long totalSqlCount = 0;
            int htmlBytes = 0;
            int cssCount = 0;
            int jsCount = 0;
            int imgCount = 0;

            for (int i = 0; i < iterations; i++)
            {
                interceptor.Reset();
                var sw = Stopwatch.StartNew();
                var response = await client.GetAsync(url);
                sw.Stop();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                durations.Add(sw.Elapsed.TotalMilliseconds);
                totalSqlCount += interceptor.CommandCount;

                if (i == 0)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    htmlBytes = html.Length;
                    cssCount = Regex.Matches(html, @"<link[^>]+rel=[""']stylesheet[""']", RegexOptions.IgnoreCase).Count;
                    jsCount = Regex.Matches(html, @"<script[^>]+src=", RegexOptions.IgnoreCase).Count;
                    imgCount = Regex.Matches(html, @"<img\s+", RegexOptions.IgnoreCase).Count;
                    var titleMatch = Regex.Match(html, @"<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "No title";
                    _output.WriteLine($"    [PAGE TITLE] {title}");
                }
            }

            durations.Sort();
            double p50 = CalculatePercentile(durations, 0.50);
            double p95 = CalculatePercentile(durations, 0.95);
            long avgSql = (long)Math.Round((double)totalSqlCount / durations.Count);

            var dto = new BenchmarkResultDto
            {
                PageName = name,
                Url = url,
                SqlCommands = avgSql,
                ResponseTimeP50 = Math.Round(p50, 1),
                ResponseTimeP95 = Math.Round(p95, 1),
                HtmlSizeBytes = htmlBytes,
                CssRequests = cssCount,
                JsRequests = jsCount,
                ImgRequests = imgCount
            };

            results.Add(dto);

            _output.WriteLine($"    -> SQL Commands: {avgSql} | p50: {p50:F1} ms | p95: {p95:F1} ms | HTML: {htmlBytes:N0} bytes | CSS: {cssCount} | JS: {jsCount}");
        }

        _output.WriteLine("\n\n================================================================================");
        _output.WriteLine($"{label} RESULTS TABLE");
        _output.WriteLine("================================================================================");
        _output.WriteLine("| Page | URL | SQL Commands | p50 (ms) | p95 (ms) | HTML (bytes) | CSS req | JS req |");
        _output.WriteLine("|---|---|---|---|---|---|---|---|");
        foreach (var r in results)
        {
            _output.WriteLine($"| {r.PageName} | `{r.Url}` | **{r.SqlCommands}** | {r.ResponseTimeP50:F1} | {r.ResponseTimeP95:F1} | {r.HtmlSizeBytes:N0} | {r.CssRequests} | {r.JsRequests} |");
        }
        _output.WriteLine("================================================================================");

        if (ShouldWritePerfFiles())
        {
            var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var outDir = Path.Combine(projectDir, "tools", "perf");
            Directory.CreateDirectory(outDir);
            var jsonPath = Path.Combine(outDir, outputFileName);
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            _output.WriteLine($"Saved benchmark results to {jsonPath}");
        }
        else
        {
            _output.WriteLine("Skipped writing JSON to tools/perf (set DSS_WRITE_PERF_FILES=1 to persist results).");
        }

        return results;
    }
}

public class BenchmarkResultDto
{
    public string PageName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long SqlCommands { get; set; }
    public double ResponseTimeP50 { get; set; }
    public double ResponseTimeP95 { get; set; }
    public int HtmlSizeBytes { get; set; }
    public int CssRequests { get; set; }
    public int JsRequests { get; set; }
    public int ImgRequests { get; set; }
}

public class CommandCountingInterceptor : DbCommandInterceptor
{
    private long _commandCount;
    public long CommandCount => Volatile.Read(ref _commandCount);

    public void Reset()
    {
        Interlocked.Exchange(ref _commandCount, 0);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Interlocked.Increment(ref _commandCount);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _commandCount);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Interlocked.Increment(ref _commandCount);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _commandCount);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Interlocked.Increment(ref _commandCount);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _commandCount);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }
}

public class PerfTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly CommandCountingInterceptor _interceptor;
    private readonly string _connectionString;

    public PerfTestWebApplicationFactory(CommandCountingInterceptor interceptor, string connectionString)
    {
        _interceptor = interceptor;
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ApplicationDbContext)).ToList();

            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(_connectionString, sql => sql.CommandTimeout(120));
                options.AddInterceptors(_interceptor);
            });

            services.AddAuthentication("TestPerfScheme")
                .AddScheme<AuthenticationSchemeOptions, TestPerfAuthHandler>("TestPerfScheme", options => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "TestPerfScheme";
                options.DefaultChallengeScheme = "TestPerfScheme";
                options.DefaultScheme = "TestPerfScheme";
                options.DefaultSignInScheme = "TestPerfScheme";
            });
        });
    }
}

public class TestPerfAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestPerfAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, UserRole.Administrator),
            new Claim(ClaimTypes.Role, UserRole.Expert),
            new Claim(ClaimTypes.Role, UserRole.Manager),
            new Claim(ClaimTypes.GivenName, "Quản trị viên")
        };

        var identity = new ClaimsIdentity(claims, "TestPerfScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestPerfScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
