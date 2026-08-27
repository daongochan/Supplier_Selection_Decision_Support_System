using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Models;

public class ProductGroupIndexViewModel
{
    public string? Keyword { get; set; }

    public string Status { get; set; } = "All";

    public List<ProductGroup> ProductGroups { get; set; } = new();
}