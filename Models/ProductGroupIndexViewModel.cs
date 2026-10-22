using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Models;

public class ProductGroupIndexViewModel : IPagedResult
{
    public string? Keyword { get; set; }

    public string Status { get; set; } = "All";

    public List<ProductGroup> ProductGroups { get; set; } = new();

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public int StartItemNumber => TotalCount == 0
        ? 0
        : ((PageNumber - 1) * PageSize) + 1;

    public int EndItemNumber => Math.Min(PageNumber * PageSize, TotalCount);
}