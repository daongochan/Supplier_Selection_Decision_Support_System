using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Infrastructure.Exports;
using System.Globalization;
using System.Text.Json;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class ReportService
{
    private readonly ApplicationDbContext _context;

    private readonly IWebHostEnvironment _environment;

    private readonly AuditLogService _auditLogService;

    private readonly IConfiguration _configuration;

    private readonly ILogger<ReportService> _logger;
    private readonly TOPSISService _topsisService;

    public ReportService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        AuditLogService auditLogService,
        IConfiguration configuration,
        ILogger<ReportService> logger,
        TOPSISService topsisService)
    {
        _context = context;
        _environment = environment;
        _auditLogService = auditLogService;
        _configuration = configuration;
        _logger = logger;
        _topsisService = topsisService;
    }



    // =========================================================
    // GET REPORT WORKSPACE
    // =========================================================

    public async Task<ReportIndexViewModel?>
        GetWorkspaceAsync(
            int batchId)
    {
        var batch =
            await _context.EvaluationBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.BatchId ==
                        batchId);


        if (batch == null)
        {
            return null;
        }


        var reports =
            await _context.Reports
                .AsNoTracking()
                .Include(
                    x =>
                        x.GeneratedByNavigation)
                .Where(
                    x =>
                        x.BatchId ==
                        batchId)
                .OrderByDescending(
                    x =>
                        x.GeneratedAt)
                .Select(
                    x =>
                        new ReportHistoryItemViewModel
                        {
                            ReportId =
                                x.ReportId,

                            BatchId =
                                x.BatchId,

                            BatchCode =
                                batch.BatchCode,

                            ReportType =
                                x.ReportType,

                            GeneratedBy =
                                x.GeneratedBy,

                            GeneratedByName =
                                x.GeneratedByNavigation!
                                    .FullName,

                            GeneratedAt =
                                x.GeneratedAt,

                            FilePath =
                                x.FilePath
                        })
                .ToListAsync();


        var canGenerate =
            string.Equals(
                batch.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase);


        return new ReportIndexViewModel
        {
            BatchId =
                batch.BatchId,

            BatchCode =
                batch.BatchCode,

            BatchName =
                batch.BatchName,

            BatchStatus =
                batch.Status,

            CanGenerate =
                canGenerate,

            GenerationMessage =
                canGenerate
                    ? null
                    : "Chỉ có thể tạo báo cáo khi Batch đã hoàn tất phê duyệt.",

            Reports =
                reports
        };
    }


    // =========================================================
    // GENERATE PDF
    // =========================================================

    public async Task<GeneratedReportResult>
        GeneratePdfAsync(
            int batchId,
            int generatedBy)
    {
        var data =
            await LoadReportDataAsync(
                batchId);


        ValidateCanGenerate(
            data.Batch);


        var reportsDirectory =
            GetReportsDirectory();


        Directory.CreateDirectory(
            reportsDirectory);


        var fileName =
            BuildFileName(
                data.Batch,
                "pdf");


        var filePath =
            Path.Combine(
                reportsDirectory,
                fileName);


        QuestPdfReportExporter.ExportToFile(
            data,
            filePath);


        var generatedAt =
            DateTime.Now;


        var report =
            new Report
            {
                BatchId =
                    batchId,

                ReportType =
                    ReportTypes.Pdf,

                GeneratedBy =
                    generatedBy,

                GeneratedAt =
                    generatedAt,

                FilePath =
                    BuildRelativePath(
                        fileName)
            };


        _context.Reports.Add(
            report);


        await _context.SaveChangesAsync();


        // =====================================================
        // AUDIT LOG
        // =====================================================

        await WriteReportAuditAsync(
            report,
            data.Batch,
            generatedBy,
            fileName,
            generatedAt);


        return new GeneratedReportResult
        {
            ReportId =
                report.ReportId,

            ReportType =
                ReportTypes.Pdf,

            FileName =
                fileName,

            AbsolutePath =
                filePath
        };
    }


    // =========================================================
    // GENERATE EXCEL
    // =========================================================

    public async Task<GeneratedReportResult>
        GenerateExcelAsync(
            int batchId,
            int generatedBy)
    {
        var data =
            await LoadReportDataAsync(
                batchId);


        ValidateCanGenerate(
            data.Batch);


        var reportsDirectory =
            GetReportsDirectory();


        Directory.CreateDirectory(
            reportsDirectory);


        var fileName =
            BuildFileName(
                data.Batch,
                "xlsx");


        var filePath =
            Path.Combine(
                reportsDirectory,
                fileName);


        ClosedXmlExcelReportExporter.ExportToFile(
            data,
            filePath);


        var generatedAt =
            DateTime.Now;


        var report =
            new Report
            {
                BatchId =
                    batchId,

                ReportType =
                    ReportTypes.Excel,

                GeneratedBy =
                    generatedBy,

                GeneratedAt =
                    generatedAt,

                FilePath =
                    BuildRelativePath(
                        fileName)
            };


        _context.Reports.Add(
            report);


        await _context.SaveChangesAsync();


        // =====================================================
        // AUDIT LOG
        // =====================================================

        await WriteReportAuditAsync(
            report,
            data.Batch,
            generatedBy,
            fileName,
            generatedAt);


        return new GeneratedReportResult
        {
            ReportId =
                report.ReportId,

            ReportType =
                ReportTypes.Excel,

            FileName =
                fileName,

            AbsolutePath =
                filePath
        };
    }


    // =========================================================
    // WRITE REPORT AUDIT
    // =========================================================

    private async Task WriteReportAuditAsync(
        Report report,
        EvaluationBatch batch,
        int generatedBy,
        string fileName,
        DateTime generatedAt)
    {
        try
        {
            var newValue =
                JsonSerializer.Serialize(
                    new
                    {
                        ReportId =
                            report.ReportId,

                        BatchId =
                            report.BatchId,

                        BatchCode =
                            batch.BatchCode,

                        BatchName =
                            batch.BatchName,

                        ReportType =
                            report.ReportType,

                        FileName =
                            fileName,

                        FilePath =
                            report.FilePath,

                        GeneratedBy =
                            generatedBy,

                        GeneratedAt =
                            generatedAt
                    });


            var actionType =
                string.Equals(
                    report.ReportType,
                    ReportTypes.Pdf,
                    StringComparison.OrdinalIgnoreCase)
                    ? "Generate PDF"
                    : "Generate Excel";


            var description =
                string.Equals(
                    report.ReportType,
                    ReportTypes.Pdf,
                    StringComparison.OrdinalIgnoreCase)
                    ? $"Tạo báo cáo PDF cho Batch {batch.BatchCode}."
                    : $"Tạo báo cáo Excel cho Batch {batch.BatchCode}.";


            await _auditLogService
                .LogAsync(
                    generatedBy,
                    actionType,
                    "Reports",
                    report.ReportId,
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
            // Báo cáo đã được tạo thành công.
            // Audit Log không được làm hỏng thao tác Generate.
            //
        }
    }


    // =========================================================
    // GET FILE
    // =========================================================

    public async Task<ReportDownloadResult?>
        GetDownloadAsync(
            int reportId)
    {
        var report =
            await _context.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.ReportId ==
                        reportId);


        if (report == null)
        {
            return null;
        }


        if (
            string.IsNullOrWhiteSpace(
                report.FilePath))
        {
            return null;
        }


        var reportsDir = GetReportsDirectory();
        var rawPath = report.FilePath.Replace('/', Path.DirectorySeparatorChar);

        string candidatePath;
        if (Path.IsPathRooted(rawPath))
        {
            candidatePath = rawPath;
        }
        else if (rawPath.StartsWith("App_Data" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            candidatePath = Path.Combine(_environment.ContentRootPath, rawPath);
        }
        else if (rawPath.StartsWith("reports" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            var fileName = Path.GetFileName(rawPath);
            var inNewStorage = Path.Combine(reportsDir, fileName);
            if (File.Exists(inNewStorage))
            {
                candidatePath = inNewStorage;
            }
            else
            {
                candidatePath = Path.Combine(_environment.WebRootPath, rawPath);
            }
        }
        else
        {
            candidatePath = Path.Combine(reportsDir, Path.GetFileName(rawPath));
        }

        var canonicalPath = Path.GetFullPath(candidatePath);

        // Security check: Đường dẫn kết quả bắt buộc phải nằm trong thư mục lưu trữ báo cáo hợp lệ
        var baseReportsDir = Path.GetFullPath(reportsDir);
        var baseLegacyDir = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "reports"));

        var isInsideAllowedDir = canonicalPath.StartsWith(baseReportsDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                                 || canonicalPath.StartsWith(baseLegacyDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        if (!isInsideAllowedDir)
        {
            _logger.LogWarning("Phát hiện nguy cơ Path Traversal đối với ReportID {ReportId}: {FilePath}", reportId, report.FilePath);
            return null;
        }

        if (!File.Exists(canonicalPath))
        {
            _logger.LogWarning("File báo cáo không tồn tại trên ổ đĩa: {CanonicalPath} (ReportID: {ReportId})", canonicalPath, reportId);
            return null;
        }

        var contentType =
            string.Equals(
                report.ReportType,
                ReportTypes.Pdf,
                StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        var downloadName =
            Path.GetFileName(
                canonicalPath);

        return new ReportDownloadResult
        {
            ReportType =
                report.ReportType,

            FilePath =
                canonicalPath,

            ContentType =
                contentType,

            DownloadName =
                downloadName
        };
    }


    // =========================================================
    // LOAD REPORT DATA
    // =========================================================

    private async Task<ReportData>
        LoadReportDataAsync(
            int batchId)
    {
        var batch =
            await _context.EvaluationBatches
                .AsNoTracking()
                .AsSplitQuery()
                .Include(
                    x =>
                        x.Group)
                .Include(
                    x =>
                        x.Suppliers)
                .Include(
                    x =>
                        x.Criteria)
                .FirstOrDefaultAsync(
                    x =>
                        x.BatchId ==
                        batchId);


        if (batch == null)
        {
            throw new InvalidOperationException(
                "Evaluation Batch không tồn tại.");
        }


        // =====================================================
        // EVALUATION SCORES
        // =====================================================

        var scores =
            await _context.EvaluationScores
                .AsNoTracking()
                .Include(
                    x =>
                        x.Supplier)
                .Include(
                    x =>
                        x.Criteria)
                .Where(
                    x =>
                        x.BatchId ==
                        batchId)
                .OrderBy(
                    x =>
                        x.Supplier.SupplierCode)
                .ThenBy(
                    x =>
                        x.Criteria.CriteriaCode)
                .ToListAsync();


        // =====================================================
        // AHP RESULT
        // =====================================================

        var ahpResult =
            await _context.Ahpresults
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.BatchId ==
                        batchId);


        var ahpWeights =
            await (
                from weight
                    in _context.AhpcriteriaWeights
                join criterion
                    in _context.Criteria
                        on weight.CriteriaId
                            equals criterion.CriteriaId
                where
                    weight.BatchId ==
                    batchId
                orderby
                    criterion.CriteriaCode
                select new ReportAHPWeight
                {
                    CriteriaCode =
                        criterion.CriteriaCode,

                    CriteriaName =
                        criterion.CriteriaName,

                    WeightValue =
                        weight.WeightValue
                })
                .AsNoTracking()
                .ToListAsync();


        // =====================================================
        // AHP PAIRWISE COMPARISONS
        // =====================================================

        var ahpComparisons =
            await _context.AhppairwiseComparisons
                .AsNoTracking()
                .Include(
                    x =>
                        x.CriteriaRow)
                .Include(
                    x =>
                        x.CriteriaCol)
                .Where(
                    x =>
                        x.BatchId ==
                        batchId)
                .OrderBy(
                    x =>
                        x.CriteriaRow.CriteriaCode)
                .ThenBy(
                    x =>
                        x.CriteriaCol.CriteriaCode)
                .ToListAsync();


        // =====================================================
        // TOPSIS IDEAL
        // =====================================================

        var ideals =
            await _context.TopsisIdealSolutions
                .AsNoTracking()
                .Include(
                    x =>
                        x.Criteria)
                .Where(
                    x =>
                        x.BatchId ==
                        batchId)
                .OrderBy(
                    x =>
                        x.Criteria.CriteriaCode)
                .ToListAsync();


        // =====================================================
        // TOPSIS RESULTS
        // =====================================================

        var topsis =
            await _context.TopsisResults
                .AsNoTracking()
                .Include(
                    x =>
                        x.Supplier)
                .Where(
                    x =>
                        x.BatchId ==
                        batchId)
                .OrderBy(
                    x =>
                        x.RankNo)
                .ToListAsync();


        // =====================================================
        // FINAL APPROVAL
        // =====================================================

        var approval =
            await _context.Approvals
                .AsNoTracking()
                .Include(
                    x =>
                        x.SelectedSupplier)
                .Include(
                    x =>
                        x.ApprovedByNavigation)
                .Where(
                    x =>
                        x.BatchId ==
                        batchId &&
                        x.IsFinal &&
                        x.Status ==
                            "Approved")
                .OrderByDescending(
                    x =>
                        x.ApprovalDate)
                .ThenByDescending(
                    x =>
                        x.ApprovalId)
                .FirstOrDefaultAsync();


        var reportData = new ReportData
        {
            Batch = batch,
            Scores = scores,
            AhpResult = ahpResult,
            AhpWeights = ahpWeights,
            AhpComparisons = ahpComparisons,
            Ideals = ideals,
            TopsisResults = topsis,
            Approval = approval
        };

        var topsisWs = await _topsisService.GetWorkspaceAsync(batchId);
        if (topsisWs != null)
        {
            reportData.NormalizedMatrix = topsisWs.NormalizedMatrix;
            reportData.WeightedMatrix = topsisWs.WeightedMatrix;
            reportData.SensitivityAnalysis = topsisWs.SensitivityAnalysis;
        }

        return reportData;
    }



    // =========================================================
    // VALIDATION
    // =========================================================

    private static void ValidateCanGenerate(
        EvaluationBatch batch)
    {
        if (
            !string.Equals(
                batch.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Chỉ có thể tạo báo cáo khi Batch đã ở trạng thái Completed.");
        }
    }


    // =========================================================
    // FILE PATH
    // =========================================================

    private string GetReportsDirectory()
    {
        var configuredPath = _configuration["Reports:Directory"];
        string dirPath;

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            dirPath = Path.Combine(_environment.ContentRootPath, "App_Data", "reports");
        }
        else if (!Path.IsPathRooted(configuredPath))
        {
            dirPath = Path.Combine(_environment.ContentRootPath, configuredPath);
        }
        else
        {
            dirPath = configuredPath;
        }

        Directory.CreateDirectory(dirPath);
        return Path.GetFullPath(dirPath);
    }


    private static string BuildFileName(
        EvaluationBatch batch,
        string extension)
    {
        var safeCode =
            string.Join(
                "_",
                batch.BatchCode
                    .Split(
                        Path.GetInvalidFileNameChars(),
                        StringSplitOptions.RemoveEmptyEntries));


        return
            $"{safeCode}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
    }


    private static string BuildRelativePath(
        string fileName)
    {
        return
            Path.Combine(
                "App_Data",
                "reports",
                fileName)
                .Replace(
                    '\\',
                    '/');
    }


}
