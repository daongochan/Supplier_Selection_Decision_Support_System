namespace SupplierSelectionDSS.Models;

public class EvaluationBatchIndexViewModel : IPagedResult
{
    public string? Keyword { get; set; }

    public string Status { get; set; } = "All";

    public List<EvaluationBatchListItemViewModel> Batches { get; set; }
        = new();

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