namespace SupplierSelectionDSS.Infrastructure.Diagnostics;

public class PerformanceDiagnosticsOptions
{
    public const string SectionName = "PerformanceDiagnostics";

    /// <summary>
    /// Master switch for performance diagnostics and interceptors.
    /// Default: false.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// If true, adds standard Server-Timing response headers for browser DevTools.
    /// Default: false.
    /// </summary>
    public bool EnableServerTimingHeader { get; set; } = false;

    /// <summary>
    /// If true, logs structured SQL performance metrics per request.
    /// Default: false.
    /// </summary>
    public bool EnableSqlMetricsLogging { get; set; } = false;

    /// <summary>
    /// Queries exceeding this threshold (in ms) trigger a slow-query warning.
    /// Default: 100 ms.
    /// </summary>
    public double SlowQueryThresholdMs { get; set; } = 100.0;
}
