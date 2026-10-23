using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Models;

public class EvaluationBatchIndexViewModel : IPagedResult
{
    public string? Keyword { get; set; }

    public string Status { get; set; } = "All";

    public int? GroupId { get; set; }

    public List<ProductGroup> Groups { get; set; } = new();

    public int TotalBatches { get; set; }

    public int EvaluatingBatches { get; set; }

    public int CompletedBatches { get; set; }

    public int DraftBatches { get; set; }

    public List<EvaluationBatchListItemViewModel> Batches { get; set; }
        = new();

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


public class EvaluationBatchListItemViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BatchName { get; set; } = string.Empty;

    public string GroupCode { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int SupplierCount { get; set; }

    public int CriteriaCount { get; set; }
}
