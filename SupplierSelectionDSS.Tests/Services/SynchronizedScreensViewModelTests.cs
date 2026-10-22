using FluentAssertions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using Xunit;

namespace SupplierSelectionDSS.Tests.Services;

public class SynchronizedScreensViewModelTests
{
    // =========================================================
    // 1. EVALUATION BATCH TESTS
    // =========================================================

    [Fact]
    public void EvaluationBatchIndexViewModel_InitializesWithDefaultValues()
    {
        var vm = new EvaluationBatchIndexViewModel();

        vm.Batches.Should().NotBeNull();
        vm.Groups.Should().NotBeNull();
        vm.Status.Should().Be("All");
        vm.GroupId.Should().BeNull();
        vm.PageNumber.Should().Be(1);
        vm.PageSize.Should().Be(10);
        vm.TotalBatches.Should().Be(0);
        vm.EvaluatingBatches.Should().Be(0);
        vm.CompletedBatches.Should().Be(0);
        vm.DraftBatches.Should().Be(0);
    }

    [Fact]
    public void EvaluationBatchIndexViewModel_CalculatesPaginationMetricsCorrectly()
    {
        var vm = new EvaluationBatchIndexViewModel
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 28,
            TotalPages = 3
        };

        vm.StartItemNumber.Should().Be(1);
        vm.EndItemNumber.Should().Be(10);
        vm.HasPreviousPage.Should().BeFalse();
        vm.HasNextPage.Should().BeTrue();

        vm.PageNumber = 3;
        vm.StartItemNumber.Should().Be(21);
        vm.EndItemNumber.Should().Be(28);
        vm.HasPreviousPage.Should().BeTrue();
        vm.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void EvaluationBatchIndexViewModel_BindsKpiMetricsCorrectly()
    {
        var vm = new EvaluationBatchIndexViewModel
        {
            TotalBatches = 15,
            EvaluatingBatches = 5,
            CompletedBatches = 8,
            DraftBatches = 2
        };

        vm.TotalBatches.Should().Be(15);
        vm.EvaluatingBatches.Should().Be(5);
        vm.CompletedBatches.Should().Be(8);
        vm.DraftBatches.Should().Be(2);
    }

    // =========================================================
    // 2. USER TESTS
    // =========================================================

    [Fact]
    public void UserIndexViewModel_InitializesWithDefaultValues()
    {
        var vm = new UserIndexViewModel();

        vm.Users.Should().NotBeNull();
        vm.Role.Should().Be("All");
        vm.Status.Should().Be("All");
        vm.PageNumber.Should().Be(1);
        vm.PageSize.Should().Be(10);
        vm.TotalUsers.Should().Be(0);
        vm.ActiveUsers.Should().Be(0);
        vm.LockedUsers.Should().Be(0);
        vm.InactiveUsers.Should().Be(0);
    }

    [Fact]
    public void UserIndexViewModel_CalculatesPaginationMetricsCorrectly()
    {
        var vm = new UserIndexViewModel
        {
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 25,
            TotalPages = 3
        };

        vm.StartItemNumber.Should().Be(11);
        vm.EndItemNumber.Should().Be(20);
        vm.HasPreviousPage.Should().BeTrue();
        vm.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void UserIndexViewModel_BindsKpiMetricsCorrectly()
    {
        var vm = new UserIndexViewModel
        {
            TotalUsers = 30,
            ActiveUsers = 25,
            LockedUsers = 2,
            InactiveUsers = 3
        };

        vm.TotalUsers.Should().Be(30);
        vm.ActiveUsers.Should().Be(25);
        vm.LockedUsers.Should().Be(2);
        vm.InactiveUsers.Should().Be(3);
    }

    // =========================================================
    // 3. AUDIT LOG TESTS
    // =========================================================

    [Fact]
    public void AuditLogIndexViewModel_InitializesWithDefaultValues()
    {
        var vm = new AuditLogIndexViewModel();

        vm.Logs.Should().NotBeNull();
        vm.Users.Should().NotBeNull();
        vm.ActionType.Should().Be("All");
        vm.UserId.Should().BeNull();
        vm.PageNumber.Should().Be(1);
        vm.PageSize.Should().Be(15);
        vm.TotalLogs.Should().Be(0);
        vm.TodayLogs.Should().Be(0);
        vm.ActiveUsersCount.Should().Be(0);
        vm.CriticalActionsCount.Should().Be(0);
    }

    [Fact]
    public void AuditLogIndexViewModel_CalculatesPaginationMetricsCorrectly()
    {
        var vm = new AuditLogIndexViewModel
        {
            PageNumber = 1,
            PageSize = 15,
            TotalCount = 50,
            TotalPages = 4
        };

        vm.StartItemNumber.Should().Be(1);
        vm.EndItemNumber.Should().Be(15);
        vm.HasPreviousPage.Should().BeFalse();
        vm.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void AuditLogIndexViewModel_BindsKpiMetricsCorrectly()
    {
        var vm = new AuditLogIndexViewModel
        {
            TotalLogs = 1200,
            TodayLogs = 45,
            ActiveUsersCount = 8,
            CriticalActionsCount = 12
        };

        vm.TotalLogs.Should().Be(1200);
        vm.TodayLogs.Should().Be(45);
        vm.ActiveUsersCount.Should().Be(8);
        vm.CriticalActionsCount.Should().Be(12);
    }
}
