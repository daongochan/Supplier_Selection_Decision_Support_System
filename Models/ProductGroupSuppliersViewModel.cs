using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Models;

public class ProductGroupSuppliersViewModel
{
    public int GroupId { get; set; }

    public string GroupCode { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public List<SupplierSelectionItemViewModel> Suppliers { get; set; }
        = new();
}

public class SupplierSelectionItemViewModel
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public bool Selected { get; set; }
}
