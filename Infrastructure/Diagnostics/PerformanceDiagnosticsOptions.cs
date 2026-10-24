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

    /// <summary>
    /// If true, actively samples SQL locks and blocking chains for in-flight requests exceeding threshold.
    /// Default: true.
    /// </summary>
    public bool EnableInFlightLockDiagnostics { get; set; } = true;

    /// <summary>
    /// Threshold in seconds before active lock sampling is triggered for an in-flight request.
    /// Default: 5.0 seconds.
    /// </summary>
    public double InFlightLockSamplingThresholdSeconds { get; set; } = 5.0;
}
