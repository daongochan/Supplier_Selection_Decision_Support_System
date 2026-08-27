using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Models;

public class CriteriaIndexViewModel
{
    public string? Keyword { get; set; }

    public string Type { get; set; } = "All";

    public string Status { get; set; } = "All";

    public List<Criterion> Criteria { get; set; } = new();
}