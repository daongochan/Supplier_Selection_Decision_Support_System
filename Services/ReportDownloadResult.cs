namespace SupplierSelectionDSS.Services;

public class ReportDownloadResult
{
    public string ReportType { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string DownloadName { get; set; } = string.Empty;
}
