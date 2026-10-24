using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using Xunit;
using Xunit.Abstractions;

namespace SupplierSelectionDSS.Tests.Integration;

public class SqlServerConcurrencyAndTriggerTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly string _dbName = "SupplierSelectionDSS_Test_" + Guid.NewGuid().ToString("N")[..12];
    private string _masterConnString = string.Empty;
    private string _testConnString = string.Empty;
    private bool _sqlAvailable;
    private bool _databaseCreated;
    private Exception? _initException;

    public SqlServerConcurrencyAndTriggerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var baseConnStr = Environment.GetEnvironmentVariable("DSS_TEST_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(baseConnStr))
        {
            var builder = new SqlConnectionStringBuilder(baseConnStr);
            builder.InitialCatalog = "master";
            _masterConnString = builder.ConnectionString;
            builder.InitialCatalog = _dbName;
            _testConnString = builder.ConnectionString;
        }
        else
        {
            var baseServer = Environment.GetEnvironmentVariable("DSS_TEST_SQL_SERVER") ?? ".";
            _masterConnString = $"Server={baseServer};Database=master;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=5";
            _testConnString = $"Server={baseServer};Database={_dbName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=10";
        }

        try
        {
            await using (var conn = new SqlConnection(_masterConnString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE [{_dbName}];";
                await cmd.ExecuteNonQueryAsync();
            }

            _databaseCreated = true;

            await ApplySchemaToTestDatabaseAsync();
            _sqlAvailable = true;
        }
        catch (Exception ex)
        {
            _initException = ex;
            _sqlAvailable = false;
            if (Environment.GetEnvironmentVariable("DSS_REQUIRE_SQL") == "1")
            {
                throw new InvalidOperationException($"SQL Server initialization failed under DSS_REQUIRE_SQL=1: {ex.Message}", ex);
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (!_databaseCreated)
        {
            return;
        }

        try
        {
            SqlConnection.ClearAllPools();
            await using var conn = new SqlConnection(_masterConnString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
IF DB_ID(N'{_dbName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_dbName}];
END";
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore cleanup failure in disposable test DB
        }
    }

    private async Task ApplySchemaToTestDatabaseAsync()
    {
        var baseDir = AppContext.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        var schemaPath = Path.Combine(repoRoot, "database", "01_schema.sql");
        var sqlContent = await File.ReadAllTextAsync(schemaPath);

        var batches = Regex.Split(sqlContent, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        await using var conn = new SqlConnection(_testConnString);
        await conn.OpenAsync();

        foreach (var rawBatch in batches)
        {
            var batch = rawBatch.Trim();
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            // Skip the initial 00_Create_Database header blocks (USE master, SAFETY GUARD, CREATE DATABASE, ALTER DATABASE, USE SupplierSelectionDSS)
            if (Regex.IsMatch(batch, @"\bUSE\s+master\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(batch, @"\bSET\s+NOEXEC\s+ON\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(batch, @"\bALTER\s+DATABASE\s+SupplierSelectionDSS\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(batch, @"\bCREATE\s+DATABASE\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(batch, @"^\s*USE\s+\[?SupplierSelectionDSS\]?\s*;?\s*$", RegexOptions.IgnoreCase))
            {
                continue;
            }

            batch = Regex.Replace(batch, @"^\s*USE\s+\[?SupplierSelectionDSS\]?\s*;?", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 60;
            cmd.CommandText = batch;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private ApplicationDbContext CreateSqlContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_testConnString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private bool EnsureSqlAvailable()
    {
        if (!_sqlAvailable)
        {
            if (Environment.GetEnvironmentVariable("DSS_REQUIRE_SQL") == "1")
            {
                throw new InvalidOperationException($"SQL Server is required by DSS_REQUIRE_SQL=1 but failed to initialize test database: {_initException?.Message}", _initException);
            }
            _output.WriteLine($"[SKIPPED] Test skipped because SQL Server is not available: {_initException?.Message}");
            return false;
        }
        return true;
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    [Trait("Category", "Integration")]
    public async Task SqlServer_F03_TriggersBlockChildMutations_WhenBatchIsCompletedOrCancelled_And_RowVersionDetectsConcurrentWrite()
    {
        if (!EnsureSqlAvailable())
        {
            return;
        }

        int batchId;
        int sup1Id, sup2Id, crit1Id, crit2Id, userId;

        await using (var db = CreateSqlContext())
        {
            var user = new User
            {
                Username = "sql_tester_" + Guid.NewGuid().ToString("N")[..6],
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
                FullName = "SQL Tester",
                Role = UserRole.Administrator,
                Status = EntityStatus.Active
            };
            var group = new ProductGroup
            {
                GroupCode = "PG-SQL",
                GroupName = "SQL Test Group",
                Status = EntityStatus.Active
            };
            var s1 = new Supplier { SupplierCode = "S-SQL-1", SupplierName = "SQL Supplier 1", Status = EntityStatus.Active };
            var s2 = new Supplier { SupplierCode = "S-SQL-2", SupplierName = "SQL Supplier 2", Status = EntityStatus.Active };
            var c1 = new Criterion { CriteriaCode = "C-SQL-1", CriteriaName = "SQL Cost", CriteriaType = "Cost", Unit = "USD", Status = EntityStatus.Active };
            var c2 = new Criterion { CriteriaCode = "C-SQL-2", CriteriaName = "SQL Quality", CriteriaType = "Benefit", Unit = "Score", Status = EntityStatus.Active };

            db.Users.Add(user);
            db.ProductGroups.Add(group);
            db.Suppliers.AddRange(s1, s2);
            db.Criteria.AddRange(c1, c2);
            await db.SaveChangesAsync();

            userId = user.UserId;
            sup1Id = s1.SupplierId;
            sup2Id = s2.SupplierId;
            crit1Id = c1.CriteriaId;
            crit2Id = c2.CriteriaId;

            var batch = new EvaluationBatch
            {
                BatchCode = "B-SQL-01",
                BatchName = "SQL Concurrency Batch",
                GroupId = group.GroupId,
                Status = BatchStatus.Evaluating,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            batch.Suppliers.Add(s1);
            batch.Suppliers.Add(s2);
            batch.Criteria.Add(c1);
            batch.Criteria.Add(c2);
            db.EvaluationBatches.Add(batch);
            await db.SaveChangesAsync();
            batchId = batch.BatchId;
        }

        // 1. Capture initial RowVersion, then perform two concurrent score saves presenting the same initial RowVersion
        string initialRowVersion;
        await using (var db = CreateSqlContext())
        {
            var b = await db.EvaluationBatches.AsNoTracking().FirstAsync(x => x.BatchId == batchId);
            initialRowVersion = BatchConcurrencyHelper.ToBase64(b.RowVersion);
        }

        var saveTask1 = Task.Run(async () =>
        {
            await using var db1 = CreateSqlContext();
            var svc1 = new EvaluationService(db1);
            await svc1.SaveAsync(new EvaluationScoreInputViewModel
            {
                BatchId = batchId,
                RowVersion = initialRowVersion,
                Rows = new List<EvaluationScoreInputRowViewModel>
                {
                    new() { SupplierId = sup1Id, Scores = new Dictionary<int, decimal?> { { crit1Id, 100m }, { crit2Id, 80m } } },
                    new() { SupplierId = sup2Id, Scores = new Dictionary<int, decimal?> { { crit1Id, 120m }, { crit2Id, 90m } } }
                }
            }, userId);
        });

        var saveTask2 = Task.Run(async () =>
        {
            await using var db2 = CreateSqlContext();
            var svc2 = new EvaluationService(db2);
            await svc2.SaveAsync(new EvaluationScoreInputViewModel
            {
                BatchId = batchId,
                RowVersion = initialRowVersion,
                Rows = new List<EvaluationScoreInputRowViewModel>
                {
                    new() { SupplierId = sup1Id, Scores = new Dictionary<int, decimal?> { { crit1Id, 110m }, { crit2Id, 85m } } },
                    new() { SupplierId = sup2Id, Scores = new Dictionary<int, decimal?> { { crit1Id, 130m }, { crit2Id, 95m } } }
                }
            }, userId);
        });

        var acts = await Task.WhenAll(
            saveTask1.ContinueWith(t => t.Exception?.InnerException),
            saveTask2.ContinueWith(t => t.Exception?.InnerException));

        // Exactly one should succeed (null exception) and the other should fail with concurrency conflict
        acts.Count(ex => ex == null).Should().Be(1);
        acts.Count(ex => ex is BusinessRuleException).Should().Be(1);

        // 2. Complete the workflow and verify SQL trigger blocks direct raw SQL mutation on a Completed batch
        await using (var db = CreateSqlContext())
        {
            var ahpSvc = new AHPService(db, new AHPCalculationService());
            await ahpSvc.SaveComparisonsAsync(new AHPComparisonInputViewModel
            {
                BatchId = batchId,
                Comparisons = new List<AHPComparisonInputItemViewModel>
                {
                    new() { CriteriaRowId = crit1Id, CriteriaColId = crit2Id, Value = 2m }
                }
            }, userId);
            await ahpSvc.CalculateAsync(batchId);

            var topsisCalc = new TOPSISCalculationService();
            var topsisSvc = new TOPSISService(db, topsisCalc, new SensitivityAnalysisService(topsisCalc));
            await topsisSvc.CalculateAsync(batchId);

            var decisionSvc = new DecisionResultService(db, topsisSvc, NullLogger<DecisionResultService>.Instance);
            await decisionSvc.ApproveAsync(batchId, sup1Id, userId, "Final approval on SQL Server");
        }

        // Attempt direct SQL UPDATE on EvaluationScores of the Completed batch -> Trigger TR_EvaluationScores_ValidateBatchScope must block it
        await using (var conn = new SqlConnection(_testConnString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE dbo.EvaluationScores SET ScoreValue = 999 WHERE BatchId = {batchId};";
            Func<Task> actDirectSql = async () => await cmd.ExecuteNonQueryAsync();
            await actDirectSql.Should().ThrowAsync<SqlException>()
                .WithMessage("*hoàn tất hoặc đã hủy*");
        }
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    [Trait("Category", "Integration")]
    public async Task SqlServer_F10_ConcurrentAdminDemotions_NeverLeaveZeroActiveAdministrators()
    {
        if (!EnsureSqlAvailable())
        {
            return;
        }

        int admin1Id, admin2Id;
        await using (var db = CreateSqlContext())
        {
            var a1 = new User
            {
                Username = "concurrent_admin_1",
                PasswordHash = "hash",
                FullName = "Concurrent Admin 1",
                Role = UserRole.Administrator,
                Status = EntityStatus.Active
            };
            var a2 = new User
            {
                Username = "concurrent_admin_2",
                PasswordHash = "hash",
                FullName = "Concurrent Admin 2",
                Role = UserRole.Administrator,
                Status = EntityStatus.Active
            };
            db.Users.AddRange(a1, a2);
            await db.SaveChangesAsync();
            admin1Id = a1.UserId;
            admin2Id = a2.UserId;
        }

        // Two concurrent threads each try to demote one of the two active admins
        var demote1 = Task.Run(async () =>
        {
            await using var db1 = CreateSqlContext();
            var svc1 = new UserService(db1);
            await svc1.UpdateAsync(new UserFormViewModel
            {
                UserId = admin1Id,
                Username = "concurrent_admin_1",
                FullName = "Concurrent Admin 1",
                Role = UserRole.Manager,
                Status = EntityStatus.Active
            }, currentUserId: admin2Id);
        });

        var demote2 = Task.Run(async () =>
        {
            await using var db2 = CreateSqlContext();
            var svc2 = new UserService(db2);
            await svc2.UpdateAsync(new UserFormViewModel
            {
                UserId = admin2Id,
                Username = "concurrent_admin_2",
                FullName = "Concurrent Admin 2",
                Role = UserRole.Manager,
                Status = EntityStatus.Active
            }, currentUserId: admin1Id);
        });

        var results = await Task.WhenAll(
            demote1.ContinueWith(t => t.Exception?.InnerException),
            demote2.ContinueWith(t => t.Exception?.InnerException));

        results.Count(ex => ex == null).Should().Be(1, "Only one admin demotion may succeed");
        results.Count(ex => ex != null).Should().Be(1, "The second concurrent admin demotion must be rejected");

        await using (var verifyDb = CreateSqlContext())
        {
            var remainingActiveAdmins = await verifyDb.Users
                .CountAsync(u => u.Role == UserRole.Administrator && u.Status == EntityStatus.Active);
            remainingActiveAdmins.Should().BeGreaterThanOrEqualTo(1, "System invariant: at least 1 active Administrator must remain");
        }
    }
}
