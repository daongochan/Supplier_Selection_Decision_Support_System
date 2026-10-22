using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Services;

public class BusinessWorkflowHardeningTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public BusinessWorkflowHardeningTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "WorkflowHardening_" + Guid.NewGuid())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private DecisionResultService CreateDecisionResultService()
    {
        var calc = new TOPSISCalculationService();
        var sensitivity = new SensitivityAnalysisService(calc);
        var topsisService = new TOPSISService(_context, calc, sensitivity);
        return new DecisionResultService(_context, topsisService, NullLogger<DecisionResultService>.Instance);
    }

    // =========================================================
    // C4: APPROVAL WORKFLOW — REQUIRE REASON FOR NON-RANK-1
    // =========================================================

    [Fact]
    public async Task ApproveAsync_NonRank1SupplierWithoutReason_ThrowsBusinessRuleException()
    {
        // Arrange
        var batch = new EvaluationBatch
        {
            BatchId = 101,
            BatchCode = "B101",
            BatchName = "Đợt thử nghiệm",
            Status = BatchStatus.Evaluating
        };

        var sup1 = new Supplier { SupplierId = 1, SupplierCode = "S01", SupplierName = "Nhà cung cấp 1", Status = EntityStatus.Active };
        var sup2 = new Supplier { SupplierId = 2, SupplierCode = "S02", SupplierName = "Nhà cung cấp 2", Status = EntityStatus.Active };

        batch.Suppliers.Add(sup1);
        batch.Suppliers.Add(sup2);

        _context.EvaluationBatches.Add(batch);

        _context.TopsisResults.AddRange(
            new TopsisResult { BatchId = 101, SupplierId = 1, RankNo = 1, ClosenessCoefficient = 0.85m },
            new TopsisResult { BatchId = 101, SupplierId = 2, RankNo = 2, ClosenessCoefficient = 0.72m }
        );

        await _context.SaveChangesAsync();

        var service = CreateDecisionResultService();

        // Act
        var actEmpty = () => service.ApproveAsync(101, 2, 99, "");
        var actWhitespace = () => service.ApproveAsync(101, 2, 99, "   ");
        var actNull = () => service.ApproveAsync(101, 2, 99, null);

        // Assert
        await actEmpty.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*không phải Hạng 1*Bắt buộc phải nhập lý do*");
        await actWhitespace.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*không phải Hạng 1*Bắt buộc phải nhập lý do*");
        await actNull.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*không phải Hạng 1*Bắt buộc phải nhập lý do*");
    }

    [Fact]
    public async Task ApproveAsync_NonRank1SupplierWithReason_Succeeds()
    {
        // Arrange
        var batch = new EvaluationBatch
        {
            BatchId = 102,
            BatchCode = "B102",
            BatchName = "Đợt thử nghiệm 2",
            Status = BatchStatus.Evaluating
        };

        var sup1 = new Supplier { SupplierId = 11, SupplierCode = "S11", SupplierName = "Nhà cung cấp 11", Status = EntityStatus.Active };
        var sup2 = new Supplier { SupplierId = 12, SupplierCode = "S12", SupplierName = "Nhà cung cấp 12", Status = EntityStatus.Active };

        batch.Suppliers.Add(sup1);
        batch.Suppliers.Add(sup2);

        _context.EvaluationBatches.Add(batch);

        _context.TopsisResults.AddRange(
            new TopsisResult { BatchId = 102, SupplierId = 11, RankNo = 1, ClosenessCoefficient = 0.88m },
            new TopsisResult { BatchId = 102, SupplierId = 12, RankNo = 2, ClosenessCoefficient = 0.75m }
        );

        await _context.SaveChangesAsync();

        var service = CreateDecisionResultService();

        // Act
        var approvalId = await service.ApproveAsync(102, 12, 99, "NCC #2 có năng lực bảo hành vượt trội theo cam kết thực địa.");

        // Assert
        approvalId.Should().BeGreaterThan(0);
        var approval = await _context.Approvals.FindAsync(approvalId);
        approval.Should().NotBeNull();
        approval!.SelectedSupplierId.Should().Be(12);
        approval.Notes.Should().Contain("năng lực bảo hành");
    }

    [Fact]
    public async Task ApproveAsync_Rank1SupplierWithoutReason_Succeeds()
    {
        // Arrange
        var batch = new EvaluationBatch
        {
            BatchId = 103,
            BatchCode = "B103",
            BatchName = "Đợt thử nghiệm 3",
            Status = BatchStatus.Evaluating
        };

        var sup1 = new Supplier { SupplierId = 21, SupplierCode = "S21", SupplierName = "Nhà cung cấp 21", Status = EntityStatus.Active };
        var sup2 = new Supplier { SupplierId = 22, SupplierCode = "S22", SupplierName = "Nhà cung cấp 22", Status = EntityStatus.Active };
        batch.Suppliers.Add(sup1);
        batch.Suppliers.Add(sup2);

        _context.EvaluationBatches.Add(batch);
        _context.TopsisResults.AddRange(
            new TopsisResult { BatchId = 103, SupplierId = 21, RankNo = 1, ClosenessCoefficient = 0.92m },
            new TopsisResult { BatchId = 103, SupplierId = 22, RankNo = 2, ClosenessCoefficient = 0.65m }
        );

        await _context.SaveChangesAsync();

        var service = CreateDecisionResultService();

        // Act
        var approvalId = await service.ApproveAsync(103, 21, 99, null);

        // Assert
        approvalId.Should().BeGreaterThan(0);
        var approval = await _context.Approvals.FindAsync(approvalId);
        approval.Should().NotBeNull();
        approval!.SelectedSupplierId.Should().Be(21);
    }

    // =========================================================
    // C6: CRITERIA WORKFLOW — PREVENT EDITING UNIT IN USE
    // =========================================================

    [Fact]
    public async Task CriteriaService_UpdateAsync_WhenUnitChangedOnInUseCriterion_ThrowsInvalidOperationException()
    {
        // Arrange
        var criterion = new Criterion
        {
            CriteriaId = 501,
            CriteriaCode = "C501",
            CriteriaName = "Thời gian giao hàng",
            CriteriaType = "Cost",
            Unit = "Ngày",
            Status = EntityStatus.Active
        };
        _context.Criteria.Add(criterion);

        // Criterion is used in an evaluation score
        _context.EvaluationScores.Add(new EvaluationScore
        {
            ScoreId = 1,
            BatchId = 1,
            SupplierId = 1,
            CriteriaId = 501,
            ScoreValue = 5,
            EvaluatedBy = 1,
            EvaluatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var service = new CriteriaService(_context);

        // Act: attempt to change Unit from "Ngày" to "Giờ"
        var updated = new Criterion
        {
            CriteriaId = 501,
            CriteriaCode = "C501",
            CriteriaName = "Thời gian giao hàng",
            CriteriaType = "Cost",
            Unit = "Giờ",
            Status = EntityStatus.Active
        };

        var act = () => service.UpdateAsync(updated);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đơn vị tính*đã được sử dụng*");
    }

    [Fact]
    public async Task CriteriaService_UpdateAsync_WhenUnitUnchangedOrUnused_Succeeds()
    {
        // Arrange: unused criterion
        var criterion = new Criterion
        {
            CriteriaId = 502,
            CriteriaCode = "C502",
            CriteriaName = "Mức độ dịch vụ",
            CriteriaType = "Benefit",
            Unit = "Điểm",
            Status = EntityStatus.Active
        };
        _context.Criteria.Add(criterion);
        await _context.SaveChangesAsync();

        var service = new CriteriaService(_context);

        // Act: update Unit on unused criterion
        var updated = new Criterion
        {
            CriteriaId = 502,
            CriteriaCode = "C502",
            CriteriaName = "Mức độ dịch vụ",
            CriteriaType = "Benefit",
            Unit = "%",
            Status = EntityStatus.Active
        };

        await service.UpdateAsync(updated);

        // Assert
        var saved = await _context.Criteria.FindAsync(502);
        saved!.Unit.Should().Be("%");
    }

    // =========================================================
    // C2: DOWNSTREAM INVALIDATION ON SCORE SAVE
    // =========================================================

    [Fact]
    public async Task EvaluationService_SaveAsync_InvalidatesStalePendingApprovals()
    {
        // Arrange
        var batch = new EvaluationBatch
        {
            BatchId = 201,
            BatchCode = "B201",
            BatchName = "Đợt đánh giá invalidation",
            Status = BatchStatus.Evaluating
        };

        var sup = new Supplier { SupplierId = 31, SupplierCode = "S31", SupplierName = "Nhà cung cấp 31", Status = EntityStatus.Active };
        var crit = new Criterion { CriteriaId = 601, CriteriaCode = "C601", CriteriaName = "Giá", CriteriaType = "Cost", Unit = "VND", Status = EntityStatus.Active };

        batch.Suppliers.Add(sup);
        batch.Criteria.Add(crit);
        _context.EvaluationBatches.Add(batch);

        // Existing unfinalized approval
        _context.Approvals.Add(new Approval
        {
            ApprovalId = 10,
            BatchId = 201,
            SelectedSupplierId = 31,
            Status = ApprovalStatus.Approved,
            IsFinal = false,
            ApprovalDate = DateTime.UtcNow
        });

        // Existing TOPSIS result
        _context.TopsisResults.Add(new TopsisResult
        {
            BatchId = 201,
            SupplierId = 31,
            RankNo = 1,
            ClosenessCoefficient = 0.8m
        });

        await _context.SaveChangesAsync();

        var service = new EvaluationService(_context);

        var model = new EvaluationScoreInputViewModel
        {
            BatchId = 201,
            Rows = new List<EvaluationScoreInputRowViewModel>
            {
                new()
                {
                    SupplierId = 31,
                    Scores = new Dictionary<int, decimal?> { { 601, 1500000m } }
                }
            }
        };

        // Act: Save modified evaluation score
        await service.SaveAsync(model, 1);

        // Assert: Pending approvals and topsis results are cleanly invalidated
        var approvals = await _context.Approvals.Where(x => x.BatchId == 201).ToListAsync();
        approvals.Should().BeEmpty();

        var topsis = await _context.TopsisResults.Where(x => x.BatchId == 201).ToListAsync();
        topsis.Should().BeEmpty();
    }
}
