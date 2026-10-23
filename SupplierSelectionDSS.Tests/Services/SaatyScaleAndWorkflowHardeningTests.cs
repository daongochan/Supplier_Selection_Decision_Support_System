using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Controllers;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Services;

public class SaatyScaleAndWorkflowHardeningTests
{
    // =========================================================
    // 1. SAATY SCALE TESTS
    // =========================================================

    [Theory]
    [InlineData("1/9", 0.1111, 9.0000, "9")]
    [InlineData("1/8", 0.1250, 8.0000, "8")]
    [InlineData("1/7", 0.1429, 7.0000, "7")]
    [InlineData("1/6", 0.1667, 6.0000, "6")]
    [InlineData("1/5", 0.2000, 5.0000, "5")]
    [InlineData("1/4", 0.2500, 4.0000, "4")]
    [InlineData("1/3", 0.3333, 3.0000, "3")]
    [InlineData("1/2", 0.5000, 2.0000, "2")]
    [InlineData("1",   1.0000, 1.0000, "1")]
    [InlineData("2",   2.0000, 0.5000, "1/2")]
    [InlineData("3",   3.0000, 0.3333, "1/3")]
    [InlineData("4",   4.0000, 0.2500, "1/4")]
    [InlineData("5",   5.0000, 0.2000, "1/5")]
    [InlineData("6",   6.0000, 0.1667, "1/6")]
    [InlineData("7",   7.0000, 0.1429, "1/7")]
    [InlineData("8",   8.0000, 0.1250, "1/8")]
    [InlineData("9",   9.0000, 0.1111, "1/9")]
    public void SaatyScale_AllSeventeenValues_MapSymmetricallyWithoutDrift(
        string code,
        decimal expectedDirect,
        decimal expectedReciprocal,
        string expectedReciprocalCode)
    {
        var success = SaatyScale.TryParse(code, out var item);

        success.Should().BeTrue();
        item.Code.Should().Be(code);
        item.DirectValue.Should().Be(expectedDirect);
        item.ReciprocalValue.Should().Be(expectedReciprocal);
        item.ReciprocalCode.Should().Be(expectedReciprocalCode);

        // Verification of database constraint: ComparisonValue BETWEEN 0.1111 AND 9
        item.DirectValue.Should().BeInRange(0.1111m, 9.0000m);
        item.ReciprocalValue.Should().BeInRange(0.1111m, 9.0000m);

        // Verification of reciprocal function
        SaatyScale.GetReciprocalDecimal(item.DirectValue).Should().Be(expectedReciprocal);
        SaatyScale.GetReciprocalCode(code).Should().Be(expectedReciprocalCode);
    }

    [Fact]
    public void SaatyScale_TryParse_ReturnsFalseForInvalidOrEmptyStrings()
    {
        SaatyScale.TryParse("", out _).Should().BeFalse();
        SaatyScale.TryParse(null, out _).Should().BeFalse();
        SaatyScale.TryParse("0", out _).Should().BeFalse();
        SaatyScale.TryParse("10", out _).Should().BeFalse();
        SaatyScale.TryParse("0.3333", out _).Should().BeFalse();
        SaatyScale.TryParse("xyz", out _).Should().BeFalse();
    }

    [Fact]
    public void SaatyScale_GetComparisonExplanation_GeneratesDescriptiveHumanText()
    {
        var expEqual = SaatyScale.GetComparisonExplanation("Price", "Quality", "1");
        expEqual.Should().Be("'Price' and 'Quality' are equally important (1)");

        var expDirect = SaatyScale.GetComparisonExplanation("Price", "Quality", "3");
        expDirect.Should().Be("'Price' is moderately more important than 'Quality' (3)");

        var expInverse = SaatyScale.GetComparisonExplanation("Price", "Quality", "1/3");
        expInverse.Should().Be("'Quality' is moderately more important than 'Price' (3)");
    }

    // =========================================================
    // 2. AHP SERVICE SCALE INTEGRATION TESTS
    // =========================================================

    [Fact]
    public async Task SaveComparisonsAsync_WithScaleCodeOneThird_StoresExactReciprocalThree()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("SaatyAHPTestDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new ApplicationDbContext(options);

        var batch = new EvaluationBatch
        {
            BatchId = 1,
            BatchCode = "BATCH-01",
            BatchName = "Test Batch",
            Status = BatchStatus.Evaluating.ToString(),
            RowVersion = new byte[] { 1, 0, 0, 0 }
        };
        context.EvaluationBatches.Add(batch);

        var c1 = new Criterion { CriteriaId = 10, CriteriaCode = "C1", CriteriaName = "Price", CriteriaType = "Benefit", Status = EntityStatus.Active };
        var c2 = new Criterion { CriteriaId = 20, CriteriaCode = "C2", CriteriaName = "Quality", CriteriaType = "Benefit", Status = EntityStatus.Active };
        context.Criteria.AddRange(c1, c2);
        batch.Criteria.Add(c1);
        batch.Criteria.Add(c2);
        await context.SaveChangesAsync();

        var ahpCalc = new AHPCalculationService();
        var ahpService = new AHPService(context, ahpCalc);

        var input = new AHPComparisonInputViewModel
        {
            BatchId = 1,
            RowVersion = Convert.ToBase64String(batch.RowVersion),
            Comparisons = new List<AHPComparisonInputItemViewModel>
            {
                new()
                {
                    CriteriaRowId = 10,
                    CriteriaColId = 20,
                    ScaleCode = "1/3" // User selected 1/3 directly
                }
            }
        };

        await ahpService.SaveComparisonsAsync(input, evaluatedBy: 1);

        var savedComparisons = await context.AhppairwiseComparisons
            .Where(x => x.BatchId == 1)
            .ToListAsync();

        savedComparisons.Should().HaveCount(4); // 2 diagonal + 1 direct + 1 reciprocal

        var direct = savedComparisons.First(x => x.CriteriaRowId == 10 && x.CriteriaColId == 20);
        direct.ComparisonValue.Should().Be(0.3333m);

        var reciprocal = savedComparisons.First(x => x.CriteriaRowId == 20 && x.CriteriaColId == 10);
        reciprocal.ComparisonValue.Should().Be(3.0000m); // Exact 3, not 3.0003!
    }

    [Fact]
    public async Task SaveComparisonsAsync_WithScaleCodeOneNinth_StoresExactReciprocalNineWithoutExceedingConstraint()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("SaatyAHPTestDb_Ninth_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new ApplicationDbContext(options);

        var batch = new EvaluationBatch
        {
            BatchId = 2,
            BatchCode = "BATCH-02",
            BatchName = "Extreme Test",
            Status = BatchStatus.Evaluating.ToString(),
            RowVersion = new byte[] { 2, 0, 0, 0 }
        };
        context.EvaluationBatches.Add(batch);

        var c1 = new Criterion { CriteriaId = 1, CriteriaCode = "C1", CriteriaName = "A", CriteriaType = "Benefit", Status = EntityStatus.Active };
        var c2 = new Criterion { CriteriaId = 2, CriteriaCode = "C2", CriteriaName = "B", CriteriaType = "Benefit", Status = EntityStatus.Active };
        context.Criteria.AddRange(c1, c2);
        batch.Criteria.Add(c1);
        batch.Criteria.Add(c2);
        await context.SaveChangesAsync();

        var ahpCalc = new AHPCalculationService();
        var ahpService = new AHPService(context, ahpCalc);

        var input = new AHPComparisonInputViewModel
        {
            BatchId = 2,
            RowVersion = Convert.ToBase64String(batch.RowVersion),
            Comparisons = new List<AHPComparisonInputItemViewModel>
            {
                new()
                {
                    CriteriaRowId = 1,
                    CriteriaColId = 2,
                    ScaleCode = "1/9"
                }
            }
        };

        await ahpService.SaveComparisonsAsync(input, evaluatedBy: 1);

        var saved = await context.AhppairwiseComparisons.Where(x => x.BatchId == 2).ToListAsync();
        var reciprocal = saved.First(x => x.CriteriaRowId == 2 && x.CriteriaColId == 1);
        reciprocal.ComparisonValue.Should().Be(9.0000m); // Exact 9, NOT 9.0009m!
    }

    // =========================================================
    // 3. AUDIT LOG SENSITIVE DATA SANITIZATION
    // =========================================================

    [Fact]
    public void AuditLogService_SanitizeSensitiveData_MasksPasswordAndTokens()
    {
        var rawJson = "{\"Username\":\"admin\",\"PasswordHash\":\"$2a$11$secretHashStringHere\",\"token\":\"jwt.token.abc\"}";

        var sanitized = AuditLogService.SanitizeSensitiveData(rawJson);

        sanitized.Should().NotContain("$2a$11$secretHashStringHere");
        sanitized.Should().NotContain("jwt.token.abc");
        sanitized.Should().Contain("\"PasswordHash\": \"********\"");
        sanitized.Should().Contain("\"token\": \"********\"");
        sanitized.Should().Contain("\"Username\":\"admin\"");
    }

    // =========================================================
    // 4. REPORT CONTROLLER ROUTING BACKWARD COMPATIBILITY
    // =========================================================

    [Fact]
    public void ReportController_ViewReport_RedirectsToIndexWithBatchId()
    {
        var controller = new ReportController(null!, null!);

        var result = controller.ViewReport(batchId: 11, id: null) as RedirectToActionResult;

        result.Should().NotBeNull();
        result!.ActionName.Should().Be("Index");
        result.RouteValues.Should().ContainKey("batchId");
        result.RouteValues!["batchId"].Should().Be(11);
    }

    [Fact]
    public void ReportController_ViewReport_ReturnsNotFound_WhenBatchIdMissing()
    {
        var controller = new ReportController(null!, null!);

        var result = controller.ViewReport(batchId: null, id: 0);

        result.Should().BeOfType<NotFoundResult>();
    }
}
