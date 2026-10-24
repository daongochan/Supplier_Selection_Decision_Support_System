using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SupplierSelectionDSS.Infrastructure.Diagnostics;

public class InFlightLockDiagnosticsSampler
{
    private readonly string _connectionString;
    private readonly ILogger<InFlightLockDiagnosticsSampler> _logger;
    private readonly PerformanceDiagnosticsOptions _options;

    public InFlightLockDiagnosticsSampler(
        IConfiguration configuration,
        ILogger<InFlightLockDiagnosticsSampler> logger,
        IOptions<PerformanceDiagnosticsOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        var baseConn = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var builder = new SqlConnectionStringBuilder(baseConn)
        {
            Pooling = false,
            ConnectTimeout = 10,
            ApplicationName = "DSS_InFlightLockSampler"
        };
        _connectionString = builder.ConnectionString;
    }

    public void StartMonitoring(string traceId, string method, string path, CancellationToken completedToken)
    {
        if (!_options.Enabled || !_options.EnableInFlightLockDiagnostics || string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        var thresholdSeconds = Math.Max(1.0, _options.InFlightLockSamplingThresholdSeconds);

        _ = Task.Run(async () =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                while (!completedToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(thresholdSeconds), completedToken);

                    if (completedToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await SampleLocksAsync(traceId, method, path, sw.Elapsed.TotalSeconds, completedToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Request finished cleanly before or during delay
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[InFlightLockDiag] Sampler task encountered non-fatal error for {TraceId}", traceId);
            }
        }, CancellationToken.None);
    }

    private async Task SampleLocksAsync(string traceId, string method, string path, double elapsedSeconds, CancellationToken token)
    {
        const string query = @"
            SELECT 
                w.session_id,
                w.wait_type,
                w.wait_duration_ms,
                w.blocking_session_id,
                w.resource_description,
                r.status AS request_status,
                r.command AS request_command,
                s.open_transaction_count,
                s.program_name,
                s.login_name,
                ISNULL(t_waiter.text, N'<no_sql>') AS waiting_sql,
                ISNULL(t_blocker.text, N'<no_sql_or_sleeping_tran>') AS blocker_sql
            FROM sys.dm_os_waiting_tasks w
            JOIN sys.dm_exec_sessions s ON w.session_id = s.session_id
            LEFT JOIN sys.dm_exec_requests r ON w.session_id = r.session_id
            OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t_waiter
            LEFT JOIN sys.dm_exec_requests r_blocker ON w.blocking_session_id = r_blocker.session_id
            OUTER APPLY sys.dm_exec_sql_text(r_blocker.sql_handle) t_blocker
            WHERE w.blocking_session_id IS NOT NULL 
              AND w.session_id > 50
              AND w.session_id <> @@SPID;";

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cts.Token);

            await using var cmd = new SqlCommand(query, conn)
            {
                CommandTimeout = 4
            };

            await using var reader = await cmd.ExecuteReaderAsync(cts.Token);
            var foundBlockers = false;

            while (await reader.ReadAsync(cts.Token))
            {
                foundBlockers = true;
                var spid = reader.GetInt16(0);
                var waitType = reader.GetString(1);
                var waitDurationMs = reader.GetInt64(2);
                var blockerSpid = reader.GetInt16(3);
                var resourceDesc = reader.IsDBNull(4) ? "N/A" : reader.GetString(4);
                var reqStatus = reader.IsDBNull(5) ? "N/A" : reader.GetString(5);
                var reqCommand = reader.IsDBNull(6) ? "N/A" : reader.GetString(6);
                var openTran = reader.GetInt32(7);
                var waiterSql = reader.GetString(10);
                var blockerSql = reader.GetString(11);

                _logger.LogWarning(
                    "[InFlightLockDiag] ACTIVE STALL DETECTED (Elapsed: {Elapsed:F1}s) on {Method} {Path} [Trace: {TraceId}]! " +
                    "SPID {Spid} is WAITING ({WaitType}, Duration: {WaitMs}ms, Resource: {Resource}) for Head Blocker SPID {BlockerSpid}. " +
                    "OpenTrans: {OpenTran}, Status: {ReqStatus}, Command: {ReqCommand}. " +
                    "Waiter SQL: [{WaiterSql}]. " +
                    "Head Blocker SQL: [{BlockerSql}].",
                    elapsedSeconds, method, path, traceId,
                    spid, waitType, waitDurationMs, resourceDesc, blockerSpid,
                    openTran, reqStatus, reqCommand,
                    Truncate(waiterSql, 300), Truncate(blockerSql, 300));
            }

            if (!foundBlockers)
            {
                _logger.LogInformation(
                    "[InFlightLockDiag] In-flight request {Method} {Path} [Trace: {TraceId}] is running ({Elapsed:F1}s). " +
                    "No active SQL lock waits or blocking chains detected.",
                    method, path, traceId, elapsedSeconds);
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout or request cancelled
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[InFlightLockDiag] Lock sampling DMV query failed gracefully for {TraceId}", traceId);
        }
    }

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var clean = text.Trim().Replace('\r', ' ').Replace('\n', ' ');
        while (clean.Contains("  "))
        {
            clean = clean.Replace("  ", " ");
        }
        return clean.Length > maxLength ? clean[..maxLength] + "..." : clean;
    }
}
