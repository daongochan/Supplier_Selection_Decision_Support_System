using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Models;

public class CriteriaIndexViewModel : IPagedResult
{
    public string? Keyword { get; set; }

    public string Type { get; set; } = "All";

    public string Status { get; set; } = "All";

    public List<Criterion> Criteria { get; set; } = new();

    // =========================================================
    // KPI METRICS (100% GENUINE DATABASE DATA)
    // =========================================================
    public int TotalCriteria { get; set; }

    public int BenefitCriteria { get; set; }

    public int CostCriteria { get; set; }

    public int ActiveCriteria { get; set; }

    // =========================================================
    // PAGINATION
    // =========================================================
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public int StartItemNumber =>
        PaginationCalculationHelper.CalculateStartItemNumber(PageNumber, PageSize, TotalCount);

    public int EndItemNumber =>
        PaginationCalculationHelper.CalculateEndItemNumber(PageNumber, PageSize, TotalCount);
}
