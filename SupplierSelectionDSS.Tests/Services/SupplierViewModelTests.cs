using FluentAssertions;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using Xunit;

namespace SupplierSelectionDSS.Tests.Services;

public class SupplierViewModelTests
{
    [Fact]
    public void SupplierIndexViewModel_InitializesWithDefaultValues()
    {
        var vm = new SupplierIndexViewModel();

        vm.Suppliers.Should().NotBeNull();
        vm.Groups.Should().NotBeNull();
        vm.Status.Should().Be("All");
        vm.PageNumber.Should().Be(1);
        vm.PageSize.Should().Be(10);
        vm.TotalSuppliers.Should().Be(0);
        vm.ActiveSuppliers.Should().Be(0);
        vm.InactiveSuppliers.Should().Be(0);
        vm.TotalGroups.Should().Be(0);
    }

    [Fact]
    public void SupplierIndexViewModel_CalculatesPaginationMetricsCorrectly()
    {
        var vm = new SupplierIndexViewModel
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
    }

    [Fact]
    public void SupplierIndexViewModel_BindsKpiMetricsCorrectly()
    {
        var vm = new SupplierIndexViewModel
        {
            TotalSuppliers = 15,
            ActiveSuppliers = 12,
            InactiveSuppliers = 3,
            TotalGroups = 5
        };

        var activeRatio = vm.TotalSuppliers > 0
            ? (vm.ActiveSuppliers * 100.0 / vm.TotalSuppliers)
            : 0;

        var inactiveRatio = vm.TotalSuppliers > 0
            ? (vm.InactiveSuppliers * 100.0 / vm.TotalSuppliers)
            : 0;

        activeRatio.Should().Be(80.0);
        inactiveRatio.Should().Be(20.0);
        vm.TotalGroups.Should().Be(5);
    }

    [Fact]
    public void Supplier_WithAssociatedGroups_MaintainsCollectionIntegrity()
    {
        var supplier = new Supplier
        {
            SupplierId = 1,
            SupplierCode = "NCC-001",
            SupplierName = "Công ty TNHH Thép Việt",
            Status = "Active",
            Groups = new List<ProductGroup>
            {
                new() { GroupId = 1, GroupCode = "VLXD", GroupName = "Vật liệu xây dựng", Status = "Active" },
                new() { GroupId = 2, GroupCode = "CK", GroupName = "Cơ khí", Status = "Active" }
            }
        };

        supplier.Groups.Should().HaveCount(2);
        supplier.Groups.Select(g => g.GroupName).Should().Contain("Vật liệu xây dựng");
    }
}
