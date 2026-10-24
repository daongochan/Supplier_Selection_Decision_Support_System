using System;
using Xunit;

namespace SupplierSelectionDSS.Tests.Integration;

/// <summary>
/// Custom xUnit Fact attribute for SQL Server integration and concurrency tests.
/// Dynamically reports test as Skipped when SQL Server is not configured or disabled,
/// eliminating false passes from silent method returns.
/// When DSS_REQUIRE_SQL=1, DSS_TEST_SQL_SERVER, or DSS_TEST_CONNECTION_STRING is set,
/// the test is discovered and executed; any connectivity or schema failures fail fast.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        var requireSql = Environment.GetEnvironmentVariable("DSS_REQUIRE_SQL") == "1";
        var hasConnStr = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DSS_TEST_CONNECTION_STRING"));
        var hasServer = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DSS_TEST_SQL_SERVER"));
        var runSqlTests = Environment.GetEnvironmentVariable("DSS_RUN_SQL_TESTS") == "1";

        var isConfigured = requireSql || hasConnStr || hasServer || runSqlTests;
        if (!isConfigured)
        {
            Skip = "SQL Server integration tests are disabled by default. Set DSS_REQUIRE_SQL=1 or DSS_TEST_SQL_SERVER to execute.";
        }
    }
}
