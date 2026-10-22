namespace SupplierSelectionDSS.Models;

public class TOPSISIdealViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public string CriteriaType { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public decimal PISValue { get; set; }

    public decimal NISValue { get; set; }
}
