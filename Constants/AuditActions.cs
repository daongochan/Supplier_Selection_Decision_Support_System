namespace SupplierSelectionDSS.Constants;

public static class AuditActions
{
    public const string CreateUser = "Create User";
    public const string UpdateUser = "Update User";
    public const string CreateBatch = "Create Batch";
    public const string StartBatch = "Start Batch";
    public const string CancelBatch = "Cancel Batch";
    public const string SaveEvaluation = "Save Evaluation";
    public const string SaveAHP = "Save AHP";
    public const string CalculateAHP = "Calculate AHP";
    public const string CalculateTOPSIS = "Calculate TOPSIS";
    public const string Approve = "Approve";
    public const string Reject = "Reject";
    public const string Reevaluate = "Re-evaluate";
    public const string GeneratePdf = "Generate PDF";
    public const string GenerateExcel = "Generate Excel";
    public const string DownloadPdf = "Download PDF";
    public const string DownloadExcel = "Download Excel";
    public const string LoginSuccess = "Login Success";
    public const string LoginFailed = "Login Failed";

    public static readonly string[] All =
    {
        CreateUser,
        UpdateUser,
        CreateBatch,
        StartBatch,
        CancelBatch,
        SaveEvaluation,
        SaveAHP,
        CalculateAHP,
        CalculateTOPSIS,
        Approve,
        Reject,
        Reevaluate,
        GeneratePdf,
        GenerateExcel,
        DownloadPdf,
        DownloadExcel,
        LoginSuccess,
        LoginFailed
    };
}
