using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Infrastructure.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace SupplierSelectionDSS.Tests.Perf;

public class ComprehensiveBaselineBenchmark
{
    private readonly ITestOutputHelper _output;

    private const string PerfConnectionString =
        "Server=.;Database=SupplierSelectionDSS_Perf;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30";

    public ComprehensiveBaselineBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    private static bool IsPerfBenchmarkEnabled()
    {
        var flag = Environment.GetEnvironmentVariable("DSS_RUN_PERF");
        return string.Equals(flag, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPerfDbAvailable(out string error)
    {
        error = string.Empty;
        try
        {
            using var conn = new SqlConnection(PerfConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM EvaluationBatches";
            var count = (int)(cmd.ExecuteScalar() ?? 0);
            return count > 0;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task RunFullBaselineSuite_OnPerfDatabase()
    {
        if (!IsPerfBenchmarkEnabled())
        {
            _output.WriteLine("Set DSS_RUN_PERF=1 to run full baseline performance benchmark suite.");
            return;
        }

        if (!IsPerfDbAvailable(out var dbErr))
        {
            throw new InvalidOperationException($"SupplierSelectionDSS_Perf database is unavailable: {dbErr}");
        }

        using var factory = new RealAuthPerfWebApplicationFactory(PerfConnectionString);

        // 1. Unauthenticated client for login tests
        var anonClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // 2. Authenticated admin client for all authenticated endpoints
        var authClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        _output.WriteLine("================================================================================");
        _output.WriteLine("AUTHENTICATING TEST RUNNER VIA REAL LOGIN FORM (admin / Admin@123456)...");
        var loginGet = await authClient.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, loginGet.StatusCode);
        var loginHtml = await loginGet.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(loginHtml);
        Assert.False(string.IsNullOrEmpty(token), "Anti-forgery token must be present on /Account/Login.");

        var loginPost = await authClient.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Username", "admin" },
            { "Password", "Admin@123456" },
            { "__RequestVerificationToken", token }
        }));
        Assert.True(loginPost.StatusCode == HttpStatusCode.Redirect || loginPost.StatusCode == HttpStatusCode.Found,
            $"Login POST should redirect. Actual status: {loginPost.StatusCode}");
        _output.WriteLine("LOGIN SUCCESSFUL. REAL AUTHENTICATION COOKIE RECEIVED.");
        _output.WriteLine("================================================================================");

        var baselineResults = new List<ComprehensiveBenchmarkDto>();

        // -------------------------------------------------------------
        // Read Endpoints (Cold + 30 Warm Iterations)
        // -------------------------------------------------------------
        var readEndpoints = new List<(string Name, string Url, bool IsAuth)>
        {
            ("Login GET", "/Account/Login", false),
            ("Dashboard (Default Batch)", "/Dashboard", true),
            ("Dashboard (Switch Batch 2)", "/Dashboard?batchId=2", true),
            ("Supplier List", "/Supplier", true),
            ("Criteria List", "/Criteria", true),
            ("Evaluation Batch List", "/EvaluationBatch", true),
            ("Batch Details (Batch 1)", "/EvaluationBatch/Details/1", true),
            ("Evaluation Score Matrix", "/Evaluation/Index?batchId=1", true),
            ("AHP Comparison Matrix", "/EvaluationBatch/1/AHP", true),
            ("TOPSIS Results & Matrices", "/EvaluationBatch/1/TOPSIS", true),
            ("Decision Result", "/EvaluationBatch/1/Result", true),
            ("Report History List", "/EvaluationBatch/1/Report", true),
            ("Audit Log", "/AuditLog/Index", true)
        };

        const int warmIterations = 30;

        foreach (var (name, url, isAuth) in readEndpoints)
        {
            _output.WriteLine($"\n[BENCHMARK] {name} [{url}] - Cold + {warmIterations} Warm Samples...");
            var client = isAuth ? authClient : anonClient;

            // Cold Request
            var swCold = Stopwatch.StartNew();
            var coldResponse = await client.GetAsync(url);
            swCold.Stop();
            var coldTotalMs = swCold.Elapsed.TotalMilliseconds;
            Assert.Equal(HttpStatusCode.OK, coldResponse.StatusCode);

            var coldBody = await coldResponse.Content.ReadAsStringAsync();
            var (coldSqlCount, coldSqlMs, coldAuthMs) = ParseServerTiming(coldResponse);

            var durations = new List<double>(warmIterations);
            var sqlCounts = new List<long>(warmIterations);
            var sqlDurations = new List<double>(warmIterations);
            var authDurations = new List<double>(warmIterations);

            int htmlBytes = coldBody.Length;
            int cssCount = Regex.Matches(coldBody, @"<link[^>]+rel=[""']stylesheet[""']", RegexOptions.IgnoreCase).Count;
            int jsCount = Regex.Matches(coldBody, @"<script[^>]+src=", RegexOptions.IgnoreCase).Count;

            for (int i = 0; i < warmIterations; i++)
            {
                var sw = Stopwatch.StartNew();
                var res = await client.GetAsync(url);
                sw.Stop();
                Assert.Equal(HttpStatusCode.OK, res.StatusCode);

                durations.Add(sw.Elapsed.TotalMilliseconds);
                var (sqlC, sqlMs, authMs) = ParseServerTiming(res);
                sqlCounts.Add(sqlC);
                sqlDurations.Add(sqlMs);
                authDurations.Add(authMs);
            }

            durations.Sort();
            double p50 = PerfBenchmarkRunner.CalculatePercentile(durations, 0.50);
            double p95 = PerfBenchmarkRunner.CalculatePercentile(durations, 0.95);
            long avgSql = (long)Math.Round(sqlCounts.Average());
            double avgSqlMs = sqlDurations.Average();
            double avgAuthMs = authDurations.Average();

            var dto = new ComprehensiveBenchmarkDto
            {
                Name = name,
                Url = url,
                Role = isAuth ? "Administrator" : "Anonymous",
                SampleSize = warmIterations,
                ColdDurationMs = Math.Round(coldTotalMs, 1),
                P50DurationMs = Math.Round(p50, 1),
                P95DurationMs = Math.Round(p95, 1),
                AvgSqlCommands = avgSql,
                AvgSqlDurationMs = Math.Round(avgSqlMs, 1),
                AvgAuthValidationMs = Math.Round(avgAuthMs, 1),
                HtmlSizeBytes = htmlBytes,
                CssRequests = cssCount,
                JsRequests = jsCount
            };
            baselineResults.Add(dto);

            _output.WriteLine($"    -> Cold: {coldTotalMs:F1}ms | Median(p50): {p50:F1}ms | p95: {p95:F1}ms | SQL: {avgSql} ({avgSqlMs:F1}ms) | Auth: {avgAuthMs:F1}ms | HTML: {htmlBytes:N0} B");
        }

        // -------------------------------------------------------------
        // Concurrent Load Test (5 Concurrent Users for Key Read Endpoints)
        // -------------------------------------------------------------
        _output.WriteLine("\n\n================================================================================");
        _output.WriteLine("CONCURRENCY BENCHMARK (5 Simultaneous Users x 10 Rounds = 50 Requests)");
        _output.WriteLine("================================================================================");

        var concurrentEndpoints = new[]
        {
            ("Dashboard", "/Dashboard"),
            ("Evaluation Matrix", "/Evaluation/Index?batchId=1"),
            ("TOPSIS", "/EvaluationBatch/1/TOPSIS"),
            ("Decision Result", "/EvaluationBatch/1/Result")
        };

        var concurrencyResults = new Dictionary<string, (double p50, double p95)>();

        foreach (var (cName, cUrl) in concurrentEndpoints)
        {
            var concurrentSamples = new List<double>();
            const int rounds = 10;
            const int concurrency = 5;

            for (int r = 0; r < rounds; r++)
            {
                var tasks = Enumerable.Range(0, concurrency).Select(async _ =>
                {
                    var sw = Stopwatch.StartNew();
                    var resp = await authClient.GetAsync(cUrl);
                    sw.Stop();
                    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                    return sw.Elapsed.TotalMilliseconds;
                });

                var roundTimes = await Task.WhenAll(tasks);
                concurrentSamples.AddRange(roundTimes);
            }

            concurrentSamples.Sort();
            double cP50 = PerfBenchmarkRunner.CalculatePercentile(concurrentSamples, 0.50);
            double cP95 = PerfBenchmarkRunner.CalculatePercentile(concurrentSamples, 0.95);
            concurrencyResults[cName] = (Math.Round(cP50, 1), Math.Round(cP95, 1));

            _output.WriteLine($"[CONCURRENCY 5] {cName} -> Median: {cP50:F1}ms | p95: {cP95:F1}ms (Samples: {concurrentSamples.Count})");
        }

        // -------------------------------------------------------------
        // File Generation & Download Benchmarks
        // -------------------------------------------------------------
        _output.WriteLine("\n\n================================================================================");
        _output.WriteLine("FILE GENERATION & DOWNLOAD BENCHMARKS");
        _output.WriteLine("================================================================================");

        var reportListRes = await authClient.GetAsync("/EvaluationBatch/1/Report");
        var reportHtml = await reportListRes.Content.ReadAsStringAsync();
        var reportToken = ExtractAntiforgeryToken(reportHtml);

        if (!string.IsNullOrEmpty(reportToken))
        {
            // PDF Generation
            var swPdf = Stopwatch.StartNew();
            var pdfRes = await authClient.PostAsync("/EvaluationBatch/1/Report/GeneratePdf",
                new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", reportToken } }));
            swPdf.Stop();
            var (pdfSql, pdfSqlMs, _) = ParseServerTiming(pdfRes);
            _output.WriteLine($"[REPORT PDF] Generated in {swPdf.Elapsed.TotalMilliseconds:F1}ms (Status: {pdfRes.StatusCode}, SQL: {pdfSql} queries, {pdfSqlMs:F1}ms)");

            // Excel Generation
            var swExcel = Stopwatch.StartNew();
            var excelRes = await authClient.PostAsync("/EvaluationBatch/1/Report/GenerateExcel",
                new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", reportToken } }));
            swExcel.Stop();
            var (excelSql, excelSqlMs, _) = ParseServerTiming(excelRes);
            _output.WriteLine($"[REPORT EXCEL] Generated in {swExcel.Elapsed.TotalMilliseconds:F1}ms (Status: {excelRes.StatusCode}, SQL: {excelSql} queries, {excelSqlMs:F1}ms)");

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var latestReport = await db.Reports
                .Where(r => r.BatchId == 1)
                .OrderByDescending(r => r.GeneratedAt)
                .FirstOrDefaultAsync();

            if (latestReport != null)
            {
                var swDl = Stopwatch.StartNew();
                var dlRes = await authClient.GetAsync($"/Report/Download/{latestReport.ReportId}");
                swDl.Stop();
                var fileBytes = (await dlRes.Content.ReadAsByteArrayAsync()).Length;
                _output.WriteLine($"[REPORT DOWNLOAD] Downloaded Report #{latestReport.ReportId} ({latestReport.ReportType}) in {swDl.Elapsed.TotalMilliseconds:F1}ms (Bytes: {fileBytes:N0})");
            }
        }

        // -------------------------------------------------------------
        // Print Summary Baseline Table
        // -------------------------------------------------------------
        _output.WriteLine("\n\n================================================================================");
        _output.WriteLine("BASELINE MEASUREMENT RESULTS (PRE-OPTIMIZATION)");
        _output.WriteLine("================================================================================");
        _output.WriteLine("| Endpoint | Role | Samples | Cold (ms) | Median (ms) | p95 (ms) | SQL Cmds | SQL Time (ms) | Auth Time (ms) | HTML Size |");
        _output.WriteLine("|---|---|:---:|---:|---:|---:|:---:|---:|---:|---:|");
        foreach (var r in baselineResults)
        {
            _output.WriteLine($"| {r.Name} | {r.Role} | {r.SampleSize} | {r.ColdDurationMs:F1} | **{r.P50DurationMs:F1}** | **{r.P95DurationMs:F1}** | {r.AvgSqlCommands} | {r.AvgSqlDurationMs:F1} | {r.AvgAuthValidationMs:F1} | {r.HtmlSizeBytes:N0} B |");
        }

        _output.WriteLine("\nCONCURRENT LOAD (5 USERS):");
        foreach (var (k, v) in concurrencyResults)
        {
            _output.WriteLine($"- {k}: Median = {v.p50:F1}ms, p95 = {v.p95:F1}ms");
        }

        // Persist to json
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var outDir = Path.Combine(projectDir, "docs", "qa", "evidence", "performance");
        Directory.CreateDirectory(outDir);
        var jsonPath = Path.Combine(outDir, "baseline_comprehensive.json");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(new
        {
            Timestamp = DateTime.UtcNow,
            Database = "SupplierSelectionDSS_Perf",
            Endpoints = baselineResults,
            Concurrency = concurrencyResults
        }, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine($"Saved baseline comprehensive report to {jsonPath}");
    }

    private static (long sqlCount, double sqlMs, double authMs) ParseServerTiming(HttpResponseMessage response)
    {
        long sqlCount = 0;
        double sqlMs = 0;
        double authMs = 0;

        if (response.Headers.TryGetValues("Server-Timing", out var values))
        {
            var header = string.Join(", ", values);
            var sqlMatch = Regex.Match(header, @"sql;dur=([\d\.]+);desc=""(\d+)\s*cmds""");
            if (sqlMatch.Success)
            {
                double.TryParse(sqlMatch.Groups[1].Value, out sqlMs);
                long.TryParse(sqlMatch.Groups[2].Value, out sqlCount);
            }

            var authMatch = Regex.Match(header, @"auth;dur=([\d\.]+)");
            if (authMatch.Success)
            {
                double.TryParse(authMatch.Groups[1].Value, out authMs);
            }
        }

        return (sqlCount, sqlMs, authMs);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html, @"name=[""']__RequestVerificationToken[""'][^>]*value=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            match = Regex.Match(html, @"value=[""']([^""']+)[""'][^>]*name=[""']__RequestVerificationToken[""']", RegexOptions.IgnoreCase);
        }
        return match.Groups[1].Value;
    }
}

public class ComprehensiveBenchmarkDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int SampleSize { get; set; }
    public double ColdDurationMs { get; set; }
    public double P50DurationMs { get; set; }
    public double P95DurationMs { get; set; }
    public long AvgSqlCommands { get; set; }
    public double AvgSqlDurationMs { get; set; }
    public double AvgAuthValidationMs { get; set; }
    public int HtmlSizeBytes { get; set; }
    public int CssRequests { get; set; }
    public int JsRequests { get; set; }
}

public class RealAuthPerfWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public RealAuthPerfWebApplicationFactory(string connectionString)
    {
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

            // Register Real DbContext targeting SupplierSelectionDSS_Perf with PerformanceCommandInterceptor
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseSqlServer(_connectionString, sql =>
                {
                    sql.CommandTimeout(60);
                });
                var interceptor = sp.GetRequiredService<PerformanceCommandInterceptor>();
                options.AddInterceptors(interceptor);
            });
        });
    }
}
