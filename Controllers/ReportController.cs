using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Web.Extensions;

namespace SupplierSelectionDSS.Controllers;

[Authorize(
    Roles =
        UserRole.Administrator + "," + UserRole.Manager)]
public class ReportController : Controller
{
    private readonly ReportService _reportService;
    private readonly AuditLogService _auditLogService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        ReportService reportService,
        AuditLogService auditLogService,
        ILogger<ReportController>? logger = null)
    {
        _reportService = reportService;
        _auditLogService = auditLogService;
        _logger = logger ?? NullLogger<ReportController>.Instance;
    }


    // =========================================================
    // REPORT WORKSPACE
    // /EvaluationBatch/{batchId}/Report
    // =========================================================

    [HttpGet]
    [Route(
        "EvaluationBatch/{batchId:int}/Report")]
    public async Task<IActionResult> Index(
        int batchId)
    {
        if (batchId <= 0)
        {
            return NotFound();
        }


        var model =
            await _reportService
                .GetWorkspaceAsync(
                    batchId);


        if (model == null)
        {
            return NotFound();
        }


        return View(model);
    }


    // =========================================================
    // GENERATE PDF
    // /EvaluationBatch/{batchId}/Report/GeneratePdf
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(
        "EvaluationBatch/{batchId:int}/Report/GeneratePdf")]
    public async Task<IActionResult> GeneratePdf(
        int batchId)
    {
        return await GenerateReport(
            batchId,
            ReportTypes.Pdf);
    }


    // =========================================================
    // GENERATE EXCEL
    // /EvaluationBatch/{batchId}/Report/GenerateExcel
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(
        "EvaluationBatch/{batchId:int}/Report/GenerateExcel")]
    public async Task<IActionResult> GenerateExcel(
        int batchId)
    {
        return await GenerateReport(
            batchId,
            ReportTypes.Excel);
    }


    // =========================================================
    // DOWNLOAD
    // /Report/Download/{reportId}
    // =========================================================

    [HttpGet]
    [Route(
        "Report/Download/{reportId:int}")]
    public async Task<IActionResult> Download(
        int reportId)
    {
        if (reportId <= 0)
        {
            return NotFound();
        }


        // =====================================================
        // CURRENT USER
        // =====================================================

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        var downloadedBy = userId.Value;


        // =====================================================
        // GET REPORT FILE
        // =====================================================

        var file =
            await _reportService
                .GetDownloadAsync(
                    reportId);


        if (file == null)
        {
            return NotFound(
                "Không tìm thấy file báo cáo.");
        }


        // =====================================================
        // AUDIT DOWNLOAD
        // =====================================================

        try
        {
            var reportType =
                string.Equals(
                    file.ReportType,
                    ReportTypes.Pdf,
                    StringComparison.OrdinalIgnoreCase)
                    ? ReportTypes.Pdf
                    : ReportTypes.Excel;


            var actionType =
                reportType == ReportTypes.Pdf
                    ? AuditActions.DownloadPdf
                    : AuditActions.DownloadExcel;


            var newValue =
                JsonSerializer.Serialize(
                    new
                    {
                        ReportId =
                            reportId,

                        ReportType =
                            reportType,

                        FileName =
                            file.DownloadName,

                        FilePath =
                            file.FilePath,

                        DownloadedBy =
                            downloadedBy,

                        DownloadedAt =
                            DateTime.Now
                    });


            var description =
                reportType == ReportTypes.Pdf
                    ? $"Tải báo cáo PDF '{file.DownloadName}'."
                    : $"Tải báo cáo Excel '{file.DownloadName}'.";


            await _auditLogService
                .LogAsync(
                    downloadedBy,
                    actionType,
                    "Reports",
                    reportId,
                    null,
                    newValue,
                    description);
        }
        catch
        {
            // =================================================
            // IMPORTANT
            // =================================================
            //
            // File đã tồn tại và có thể tải xuống.
            // Audit Log không được làm hỏng thao tác Download.
            //
        }


        // =====================================================
        // RETURN FILE
        // =====================================================

        return PhysicalFile(
            file.FilePath,
            file.ContentType,
            file.DownloadName);
    }


    // =========================================================
    // GENERATE
    // =========================================================

    private async Task<IActionResult>
        GenerateReport(
            int batchId,
            string reportType)
    {
        if (batchId <= 0)
        {
            return NotFound();
        }


        // =====================================================
        // CURRENT USER
        // =====================================================

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        var generatedBy = userId.Value;


        try
        {
            GeneratedReportResult result;


            if (
                string.Equals(
                    reportType,
                    ReportTypes.Pdf,
                    StringComparison.OrdinalIgnoreCase))
            {
                result =
                    await _reportService
                        .GeneratePdfAsync(
                            batchId,
                            generatedBy);
            }
            else
            {
                result =
                    await _reportService
                        .GenerateExcelAsync(
                            batchId,
                            generatedBy);
            }


            TempData["SuccessMessage"] =
                $"{result.ReportType} report đã được tạo thành công.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }
        catch (
            BusinessRuleException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }
        catch (
            InvalidOperationException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Lỗi không xác định khi tạo báo cáo cho đợt {BatchId}.",
                batchId);

            TempData["ErrorMessage"] =
                "Không thể tạo báo cáo. Vui lòng thử lại sau.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }
    }
}
