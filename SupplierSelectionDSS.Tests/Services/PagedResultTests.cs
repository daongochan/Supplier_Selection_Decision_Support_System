using FluentAssertions;
using SupplierSelectionDSS.Models;
using Xunit;

namespace SupplierSelectionDSS.Tests.Services;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_EmptyList_CalculatesCorrectMetrics()
    {
        var result = new PagedResult<string>
        {
            Items = new List<string>(),
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 0,
            TotalPages = 0
        };

        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
        result.StartItemNumber.Should().Be(0);
        result.EndItemNumber.Should().Be(0);
    }

    [Fact]
    public void PagedResult_FirstPage_CalculatesCorrectMetrics()
    {
        var items = Enumerable.Range(1, 10).Select(i => $"Item {i}").ToList();
        var result = new PagedResult<string>
        {
            Items = items,
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 25,
            TotalPages = 3
        };

        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
        result.StartItemNumber.Should().Be(1);
        result.EndItemNumber.Should().Be(10);
    }

    [Fact]
    public void PagedResult_MiddlePage_CalculatesCorrectMetrics()
    {
        var items = Enumerable.Range(11, 10).Select(i => $"Item {i}").ToList();
        var result = new PagedResult<string>
        {
            Items = items,
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 25,
            TotalPages = 3
        };

        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.StartItemNumber.Should().Be(11);
        result.EndItemNumber.Should().Be(20);
    }

    [Fact]
    public void PagedResult_LastPage_CalculatesCorrectMetrics()
    {
        var items = Enumerable.Range(21, 5).Select(i => $"Item {i}").ToList();
        var result = new PagedResult<string>
        {
            Items = items,
            PageNumber = 3,
            PageSize = 10,
            TotalCount = 25,
            TotalPages = 3
        };

        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
        result.StartItemNumber.Should().Be(21);
        result.EndItemNumber.Should().Be(25);
    }

    [Theory]
    [InlineData(25, 10, 3)]
    [InlineData(30, 10, 3)]
    [InlineData(31, 10, 4)]
    [InlineData(0, 10, 0)]
    public void TotalPages_CalculationFormula_IsCorrect(int totalCount, int pageSize, int expectedTotalPages)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        totalPages.Should().Be(expectedTotalPages);
    }
}
