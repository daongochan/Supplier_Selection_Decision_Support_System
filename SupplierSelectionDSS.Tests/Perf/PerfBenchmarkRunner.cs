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

    [Fact]
    public async Task RunBaselineBenchmark_OnPerfDatabase()
    {
        if (!IsPerfDbAvailable())
        {
            _output.WriteLine("Cơ sở dữ liệu SupplierSelectionDSS_Perf không khả dụng hoặc chưa nạp dữ liệu. Bỏ qua đo benchmark trên môi trường này (CI/máy không có SQL Server).");
            return;
        }

        _output.WriteLine("================================================================================");
        _output.WriteLine("BẮT ĐẦU ĐO HIỆU NĂNG BASELINE TRÊN SupplierSelectionDSS_Perf");
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
            ("Dashboard (Tổng quan)", "/Dashboard"),
            ("Danh sách Batch", "/EvaluationBatch/Index"),
            ("Nhập điểm đánh giá (Batch 1)", "/Evaluation/Index?batchId=1"),
            ("Ma trận so sánh AHP (Batch 1)", "/EvaluationBatch/1/AHP"),
            ("Kết quả TOPSIS (Batch 1)", "/EvaluationBatch/1/TOPSIS"),
            ("Kết quả quyết định (Batch 1)", "/EvaluationBatch/1/Result"),
            ("Lịch sử báo cáo (Batch 1)", "/EvaluationBatch/1/Report"),
            ("Nhật ký hệ thống (Audit Log)", "/AuditLog/Index")
        };

        var results = new List<BenchmarkResultDto>();

        foreach (var (name, url) in endpoints)
        {
            _output.WriteLine($"\n[ĐO] {name} [{url}]");

            // Warm-up (1 lượt; bỏ qua cho AuditLog)
            if (!url.Contains("AuditLog"))
            {
                var warmRes = await client.GetAsync(url);
                if (warmRes.StatusCode != HttpStatusCode.OK)
                {
                    _output.WriteLine($"    [CẢNH BÁO] Warmup trả về HTTP {(int)warmRes.StatusCode} ({warmRes.StatusCode})");
                }
            }

            // Benchmark (3 lượt; riêng AuditLog 1 lượt do thời gian quét không index ~100s)
            int iterations = url.Contains("AuditLog") ? 1 : 3;
            var durations = new List<double>();
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

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _output.WriteLine($"    [LỖI] Lượt {i + 1} thất bại với HTTP {response.StatusCode}");
                    break;
                }

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
                    _output.WriteLine($"    [TIÊU ĐỀ TRANG] {title}");
                }
            }

            if (durations.Count == 0)
            {
                _output.WriteLine("    [THẤT BẠI] Không có lượt đo thành công nào.");
                continue;
            }

            durations.Sort();
            double p50 = durations[(int)(durations.Count * 0.50)];
            double p95 = durations[(int)(durations.Count * 0.95)];
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

            _output.WriteLine($"    -> Lệnh SQL: {avgSql} | p50: {p50:F1} ms | p95: {p95:F1} ms | HTML: {htmlBytes:N0} bytes | CSS: {cssCount} | JS: {jsCount}");
        }

        _output.WriteLine("\n\n================================================================================");
        _output.WriteLine("BẢNG KẾT QUẢ BASELINE (DOCS/PERFORMANCE.MD)");
        _output.WriteLine("================================================================================");
        _output.WriteLine("| Trang | URL | Số lệnh SQL | p50 (ms) | p95 (ms) | HTML (bytes) | CSS req | JS req |");
        _output.WriteLine("|---|---|---|---|---|---|---|---|");
        foreach (var r in results)
        {
            _output.WriteLine($"| {r.PageName} | `{r.Url}` | **{r.SqlCommands}** | {r.ResponseTimeP50:F1} | {r.ResponseTimeP95:F1} | {r.HtmlSizeBytes:N0} | {r.CssRequests} | {r.JsRequests} |");
        }
        _output.WriteLine("================================================================================");

        // Lưu kết quả JSON vào tools/perf/baseline_results.json
        try
        {
            var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var outDir = Path.Combine(projectDir, "tools", "perf");
            Directory.CreateDirectory(outDir);
            var jsonPath = Path.Combine(outDir, "baseline_results.json");
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            _output.WriteLine($"Đã lưu kết quả đo vào {jsonPath}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Không thể lưu file json kết quả: {ex.Message}");
        }
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
