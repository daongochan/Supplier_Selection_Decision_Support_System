using System.Net;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Integration;

public class AuditFindingsRegressionTests : IDisposable
{
    private readonly string _tempReportsDir;

    public AuditFindingsRegressionTests()
    {
        _tempReportsDir = Path.Combine(Path.GetTempPath(), "DSS_AuditRegression_" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(_tempReportsDir, "App_Data", "reports"));
        Directory.CreateDirectory(Path.Combine(_tempReportsDir, "wwwroot", "reports"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempReportsDir))
            {
                Directory.Delete(_tempReportsDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private static ApplicationDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? ("AuditRegressionDb_" + Guid.NewGuid()))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class SharedDbWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "SharedHttpTestDb_" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                var efDescriptors = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                        d.ServiceType == typeof(DbContextOptions) ||
                        (d.ServiceType.IsGenericType &&
                         d.ServiceType.GetGenericTypeDefinition().FullName != null &&
                         d.ServiceType.GetGenericTypeDefinition().FullName!.Contains("IDbContextOptionsConfiguration")))
                    .ToList();

                foreach (var d in efDescriptors)
                {
                    services.Remove(d);
                }

                var internalSp = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName)
                        .UseInternalServiceProvider(internalSp)
                        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        }
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            match = Regex.Match(
                html,
                @"type=""hidden""\s+name=""__RequestVerificationToken""\s+value=""([^""]+)""",
                RegexOptions.IgnoreCase);
        }
        match.Success.Should().BeTrue("Expected antiforgery hidden input in HTML form");
        return match.Groups[1].Value;
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(
        SharedDbWebApplicationFactory factory,
        string username,
        string password,
        string? returnUrl = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var loginUrl = string.IsNullOrEmpty(returnUrl)
            ? "/Account/Login"
            : $"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
        var getLogin = await client.GetAsync(loginUrl);
        getLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginHtml = await getLogin.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(loginHtml);

        var formData = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Username"] = username,
            ["Password"] = password,
            ["RememberMe"] = "true"
        };
        if (!string.IsNullOrEmpty(returnUrl))
        {
            formData["ReturnUrl"] = returnUrl;
        }

        var postLogin = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(formData));
        postLogin.StatusCode.Should().Be(HttpStatusCode.Redirect);
        if (!string.IsNullOrEmpty(returnUrl))
        {
            postLogin.Headers.Location!.ToString().Should().Be(returnUrl);
        }

        return client;
    }

    // =========================================================
    // F01 & F08: DOM XSS SINK & PRODUCT GROUP AJAX ENDPOINT
    // =========================================================

    [Fact]
    public async Task F01_F08_SuppliersByGroup_And_CreateView_DoNotUseUnsafeInnerHtml_AndPreserveSelection()
    {
        await using var factory = new SharedDbWebApplicationFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(new User
            {
                UserId = 1,
                Username = "admin_f01",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "Admin F01",
                Role = UserRole.Administrator,
                Status = EntityStatus.Active
            });
            var group = new ProductGroup
            {
                GroupId = 10,
                GroupCode = "PG10",
                GroupName = "Test Group",
                Status = EntityStatus.Active
            };
            var xssSupplier = new Supplier
            {
                SupplierId = 100,
                SupplierCode = "SUP-XSS",
                SupplierName = "\"><img src=x onerror=window.__xss=1><svg onload=window.__xss2=1>",
                Status = EntityStatus.Active
            };
            group.Suppliers.Add(xssSupplier);
            db.ProductGroups.Add(group);
            await db.SaveChangesAsync();
        }

        var client = await CreateAuthenticatedClientAsync(factory, "admin_f01", "Admin@123");

        // Verify Create.cshtml rendered script uses createElement/textContent and AbortController + sequence guard
        var createResp = await client.GetAsync("/EvaluationBatch/Create");
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var createHtml = await createResp.Content.ReadAsStringAsync();

        createHtml.Should().Contain("AbortController");
        createHtml.Should().Contain("latestSupplierRequestSeq");
        createHtml.Should().Contain("document.createElement(\"label\")");
        createHtml.Should().Contain("codeStrong.textContent = String(supplier.supplierCode");
        createHtml.Should().Contain("nameSpan.textContent = String(supplier.supplierName");
        createHtml.Should().NotContain("supplierContainer.innerHTML =");

        // Verify JSON endpoint returns raw text safely serialized as application/json
        var jsonResp = await client.GetAsync("/EvaluationBatch/SuppliersByGroup?groupId=10");
        jsonResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonText = await jsonResp.Content.ReadAsStringAsync();
        jsonText.Should().Contain("SUP-XSS");
    }

    // =========================================================
    // F02: SCORE MODEL BINDING & PARTIAL / EXPLICIT CLEAR SEMANTICS
    // =========================================================

    [Fact]
    public async Task F02_EvaluationSave_HttpPost_InvalidInput_DoesNotDeleteExistingScore_And_ExplicitClear_Works()
    {
        await using var factory = new SharedDbWebApplicationFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(new User
            {
                UserId = 1,
                Username = "admin_f02",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "Admin F02",
                Role = UserRole.Administrator,
                Status = EntityStatus.Active
            });

            var group = new ProductGroup { GroupId = 1, GroupCode = "PG01", GroupName = "Group F02", Status = EntityStatus.Active };
            var sup1 = new Supplier { SupplierId = 1, SupplierCode = "S01", SupplierName = "Supplier 1", Status = EntityStatus.Active };
            var sup2 = new Supplier { SupplierId = 2, SupplierCode = "S02", SupplierName = "Supplier 2", Status = EntityStatus.Active };
            var crit1 = new Criterion { CriteriaId = 10, CriteriaCode = "C01", CriteriaName = "Price", CriteriaType = "Cost", Unit = "USD", Status = EntityStatus.Active };
            var crit2 = new Criterion { CriteriaId = 20, CriteriaCode = "C02", CriteriaName = "Quality", CriteriaType = "Benefit", Unit = "Score", Status = EntityStatus.Active };

            var batch = new EvaluationBatch
            {
                BatchId = 50,
                BatchCode = "B050",
                BatchName = "Batch F02",
                GroupId = 1,
                Group = group,
                Status = BatchStatus.Evaluating,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };
            batch.Suppliers.Add(sup1);
            batch.Suppliers.Add(sup2);
            batch.Criteria.Add(crit1);
            batch.Criteria.Add(crit2);
            db.ProductGroups.Add(group);
            db.EvaluationBatches.Add(batch);

            db.EvaluationScores.AddRange(
                new EvaluationScore { BatchId = 50, SupplierId = 1, CriteriaId = 10, ScoreValue = 120m, EvaluatedBy = 1, EvaluatedAt = DateTime.UtcNow },
                new EvaluationScore { BatchId = 50, SupplierId = 1, CriteriaId = 20, ScoreValue = 8.5m, EvaluatedBy = 1, EvaluatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }

        var client = await CreateAuthenticatedClientAsync(factory, "admin_f02", "Admin@123");
        var getPage = await client.GetAsync("/Evaluation?batchId=50");
        getPage.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await getPage.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        // 1. Submit invalid non-numeric string "abc" -> must return View with error and MUST NOT delete existing score 120m
        var invalidForm = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["BatchId"] = "50",
            ["Rows[0].SupplierId"] = "1",
            ["Rows[0].Scores[10]"] = "abc",
            ["Rows[0].Scores[20]"] = "8.5"
        };
        var invalidPost = await client.PostAsync("/Evaluation/Save", new FormUrlEncodedContent(invalidForm));
        invalidPost.StatusCode.Should().Be(HttpStatusCode.OK); // Rerenders Index view with validation errors
        var invalidBody = await invalidPost.Content.ReadAsStringAsync();
        invalidBody.Should().Contain("abc", "Invalid raw input should be preserved so the user can fix it");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var scoreAfterInvalid = await db.EvaluationScores
                .FirstOrDefaultAsync(x => x.BatchId == 50 && x.SupplierId == 1 && x.CriteriaId == 10);
            scoreAfterInvalid.Should().NotBeNull("Invalid model binding must NEVER delete existing saved score");
            scoreAfterInvalid!.ScoreValue.Should().Be(120m);
        }

        // 2. Partial submission (missing CriteriaId 20) without ClearedCells -> must keep CriteriaId 20 intact
        var partialForm = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["BatchId"] = "50",
            ["Rows[0].SupplierId"] = "1",
            ["Rows[0].Scores[10]"] = "115"
        };
        var partialPost = await client.PostAsync("/Evaluation/Save", new FormUrlEncodedContent(partialForm));
        partialPost.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var s10 = await db.EvaluationScores.FirstOrDefaultAsync(x => x.BatchId == 50 && x.SupplierId == 1 && x.CriteriaId == 10);
            var s20 = await db.EvaluationScores.FirstOrDefaultAsync(x => x.BatchId == 50 && x.SupplierId == 1 && x.CriteriaId == 20);
            s10!.ScoreValue.Should().Be(115m);
            s20.Should().NotBeNull("Omitted cell without explicit clear flag must remain intact");
            s20!.ScoreValue.Should().Be(8.5m);
        }

        // 3. Explicit clear via ClearedCells[20] = true -> deletes CriteriaId 20
        var clearForm = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["BatchId"] = "50",
            ["Rows[0].SupplierId"] = "1",
            ["Rows[0].Scores[10]"] = "115",
            ["Rows[0].Scores[20]"] = "",
            ["Rows[0].ClearedCells[20]"] = "true"
        };
        var clearPost = await client.PostAsync("/Evaluation/Save", new FormUrlEncodedContent(clearForm));
        clearPost.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var s20AfterClear = await db.EvaluationScores.FirstOrDefaultAsync(x => x.BatchId == 50 && x.SupplierId == 1 && x.CriteriaId == 20);
            s20AfterClear.Should().BeNull("Explicitly cleared cell should be removed");
        }
    }

    [Fact]
    public async Task F02_EvaluationService_NoOpSave_DoesNotInvalidateExistingTopsisResults()
    {
        await using var db = CreateInMemoryContext();
        var sup = new Supplier { SupplierId = 1, SupplierCode = "S01", SupplierName = "Sup 1", Status = EntityStatus.Active };
        var crit = new Criterion { CriteriaId = 10, CriteriaCode = "C01", CriteriaName = "Price", CriteriaType = "Cost", Status = EntityStatus.Active };
        var batch = new EvaluationBatch { BatchId = 60, BatchCode = "B060", BatchName = "Batch NoOp", Status = BatchStatus.Evaluating };
        batch.Suppliers.Add(sup);
        batch.Criteria.Add(crit);
        db.EvaluationBatches.Add(batch);
        db.EvaluationScores.Add(new EvaluationScore { BatchId = 60, SupplierId = 1, CriteriaId = 10, ScoreValue = 100m, EvaluatedBy = 1, EvaluatedAt = DateTime.UtcNow });
        db.TopsisResults.Add(new TopsisResult { BatchId = 60, SupplierId = 1, RankNo = 1, ClosenessCoefficient = 1.0m });
        await db.SaveChangesAsync();

        var service = new EvaluationService(db);

        // Save identical score 100m -> No-op save must preserve TopsisResults
        await service.SaveAsync(new EvaluationScoreInputViewModel
        {
            BatchId = 60,
            Rows = new List<EvaluationScoreInputRowViewModel>
            {
                new()
                {
                    SupplierId = 1,
                    Scores = new Dictionary<int, decimal?> { { 10, 100m } }
                }
            }
        }, 1);

        var topsisRemaining = await db.TopsisResults.Where(x => x.BatchId == 60).ToListAsync();
        topsisRemaining.Should().ContainSingle("No-op score save must not wipe valid TOPSIS results");
    }

    // =========================================================
    // F03: AGGREGATE CONCURRENCY PROTOCOL (STALE ROWVERSION REJECTION)
    // =========================================================

    [Fact]
    public async Task F03_StaleRowVersion_IsRejectedAcrossAllBatchMutations()
    {
        await using var db = CreateInMemoryContext();
        var sup1 = new Supplier { SupplierId = 1, SupplierCode = "S01", SupplierName = "Sup 1", Status = EntityStatus.Active };
        var sup2 = new Supplier { SupplierId = 2, SupplierCode = "S02", SupplierName = "Sup 2", Status = EntityStatus.Active };
        var crit1 = new Criterion { CriteriaId = 10, CriteriaCode = "C01", CriteriaName = "Price", CriteriaType = "Cost", Status = EntityStatus.Active };
        var crit2 = new Criterion { CriteriaId = 20, CriteriaCode = "C02", CriteriaName = "Quality", CriteriaType = "Benefit", Status = EntityStatus.Active };

        var currentVersionBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 5 };
        var staleVersionBase64 = Convert.ToBase64String(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });

        var batch = new EvaluationBatch
        {
            BatchId = 70,
            BatchCode = "B070",
            BatchName = "Batch Concurrency",
            Status = BatchStatus.Evaluating,
            RowVersion = currentVersionBytes
        };
        batch.Suppliers.Add(sup1);
        batch.Suppliers.Add(sup2);
        batch.Criteria.Add(crit1);
        batch.Criteria.Add(crit2);
        db.EvaluationBatches.Add(batch);
        db.TopsisResults.AddRange(
            new TopsisResult { BatchId = 70, SupplierId = 1, RankNo = 1, ClosenessCoefficient = 0.8m },
            new TopsisResult { BatchId = 70, SupplierId = 2, RankNo = 2, ClosenessCoefficient = 0.4m });
        await db.SaveChangesAsync();

        var evalService = new EvaluationService(db);
        var ahpService = new AHPService(db, new AHPCalculationService());
        var topsisCalc = new TOPSISCalculationService();
        var topsisService = new TOPSISService(db, topsisCalc, new SensitivityAnalysisService(topsisCalc));
        var decisionService = new DecisionResultService(db, topsisService, NullLogger<DecisionResultService>.Instance);
        var batchService = new EvaluationBatchService(db);

        // 1. Score save with stale RowVersion
        var actScore = () => evalService.SaveAsync(new EvaluationScoreInputViewModel
        {
            BatchId = 70,
            RowVersion = staleVersionBase64,
            Rows = new List<EvaluationScoreInputRowViewModel>
            {
                new() { SupplierId = 1, Scores = new Dictionary<int, decimal?> { { 10, 50m } } }
            }
        }, 1);
        await actScore.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Dữ liệu đợt đánh giá vừa được người khác thay đổi*");

        // 2. AHP SaveComparisons with stale RowVersion
        var actAhpSave = () => ahpService.SaveComparisonsAsync(new AHPComparisonInputViewModel
        {
            BatchId = 70,
            RowVersion = staleVersionBase64,
            Comparisons = new List<AHPComparisonInputItemViewModel>
            {
                new() { CriteriaRowId = 10, CriteriaColId = 20, Value = 3m }
            }
        }, 1);
        await actAhpSave.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Dữ liệu đợt đánh giá vừa được người khác thay đổi*");

        // 3. AHP Calculate with stale RowVersion
        var actAhpCalc = () => ahpService.CalculateAsync(70, staleVersionBase64);
        await actAhpCalc.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Dữ liệu đợt đánh giá vừa được người khác thay đổi*");

        // 4. TOPSIS Calculate with stale RowVersion
        var actTopsisCalc = () => topsisService.CalculateAsync(70, staleVersionBase64);
        await actTopsisCalc.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Dữ liệu đợt đánh giá vừa được người khác thay đổi*");

        // 5. Decision Approve / Reject / Reevaluate with stale RowVersion
        var actApprove = () => decisionService.ApproveAsync(70, 1, 1, "OK", staleVersionBase64);
        await actApprove.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Dữ liệu đợt đánh giá vừa được người khác thay đổi*");

        var actReject = () => decisionService.RejectAsync(70, "No", 1, staleVersionBase64);
        await actReject.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Dữ liệu đợt đánh giá vừa được người khác thay đổi*");

        var actReeval = () => decisionService.RequestReevaluationAsync(70, "Redo", 1, staleVersionBase64);
        await actReeval.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Dữ liệu đợt đánh giá vừa được người khác thay đổi*");

        // 6. Batch Cancel with stale RowVersion (CancelAsync signature: batchId, reason, expectedRowVersion)
        var actCancel = () => batchService.CancelAsync(70, "Cancel reason", staleVersionBase64);
        await actCancel.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Dữ liệu đợt đánh giá vừa được người khác thay đổi*");
    }

    // =========================================================
    // F04: COOKIE PRINCIPAL VALIDATION (ROLE / STATUS / PASSWORD REVOCATION)
    // =========================================================

    [Fact]
    public async Task F04_CookieSession_IsImmediatelyRevokedOrUpdated_OnLock_Deactivate_PasswordChange_Or_RoleDemotion()
    {
        await using var factory = new SharedDbWebApplicationFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.AddRange(
                new User
                {
                    UserId = 1,
                    Username = "root_admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "Root Admin",
                    Role = UserRole.Administrator,
                    Status = EntityStatus.Active
                },
                new User
                {
                    UserId = 2,
                    Username = "SecondAdmin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "Second Admin",
                    Role = UserRole.Administrator,
                    Status = EntityStatus.Active
                });
            await db.SaveChangesAsync();
        }

        // Authenticate SecondAdmin and verify ReturnUrl redirection works
        var client = await CreateAuthenticatedClientAsync(factory, "SecondAdmin", "Admin@123", "/User");

        // Initially SecondAdmin has Administrator access to /User
        var initialResp = await client.GetAsync("/User");
        initialResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 1. Demote SecondAdmin to PurchasingStaff in DB -> same cookie must immediately lose access to /User (403/Redirect to AccessDenied)
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var u = await db.Users.FindAsync(2);
            u!.Role = UserRole.PurchasingStaff;
            await db.SaveChangesAsync();
        }

        var afterDemoteResp = await client.GetAsync("/User");
        afterDemoteResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        afterDemoteResp.Headers.Location!.ToString().Should().Contain("/Account/AccessDenied");

        // But SecondAdmin is still authenticated for PurchasingStaff routes (e.g. /Supplier)
        var supplierResp = await client.GetAsync("/Supplier");
        supplierResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Change SecondAdmin password hash -> same cookie must be rejected and redirected to /Account/Login
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var u = await db.Users.FindAsync(2);
            u!.PasswordHash = BCrypt.Net.BCrypt.HashPassword("NewPassword@456");
            await db.SaveChangesAsync();
        }

        var afterPwdChangeResp = await client.GetAsync("/Supplier");
        afterPwdChangeResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        afterPwdChangeResp.Headers.Location!.ToString().Should().Contain("/Account/Login");

        // 3. Log back in with new password, then lock account -> same cookie must be rejected
        var client2 = await CreateAuthenticatedClientAsync(factory, "SecondAdmin", "NewPassword@456");
        (await client2.GetAsync("/Supplier")).StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var u = await db.Users.FindAsync(2);
            u!.Status = EntityStatus.Locked;
            await db.SaveChangesAsync();
        }

        var afterLockResp = await client2.GetAsync("/Supplier");
        afterLockResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        afterLockResp.Headers.Location!.ToString().Should().Contain("/Account/Login");
    }

    // =========================================================
    // F05 & F06: IMMUTABLE DECISION HISTORY & FROZEN SNAPSHOT REPRODUCIBILITY
    // =========================================================

    [Fact]
    public async Task F05_F06_MultiRoundWorkflow_PreservesAppendOnlyHistory_And_FrozenSnapshotSurvivesMasterDataRename()
    {
        await using var db = CreateInMemoryContext();

        var group = new ProductGroup { GroupId = 1, GroupCode = "PG01", GroupName = "Industrial Valves", Status = EntityStatus.Active };
        var sup1 = new Supplier { SupplierId = 10, SupplierCode = "SUP-ALPHA", SupplierName = "Alpha Original Ltd", Status = EntityStatus.Active };
        var sup2 = new Supplier { SupplierId = 20, SupplierCode = "SUP-BETA", SupplierName = "Beta Original Corp", Status = EntityStatus.Active };
        var crit1 = new Criterion { CriteriaId = 100, CriteriaCode = "C-COST", CriteriaName = "Unit Cost Original", CriteriaType = "Cost", Unit = "USD", Status = EntityStatus.Active };
        var crit2 = new Criterion { CriteriaId = 200, CriteriaCode = "C-QUAL", CriteriaName = "Quality Original", CriteriaType = "Benefit", Unit = "Pts", Status = EntityStatus.Active };

        var manager = new User
        {
            UserId = 5,
            Username = "manager1",
            PasswordHash = "hash",
            FullName = "Executive Manager",
            Role = UserRole.Manager,
            Status = EntityStatus.Active
        };

        var batch = new EvaluationBatch
        {
            BatchId = 500,
            BatchCode = "BATCH-500",
            BatchName = "Q4 Valve Sourcing",
            GroupId = 1,
            Group = group,
            Status = BatchStatus.Evaluating,
            CreatedBy = 5,
            CreatedAt = DateTime.UtcNow
        };
        batch.Suppliers.Add(sup1);
        batch.Suppliers.Add(sup2);
        batch.Criteria.Add(crit1);
        batch.Criteria.Add(crit2);

        db.ProductGroups.Add(group);
        db.Users.Add(manager);
        db.EvaluationBatches.Add(batch);
        await db.SaveChangesAsync();

        var evalService = new EvaluationService(db);
        var ahpCalc = new AHPCalculationService();
        var ahpService = new AHPService(db, ahpCalc);
        var topsisCalc = new TOPSISCalculationService();
        var sensService = new SensitivityAnalysisService(topsisCalc);
        var topsisService = new TOPSISService(db, topsisCalc, sensService);
        var decisionService = new DecisionResultService(db, topsisService, NullLogger<DecisionResultService>.Instance);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_tempReportsDir);
        mockEnv.Setup(e => e.WebRootPath).Returns(Path.Combine(_tempReportsDir, "wwwroot"));
        var mockConfig = new Mock<IConfiguration>();
        var reportService = new ReportService(
            db,
            mockEnv.Object,
            new AuditLogService(db),
            mockConfig.Object,
            NullLogger<ReportService>.Instance,
            topsisService);

        // --- ROUND 1 ---
        await evalService.SaveAsync(new EvaluationScoreInputViewModel
        {
            BatchId = 500,
            Rows = new List<EvaluationScoreInputRowViewModel>
            {
                new() { SupplierId = 10, Scores = new Dictionary<int, decimal?> { { 100, 200m }, { 200, 80m } } },
                new() { SupplierId = 20, Scores = new Dictionary<int, decimal?> { { 100, 180m }, { 200, 85m } } }
            }
        }, 5);

        await ahpService.SaveComparisonsAsync(new AHPComparisonInputViewModel
        {
            BatchId = 500,
            Comparisons = new List<AHPComparisonInputItemViewModel>
            {
                new() { CriteriaRowId = 100, CriteriaColId = 200, Value = 2m }
            }
        }, 5);
        await ahpService.CalculateAsync(500);
        await topsisService.CalculateAsync(500);

        // Reject in Round 1
        await decisionService.RejectAsync(500, "Round 1 prices need renegotiation", 5);
        // Request Re-evaluation (archives Round 1 snapshot and opens Round 2)
        await decisionService.RequestReevaluationAsync(500, "Re-open for Round 2 best and final offers", 5);

        // --- ROUND 2 ---
        await evalService.SaveAsync(new EvaluationScoreInputViewModel
        {
            BatchId = 500,
            Rows = new List<EvaluationScoreInputRowViewModel>
            {
                new() { SupplierId = 10, Scores = new Dictionary<int, decimal?> { { 100, 150m }, { 200, 95m } } },
                new() { SupplierId = 20, Scores = new Dictionary<int, decimal?> { { 100, 190m }, { 200, 82m } } }
            }
        }, 5);

        await ahpService.SaveComparisonsAsync(new AHPComparisonInputViewModel
        {
            BatchId = 500,
            Comparisons = new List<AHPComparisonInputItemViewModel>
            {
                new() { CriteriaRowId = 100, CriteriaColId = 200, Value = 1m }
            }
        }, 5);
        await ahpService.CalculateAsync(500);
        await topsisService.CalculateAsync(500);

        // Final Approval of Supplier 10 (Rank #1 in Round 2)
        await decisionService.ApproveAsync(500, 10, 5, "Approved Alpha after Round 2 BAFO");

        // Now mutate master data names in DB AFTER batch completion
        sup1.SupplierName = "MUTATED SUPPLIER NAME AFTER APPROVAL";
        crit1.CriteriaName = "MUTATED CRITERION NAME AFTER APPROVAL";
        await db.SaveChangesAsync();

        // Verify DecisionResultService preserves full 3-event append-only history AND reconstructs frozen names
        var resultVm = await decisionService.GetResultAsync(500);
        resultVm.Should().NotBeNull();
        resultVm!.IsFrozenFromSnapshot.Should().BeTrue();
        resultVm.CurrentRoundNumber.Should().Be(2);
        resultVm.SelectedSupplierName.Should().Be("Alpha Original Ltd");
        resultVm.Ranking.First(r => r.SupplierId == 10).SupplierName.Should().Be("Alpha Original Ltd");

        resultVm.DecisionHistory.Should().HaveCount(3, "Rejected, PendingReevaluation, and Approved events must all be preserved");
        resultVm.DecisionHistory.Select(h => h.Status).Should().Contain(new[]
        {
            ApprovalStatus.Rejected,
            ApprovalStatus.PendingReevaluation,
            ApprovalStatus.Approved
        });
        resultVm.RoundSnapshots.Should().HaveCount(2, "Both Round 1 Reevaluation and Round 2 FinalApproval snapshots must exist");

        // Verify Excel Report generated AFTER master-data rename still contains frozen historical names
        var excelReport = await reportService.GenerateExcelAsync(500, 5);
        excelReport.Should().NotBeNull();

        using var doc = SpreadsheetDocument.Open(excelReport.AbsolutePath, false);
        var sharedStrings = doc.WorkbookPart!.SharedStringTablePart?.SharedStringTable
            .Elements<SharedStringItem>()
            .Select(s => s.InnerText)
            .ToArray() ?? Array.Empty<string>();

        var allCellTexts = doc.WorkbookPart.WorksheetParts
            .SelectMany(ws => ws.Worksheet.Descendants<Cell>())
            .Select(c =>
            {
                var raw = c.InnerText ?? string.Empty;
                if (c.DataType != null && c.DataType.Value == CellValues.SharedString &&
                    int.TryParse(raw, out var sstIdx) && sstIdx >= 0 && sstIdx < sharedStrings.Length)
                {
                    return sharedStrings[sstIdx];
                }
                return raw;
            })
            .ToList();
        var combinedSheetText = string.Join(" | ", allCellTexts);

        combinedSheetText.Should().Contain("Alpha Original Ltd");
        combinedSheetText.Should().Contain("Unit Cost Original");
        combinedSheetText.Should().NotContain("MUTATED SUPPLIER NAME AFTER APPROVAL");
        combinedSheetText.Should().NotContain("MUTATED CRITERION NAME AFTER APPROVAL");
    }
}
