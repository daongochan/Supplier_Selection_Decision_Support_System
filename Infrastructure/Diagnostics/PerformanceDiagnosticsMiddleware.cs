using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SupplierSelectionDSS.Infrastructure.Diagnostics;

public class PerformanceDiagnosticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceDiagnosticsMiddleware> _logger;
    private readonly PerformanceDiagnosticsOptions _options;
    private readonly InFlightLockDiagnosticsSampler _lockSampler;

    public PerformanceDiagnosticsMiddleware(
        RequestDelegate next,
        ILogger<PerformanceDiagnosticsMiddleware> logger,
        IOptions<PerformanceDiagnosticsOptions> options,
        InFlightLockDiagnosticsSampler lockSampler)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _lockSampler = lockSampler;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var diag = new RequestDiagnosticsContext
        {
            TraceIdentifier = context.TraceIdentifier,
            Path = context.Request.Path.Value ?? "/",
            Method = context.Request.Method
        };
        RequestDiagnosticsContext.Current = diag;
        context.Items["RequestDiagnostics"] = diag;

        using var completedCts = new System.Threading.CancellationTokenSource();
        _lockSampler.StartMonitoring(diag.TraceIdentifier, diag.Method, diag.Path, completedCts.Token);

        var sw = Stopwatch.StartNew();

        if (_options.EnableServerTimingHeader)
        {
            context.Response.OnStarting(() =>
            {
                diag.TtfbMs = sw.Elapsed.TotalMilliseconds;
                var serverTiming = $"total;dur={diag.TtfbMs:F1}, sql;dur={diag.SqlTotalDurationMs:F1};desc=\"{diag.SqlCount} cmds\"";
                if (diag.AuthValidationCount > 0)
                {
                    serverTiming += $", auth;dur={diag.AuthDurationMs:F1}";
                }
                context.Response.Headers.Append("Server-Timing", serverTiming);
                return Task.CompletedTask;
            });
        }

        try
        {
            await _next(context);
        }
        finally
        {
            try { completedCts.Cancel(); } catch { }
            sw.Stop();
            diag.TotalDurationMs = sw.Elapsed.TotalMilliseconds;
            diag.StatusCode = context.Response.StatusCode;

            if (_options.EnableSqlMetricsLogging)
            {
                _logger.LogInformation(
                    "[PerfDiag] {Method} {Path} responded {StatusCode} in {TotalMs:F1}ms (TTFB: {TtfbMs:F1}ms). SQL: {SqlCount} queries, {SqlDurationMs:F1}ms. Auth: {AuthDurationMs:F1}ms. Slowest SQL: {SlowestMs:F1}ms [{SlowestQuery}]",
                    diag.Method, diag.Path, diag.StatusCode, diag.TotalDurationMs, diag.TtfbMs,
                    diag.SqlCount, diag.SqlTotalDurationMs, diag.AuthDurationMs, diag.SqlSlowestDurationMs,
                    diag.SqlSlowestCommandText ?? "None");
            }

            RequestDiagnosticsContext.Current = null;
        }
    }
}
