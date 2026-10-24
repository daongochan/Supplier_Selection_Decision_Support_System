using System;
using System.Diagnostics;
using System.Threading;

namespace SupplierSelectionDSS.Infrastructure.Diagnostics;

public class RequestDiagnosticsContext
{
    private static readonly AsyncLocal<RequestDiagnosticsContext?> CurrentContext = new();

    public static RequestDiagnosticsContext? Current
    {
        get => CurrentContext.Value;
        set => CurrentContext.Value = value;
    }

    public string TraceIdentifier { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    private long _sqlCount;
    public long SqlCount => Volatile.Read(ref _sqlCount);

    private long _sqlDurationTicks;
    public double SqlTotalDurationMs => TimeSpan.FromTicks(Volatile.Read(ref _sqlDurationTicks)).TotalMilliseconds;

    private long _sqlSlowestTicks;
    public double SqlSlowestDurationMs => TimeSpan.FromTicks(Volatile.Read(ref _sqlSlowestTicks)).TotalMilliseconds;

    public string? SqlSlowestCommandText { get; set; }

    private long _connectionOpenCount;
    public long ConnectionOpenCount => Volatile.Read(ref _connectionOpenCount);

    private long _connectionOpenTicks;
    public double ConnectionOpenDurationMs => TimeSpan.FromTicks(Volatile.Read(ref _connectionOpenTicks)).TotalMilliseconds;

    private long _authValidationCount;
    public long AuthValidationCount => Volatile.Read(ref _authValidationCount);

    private long _authDurationTicks;
    public double AuthDurationMs => TimeSpan.FromTicks(Volatile.Read(ref _authDurationTicks)).TotalMilliseconds;

    public double TotalDurationMs { get; set; }
    public double TtfbMs { get; set; }

    public void RecordSqlCommand(TimeSpan duration, string? commandText)
    {
        Interlocked.Increment(ref _sqlCount);
        Interlocked.Add(ref _sqlDurationTicks, duration.Ticks);

        var curSlowest = Volatile.Read(ref _sqlSlowestTicks);
        while (duration.Ticks > curSlowest)
        {
            var orig = Interlocked.CompareExchange(ref _sqlSlowestTicks, duration.Ticks, curSlowest);
            if (orig == curSlowest)
            {
                SqlSlowestCommandText = SanitizeCommand(commandText);
                break;
            }
            curSlowest = Volatile.Read(ref _sqlSlowestTicks);
        }
    }

    public void RecordConnectionOpen(TimeSpan duration)
    {
        Interlocked.Increment(ref _connectionOpenCount);
        Interlocked.Add(ref _connectionOpenTicks, duration.Ticks);
    }

    public void RecordAuthValidation(TimeSpan duration)
    {
        Interlocked.Increment(ref _authValidationCount);
        Interlocked.Add(ref _authDurationTicks, duration.Ticks);
    }

    private static string? SanitizeCommand(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var clean = text.Trim().Replace('\r', ' ').Replace('\n', ' ');
        while (clean.Contains("  "))
        {
            clean = clean.Replace("  ", " ");
        }
        return clean.Length > 200 ? clean[..200] + "..." : clean;
    }
}
