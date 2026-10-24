using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SupplierSelectionDSS.Infrastructure.Diagnostics;

public class PerformanceCommandInterceptor : DbCommandInterceptor, IDbConnectionInterceptor
{
    private readonly ILogger<PerformanceCommandInterceptor> _logger;
    private readonly PerformanceDiagnosticsOptions _options;

    public PerformanceCommandInterceptor(
        ILogger<PerformanceCommandInterceptor> logger,
        IOptions<PerformanceDiagnosticsOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    // -------------------------------------------------------------
    // Connection Interception
    // -------------------------------------------------------------

    public void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        RequestDiagnosticsContext.Current?.RecordConnectionOpen(eventData.Duration);
    }

    public Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        RequestDiagnosticsContext.Current?.RecordConnectionOpen(eventData.Duration);
        return Task.CompletedTask;
    }

    public void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData)
    {
        _logger.LogWarning(eventData.Exception,
            "[PerfDiag] Database connection open failed after {DurationMs:F1}ms. Server={Server}, Database={Database}",
            eventData.Duration.TotalMilliseconds, connection.DataSource, connection.Database);
    }

    public Task ConnectionFailedAsync(DbConnection connection, ConnectionErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(eventData.Exception,
            "[PerfDiag] Database connection open failed after {DurationMs:F1}ms. Server={Server}, Database={Database}",
            eventData.Duration.TotalMilliseconds, connection.DataSource, connection.Database);
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------
    // Command Execution Interception
    // -------------------------------------------------------------

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        RecordCommand(command, eventData.Duration);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        RecordCommand(command, eventData.Duration);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        RecordCommand(command, eventData.Duration);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        RecordCommand(command, eventData.Duration);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command, CommandExecutedEventData eventData, int result)
    {
        RecordCommand(command, eventData.Duration);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        RecordCommand(command, eventData.Duration);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        _logger.LogError(eventData.Exception,
            "[PerfDiag] SQL Command failed after {DurationMs:F1}ms. Command: {CommandText}",
            eventData.Duration.TotalMilliseconds, Sanitize(command.CommandText));
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogError(eventData.Exception,
            "[PerfDiag] SQL Command failed after {DurationMs:F1}ms. Command: {CommandText}",
            eventData.Duration.TotalMilliseconds, Sanitize(command.CommandText));
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private void RecordCommand(DbCommand command, TimeSpan duration)
    {
        RequestDiagnosticsContext.Current?.RecordSqlCommand(duration, command.CommandText);

        if (_options.EnableSqlMetricsLogging && duration.TotalMilliseconds >= _options.SlowQueryThresholdMs)
        {
            _logger.LogWarning(
                "[PerfDiag] Slow SQL query ({DurationMs:F1}ms >= {Threshold}ms): {CommandText}",
                duration.TotalMilliseconds, _options.SlowQueryThresholdMs, Sanitize(command.CommandText));
        }
    }

    private static string Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var clean = text.Trim().Replace('\r', ' ').Replace('\n', ' ');
        while (clean.Contains("  "))
        {
            clean = clean.Replace("  ", " ");
        }
        return clean.Length > 250 ? clean[..250] + "..." : clean;
    }
}
