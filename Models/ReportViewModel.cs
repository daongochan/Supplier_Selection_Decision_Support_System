using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Models;

public class ReportIndexViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } =
        string.Empty;

    public string BatchName { get; set; } =
        string.Empty;

    public string BatchStatus { get; set; } =
        string.Empty;

    public bool CanGenerate { get; set; }

    public string? GenerationMessage { get; set; }

    public List<ReportHistoryItemViewModel>
        Reports
    { get; set; } = new();
}


public class ReportHistoryItemViewModel
{
    public int ReportId { get; set; }

    public int BatchId { get; set; }

    public string BatchCode { get; set; } =
        string.Empty;

    public string ReportType { get; set; } =
        string.Empty;

    public int GeneratedBy { get; set; }

    public string? GeneratedByName { get; set; }

    public DateTime GeneratedAt { get; set; }

    public string? FilePath { get; set; }
}