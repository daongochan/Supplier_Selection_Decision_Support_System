namespace SupplierSelectionDSS.Models;

public class EvaluationBatchIndexViewModel
{
    public string? Keyword { get; set; }

    public string Status { get; set; } = "All";

    public List<EvaluationBatchListItemViewModel> Batches { get; set; }
        = new();
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