using Microsoft.Data.SqlClient;

namespace SupplierSelectionDSS.Tools.DemoSeeder;

public static class DatabaseSafetyGuard
{
    public const string AllowedDatabaseName = "SupplierSelectionDSS_Demo";

    private static readonly HashSet<string> StrictlyProhibitedDatabases = new(StringComparer.OrdinalIgnoreCase)
    {
        "SupplierSelectionDSS",
        "SupplierSelectionDSS_Perf",
        "master",
        "model",
        "msdb",
        "tempdb"
    };

    /// <summary>
    /// Validates that the provided connection string targets ONLY the dedicated demo database.
    /// Throws InvalidOperationException if the database is missing, prohibited, or not equal to SupplierSelectionDSS_Demo.
    /// </summary>
    public static void AssertSafeDemoConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid SQL connection string format: {ex.Message}", ex);
        }

        var targetDb = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(targetDb))
        {
            // Try AttachDBFilename or Database keyword
            if (builder.TryGetValue("Database", out var dbVal) && dbVal is string s && !string.IsNullOrWhiteSpace(s))
            {
                targetDb = s;
            }
        }

        if (string.IsNullOrWhiteSpace(targetDb))
        {
            throw new InvalidOperationException(
                "CRITICAL SAFETY VIOLATION: No database name (Initial Catalog) specified in connection string. " +
                $"Demo seeder requires explicit targeting of '{AllowedDatabaseName}'.");
        }

        targetDb = targetDb.Trim();

        if (StrictlyProhibitedDatabases.Contains(targetDb))
        {
            throw new InvalidOperationException(
                $"CRITICAL SAFETY VIOLATION: The connection string targets prohibited database '{targetDb}'. " +
                $"The demo seeder is strictly forbidden from touching operational ('SupplierSelectionDSS'), " +
                $"performance ('SupplierSelectionDSS_Perf'), or system databases.");
        }

        if (!string.Equals(targetDb, AllowedDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SAFETY GUARD: Target database '{targetDb}' does not match the required demo database '{AllowedDatabaseName}'. " +
                $"Seeder execution is aborted to prevent accidental mutation of other environments.");
        }
    }
}
