using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Models;

public class EvaluationViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BatchName { get; set; } = string.Empty;

    public string GroupCode { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string RowVersion { get; set; } = string.Empty;

    public List<EvaluationCriteriaColumnViewModel> Criteria { get; set; }
        = new();

    public List<EvaluationSupplierRowViewModel> Suppliers { get; set; }
        = new();
}
