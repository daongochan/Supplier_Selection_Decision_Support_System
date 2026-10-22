using FluentAssertions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using Xunit;

namespace SupplierSelectionDSS.Tests.Services;

public class ProductGroupAndCriteriaViewModelTests
{
    // =========================================================
    // PRODUCT GROUP TESTS
    // =========================================================

    [Fact]
    public void ProductGroupIndexViewModel_InitializesWithDefaultValues()
    {
        var vm = new ProductGroupIndexViewModel();

        vm.ProductGroups.Should().NotBeNull();
        vm.Status.Should().Be("All");
        vm.PageNumber.Should().Be(1);
        vm.PageSize.Should().Be(10);
        vm.TotalGroups.Should().Be(0);
        vm.ActiveGroups.Should().Be(0);
        vm.InactiveGroups.Should().Be(0);
        vm.TotalLinkedSuppliers.Should().Be(0);
    }

    [Fact]
    public void ProductGroupIndexViewModel_CalculatesPaginationMetricsCorrectly()
    {
        var vm = new ProductGroupIndexViewModel
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 25,
            TotalPages = 3
        };

        vm.StartItemNumber.Should().Be(1);
        vm.EndItemNumber.Should().Be(10);
        vm.HasPreviousPage.Should().BeFalse();
        vm.HasNextPage.Should().BeTrue();

        // Test last page boundary
        vm.PageNumber = 3;
        vm.StartItemNumber.Should().Be(21);
        vm.EndItemNumber.Should().Be(25);
        vm.HasPreviousPage.Should().BeTrue();
        vm.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ProductGroupIndexViewModel_BindsKpiMetricsCorrectly()
    {
        var vm = new ProductGroupIndexViewModel
        {
            TotalGroups = 20,
            ActiveGroups = 16,
            InactiveGroups = 4,
            TotalLinkedSuppliers = 12
        };

        var activePercent = vm.TotalGroups > 0
            ? (vm.ActiveGroups * 100.0 / vm.TotalGroups)
            : 0;

        var inactivePercent = vm.TotalGroups > 0
            ? (vm.InactiveGroups * 100.0 / vm.TotalGroups)
            : 0;

        activePercent.Should().Be(80.0);
        inactivePercent.Should().Be(20.0);
        vm.TotalLinkedSuppliers.Should().Be(12);
    }

    [Fact]
    public void ProductGroup_WithAssociatedSuppliers_MaintainsCollectionIntegrity()
    {
        var group = new ProductGroup
        {
            GroupId = 1,
            GroupCode = "IT-HARDWARE",
            GroupName = "Thiết bị Công nghệ thông tin",
            Status = EntityStatus.Active,
            Suppliers = new List<Supplier>
            {
                new() { SupplierId = 10, SupplierCode = "SUP-01", SupplierName = "FPT Information System", Status = EntityStatus.Active },
                new() { SupplierId = 11, SupplierCode = "SUP-02", SupplierName = "CMC Telecom", Status = EntityStatus.Active }
            }
        };

        group.Suppliers.Should().HaveCount(2);
        group.Suppliers.Select(s => s.SupplierCode).Should().Contain("SUP-01");
    }

    // =========================================================
    // CRITERIA TESTS
    // =========================================================

    [Fact]
    public void CriteriaIndexViewModel_InitializesWithDefaultValues()
    {
        var vm = new CriteriaIndexViewModel();

        vm.Criteria.Should().NotBeNull();
        vm.Type.Should().Be("All");
        vm.Status.Should().Be("All");
        vm.PageNumber.Should().Be(1);
        vm.PageSize.Should().Be(10);
        vm.TotalCriteria.Should().Be(0);
        vm.BenefitCriteria.Should().Be(0);
        vm.CostCriteria.Should().Be(0);
        vm.ActiveCriteria.Should().Be(0);
    }

    [Fact]
    public void CriteriaIndexViewModel_CalculatesPaginationMetricsCorrectly()
    {
        var vm = new CriteriaIndexViewModel
        {
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 35,
            TotalPages = 4
        };

        vm.StartItemNumber.Should().Be(11);
        vm.EndItemNumber.Should().Be(20);
        vm.HasPreviousPage.Should().BeTrue();
        vm.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void CriteriaIndexViewModel_BindsKpiMetricsCorrectly()
    {
        var vm = new CriteriaIndexViewModel
        {
            TotalCriteria = 10,
            BenefitCriteria = 7,
            CostCriteria = 3,
            ActiveCriteria = 9
        };

        var benefitPercent = vm.TotalCriteria > 0
            ? (vm.BenefitCriteria * 100.0 / vm.TotalCriteria)
            : 0;

        var costPercent = vm.TotalCriteria > 0
            ? (vm.CostCriteria * 100.0 / vm.TotalCriteria)
            : 0;

        var activePercent = vm.TotalCriteria > 0
            ? (vm.ActiveCriteria * 100.0 / vm.TotalCriteria)
            : 0;

        benefitPercent.Should().Be(70.0);
        costPercent.Should().Be(30.0);
        activePercent.Should().Be(90.0);
    }

    [Theory]
    [InlineData(CriteriaType.Benefit, "Chất lượng sản phẩm")]
    [InlineData(CriteriaType.Cost, "Giá thành cung cấp")]
    public void Criterion_MaintainsTypeAndName(string criteriaType, string name)
    {
        var criterion = new Criterion
        {
            CriteriaId = 1,
            CriteriaCode = "C01",
            CriteriaName = name,
            CriteriaType = criteriaType,
            Status = EntityStatus.Active
        };

        criterion.CriteriaType.Should().Be(criteriaType);
        criterion.CriteriaName.Should().Be(name);
    }
}
