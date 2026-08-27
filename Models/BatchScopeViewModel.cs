namespace SupplierSelectionDSS.Models;

public class BatchSupplierOptionViewModel
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;
}


public class BatchCriteriaOptionViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public string CriteriaType { get; set; } = string.Empty;

    public string? Unit { get; set; }
}