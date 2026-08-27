using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using System.Globalization;
using System.Text.Json;

namespace SupplierSelectionDSS.Services;

public class ReportService
{
    private readonly ApplicationDbContext _context;

    private readonly IWebHostEnvironment _environment;

    private readonly AuditLogService _auditLogService;


    public ReportService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        AuditLogService auditLogService)
    {
        _context =
            context;

        _environment =
            environment;

        _auditLogService =
            auditLogService;
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


        var document =
            BuildPdfDocument(
                data);


        document.GeneratePdf(
            filePath);


        var generatedAt =
            DateTime.Now;


        var report =
            new Report
            {
                BatchId =
                    batchId,

                ReportType =
                    "PDF",

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
                "PDF",

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


        using (
            var workbook =
                new XLWorkbook())
        {
            BuildExcelWorkbook(
                workbook,
                data);


            workbook.SaveAs(
                filePath);
        }


        var generatedAt =
            DateTime.Now;


        var report =
            new Report
            {
                BatchId =
                    batchId,

                ReportType =
                    "Excel",

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
                "Excel",

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
                    "PDF",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Generate PDF"
                    : "Generate Excel";


            var description =
                string.Equals(
                    report.ReportType,
                    "PDF",
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


        var relativePath =
            report.FilePath
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar);


        var filePath =
            Path.Combine(
                _environment.WebRootPath,
                relativePath);


        if (!File.Exists(
                filePath))
        {
            return null;
        }


        var contentType =
            string.Equals(
                report.ReportType,
                "PDF",
                StringComparison.OrdinalIgnoreCase)
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";


        var downloadName =
            Path.GetFileName(
                filePath);


        return new ReportDownloadResult
        {
            ReportType =
                report.ReportType,

            FilePath =
                filePath,

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


        return new ReportData
        {
            Batch =
                batch,

            Scores =
                scores,

            AhpResult =
                ahpResult,

            AhpWeights =
                ahpWeights,

            AhpComparisons =
                ahpComparisons,

            Ideals =
                ideals,

            TopsisResults =
                topsis,

            Approval =
                approval
        };
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
        return Path.Combine(
            _environment.WebRootPath,
            "reports");
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
                "reports",
                fileName)
                .Replace(
                    '\\',
                    '/');
    }


    // =========================================================
    // BUILD EXCEL
    // =========================================================

    private static void BuildExcelWorkbook(
        XLWorkbook workbook,
        ReportData data)
    {
        BuildExcelSummarySheet(
            workbook,
            data);

        BuildExcelEvaluationSheet(
            workbook,
            data);

        BuildExcelAHPResultsSheet(
            workbook,
            data);

        BuildExcelTOPSISSheet(
            workbook,
            data);
    }


    private static void BuildExcelSummarySheet(
        XLWorkbook workbook,
        ReportData data)
    {
        var ws =
            workbook.Worksheets.Add(
                "Summary");


        ws.Cell(1, 1)
            .Value =
            "SUPPLIER SELECTION DSS";


        ws.Cell(2, 1)
            .Value =
            "Final Evaluation Report";


        ws.Cell(4, 1)
            .Value =
            "Batch Code";

        ws.Cell(4, 2)
            .Value =
            data.Batch.BatchCode;


        ws.Cell(5, 1)
            .Value =
            "Batch Name";

        ws.Cell(5, 2)
            .Value =
            data.Batch.BatchName;


        ws.Cell(6, 1)
            .Value =
            "Product Group";

        ws.Cell(6, 2)
            .Value =
            data.Batch.Group != null
                ? $"{data.Batch.Group.GroupCode} - {data.Batch.Group.GroupName}"
                : "—";


        ws.Cell(7, 1)
            .Value =
            "Status";

        ws.Cell(7, 2)
            .Value =
            data.Batch.Status;


        ws.Cell(8, 1)
            .Value =
            "Suppliers";

        ws.Cell(8, 2)
            .Value =
            data.Batch.Suppliers.Count;


        ws.Cell(9, 1)
            .Value =
            "Criteria";

        ws.Cell(9, 2)
            .Value =
            data.Batch.Criteria.Count;


        ws.Cell(11, 1)
            .Value =
            "Final Supplier";


        ws.Cell(11, 2)
            .Value =
            data.Approval?.SelectedSupplier != null
                ? data.Approval.SelectedSupplier.SupplierCode
                : "—";


        ws.Cell(12, 1)
            .Value =
            "Final Supplier Name";


        ws.Cell(12, 2)
            .Value =
            data.Approval?.SelectedSupplier != null
                ? data.Approval.SelectedSupplier.SupplierName
                : "—";


        ws.Cell(13, 1)
            .Value =
            "Approval Status";


        ws.Cell(13, 2)
            .Value =
            data.Approval?.Status
            ?? "—";


        ws.Cell(14, 1)
            .Value =
            "Approved By";


        ws.Cell(14, 2)
            .Value =
            data.Approval?.ApprovedByNavigation?.FullName
            ?? "—";


        ws.Cell(15, 1)
            .Value =
            "Approval Date";


        ws.Cell(15, 2)
            .Value =
            data.Approval?.ApprovalDate
                .ToString(
                    "dd/MM/yyyy HH:mm",
                    CultureInfo.InvariantCulture)
            ?? "—";


        ws.Column(1)
            .Width =
            25;


        ws.Column(2)
            .Width =
            48;


        ws.Range(
            "A1:B2")
            .Style.Font
            .Bold =
            true;


        ws.Range(
            "A4:A15")
            .Style.Font
            .Bold =
            true;


        ws.Columns()
            .AdjustToContents();
    }


    private static void BuildExcelEvaluationSheet(
        XLWorkbook workbook,
        ReportData data)
    {
        var ws =
            workbook.Worksheets.Add(
                "Evaluation");


        ws.Cell(1, 1)
            .Value =
            "Supplier";


        var criteria =
            data.Batch.Criteria
                .OrderBy(
                    x =>
                        x.CriteriaCode)
                .ToList();


        for (
            var j = 0;
            j < criteria.Count;
            j++)
        {
            ws.Cell(
                    1,
                    j + 2)
                .Value =
                criteria[j].CriteriaCode;
        }


        var suppliers =
            data.Batch.Suppliers
                .OrderBy(
                    x =>
                        x.SupplierCode)
                .ToList();


        var scoreMap =
            data.Scores
                .ToDictionary(
                    x =>
                        (
                            x.SupplierId,
                            x.CriteriaId
                        ),
                    x =>
                        x.ScoreValue);


        for (
            var i = 0;
            i < suppliers.Count;
            i++)
        {
            var row =
                i + 2;


            ws.Cell(
                    row,
                    1)
                .Value =
                $"{suppliers[i].SupplierCode} - {suppliers[i].SupplierName}";


            for (
                var j = 0;
                j < criteria.Count;
                j++)
            {
                if (
                    scoreMap.TryGetValue(
                        (
                            suppliers[i].SupplierId,
                            criteria[j].CriteriaId
                        ),
                        out var score))
                {
                    ws.Cell(
                            row,
                            j + 2)
                        .Value =
                        (double)score;
                }
            }
        }


        ws.Row(1)
            .Style.Font
            .Bold =
            true;


        ws.SheetView
            .FreezeRows(
                1);


        ws.Columns()
            .AdjustToContents();
    }


    private static void BuildExcelAHPResultsSheet(
        XLWorkbook workbook,
        ReportData data)
    {
        var ws =
            workbook.Worksheets.Add(
                "AHP");


        ws.Cell(1, 1)
            .Value =
            "AHP CONSISTENCY";


        ws.Cell(3, 1)
            .Value =
            "Lambda Max";


        ws.Cell(3, 2)
            .Value =
            data.AhpResult?.LambdaMax;


        ws.Cell(4, 1)
            .Value =
            "CI";


        ws.Cell(4, 2)
            .Value =
            data.AhpResult?.Civalue;


        ws.Cell(5, 1)
            .Value =
            "RI";


        ws.Cell(5, 2)
            .Value =
            data.AhpResult?.Rivalue;


        ws.Cell(6, 1)
            .Value =
            "CR";


        ws.Cell(6, 2)
            .Value =
            data.AhpResult?.Crvalue;


        ws.Cell(7, 1)
            .Value =
            "Consistent";


        ws.Cell(7, 2)
            .Value =
            data.AhpResult?.IsConsistent == true
                ? "Yes"
                : "No";


        ws.Cell(9, 1)
            .Value =
            "Criteria";


        ws.Cell(9, 2)
            .Value =
            "Weight";


        var row =
            10;


        foreach (
            var weight
            in data.AhpWeights)
        {
            ws.Cell(
                    row,
                    1)
                .Value =
                $"{weight.CriteriaCode} - {weight.CriteriaName}";


            ws.Cell(
                    row,
                    2)
                .Value =
                (double)weight.WeightValue;


            row++;
        }


        ws.Row(1)
            .Style.Font
            .Bold =
            true;


        ws.Row(9)
            .Style.Font
            .Bold =
            true;


        ws.Columns()
            .AdjustToContents();
    }


    private static void BuildExcelTOPSISSheet(
        XLWorkbook workbook,
        ReportData data)
    {
        var ws =
            workbook.Worksheets.Add(
                "TOPSIS");


        ws.Cell(1, 1)
            .Value =
            "Criteria";


        ws.Cell(1, 2)
            .Value =
            "Type";


        ws.Cell(1, 3)
            .Value =
            "PIS";


        ws.Cell(1, 4)
            .Value =
            "NIS";


        var row =
            2;


        foreach (
            var ideal
            in data.Ideals)
        {
            ws.Cell(
                    row,
                    1)
                .Value =
                ideal.Criteria.CriteriaCode;


            ws.Cell(
                    row,
                    2)
                .Value =
                ideal.Criteria.CriteriaType;


            ws.Cell(
                    row,
                    3)
                .Value =
                (double)ideal.Pisvalue;


            ws.Cell(
                    row,
                    4)
                .Value =
                (double)ideal.Nisvalue;


            row++;
        }


        row += 2;


        ws.Cell(
                row,
                1)
            .Value =
            "Rank";


        ws.Cell(
                row,
                2)
            .Value =
            "Supplier";


        ws.Cell(
                row,
                3)
            .Value =
            "Distance PIS";


        ws.Cell(
                row,
                4)
            .Value =
            "Distance NIS";


        ws.Cell(
                row,
                5)
            .Value =
            "Closeness";


        row++;


        foreach (
            var result
            in data.TopsisResults)
        {
            ws.Cell(
                    row,
                    1)
                .Value =
                result.RankNo;


            ws.Cell(
                    row,
                    2)
                .Value =
                $"{result.Supplier.SupplierCode} - {result.Supplier.SupplierName}";


            ws.Cell(
                    row,
                    3)
                .Value =
                (double)result.DistanceToPis;


            ws.Cell(
                    row,
                    4)
                .Value =
                (double)result.DistanceToNis;


            ws.Cell(
                    row,
                    5)
                .Value =
                (double)result.ClosenessCoefficient;


            row++;
        }


        ws.Row(1)
            .Style.Font
            .Bold =
            true;


        ws.Columns()
            .AdjustToContents();
    }


    // =========================================================
    // BUILD PDF
    // =========================================================

    private static QuestPDF.Fluent.Document
        BuildPdfDocument(
            ReportData data)
    {
        return QuestPDF.Fluent.Document.Create(
            container =>
            {
                container.Page(
                    page =>
                    {
                        page.Size(
                            PageSizes.A4);


                        page.Margin(
                            32);


                        page.DefaultTextStyle(
                            style =>
                                style.FontSize(
                                    9));


                        page.Header()
                            .Text(
                                "SUPPLIER SELECTION DSS")
                            .SemiBold()
                            .FontSize(
                                16);


                        page.Content()
                            .Column(
                                column =>
                                {
                                    column.Spacing(
                                        10);


                                    column.Item()
                                        .Text(
                                            "Final Evaluation Report")
                                        .Bold()
                                        .FontSize(
                                            18);


                                    column.Item()
                                        .Text(
                                            $"Batch: {data.Batch.BatchCode} · {data.Batch.BatchName}");


                                    column.Item()
                                        .Text(
                                            $"Product Group: {data.Batch.Group?.GroupCode} · {data.Batch.Group?.GroupName}");


                                    column.Item()
                                        .Text(
                                            $"Status: {data.Batch.Status}");


                                    column.Item()
                                        .Text(
                                            $"Suppliers: {data.Batch.Suppliers.Count} · Criteria: {data.Batch.Criteria.Count}");


                                    column.Item()
                                        .PaddingTop(
                                            8)
                                        .Text(
                                            "AHP")
                                        .Bold()
                                        .FontSize(
                                            13);


                                    if (
                                        data.AhpResult != null)
                                    {
                                        column.Item()
                                            .Text(
                                                $"Lambda Max: {data.AhpResult.LambdaMax:0.000000}   " +
                                                $"CI: {data.AhpResult.Civalue:0.000000}   " +
                                                $"RI: {data.AhpResult.Rivalue:0.000000}   " +
                                                $"CR: {data.AhpResult.Crvalue:0.000000}");


                                        column.Item()
                                            .Text(
                                                $"Consistency: {(data.AhpResult.IsConsistent ? "Consistent" : "Inconsistent")}");
                                    }


                                    column.Item()
                                        .PaddingTop(
                                            8)
                                        .Text(
                                            "Criteria Weights")
                                        .Bold()
                                        .FontSize(
                                            13);


                                    column.Item()
                                        .Table(
                                            table =>
                                            {
                                                table.ColumnsDefinition(
                                                    columns =>
                                                    {
                                                        columns.RelativeColumn(
                                                            3);


                                                        columns.RelativeColumn(
                                                            1);
                                                    });


                                                table.Header(
                                                    header =>
                                                    {
                                                        header.Cell()
                                                            .Text(
                                                                "Criteria")
                                                            .Bold();


                                                        header.Cell()
                                                            .AlignRight()
                                                            .Text(
                                                                "Weight")
                                                            .Bold();
                                                    });


                                                foreach (
                                                    var weight
                                                    in data.AhpWeights)
                                                {
                                                    table.Cell()
                                                        .Text(
                                                            $"{weight.CriteriaCode} · {weight.CriteriaName}");


                                                    table.Cell()
                                                        .AlignRight()
                                                        .Text(
                                                            $"{weight.WeightValue:0.000000}");
                                                }
                                            });


                                    column.Item()
                                        .PaddingTop(
                                            8)
                                        .Text(
                                            "TOPSIS Ranking")
                                        .Bold()
                                        .FontSize(
                                            13);


                                    column.Item()
                                        .Table(
                                            table =>
                                            {
                                                table.ColumnsDefinition(
                                                    columns =>
                                                    {
                                                        columns.ConstantColumn(
                                                            35);


                                                        columns.RelativeColumn(
                                                            3);


                                                        columns.RelativeColumn(
                                                            1.2f);


                                                        columns.RelativeColumn(
                                                            1.2f);


                                                        columns.RelativeColumn(
                                                            1.2f);
                                                    });


                                                table.Header(
                                                    header =>
                                                    {
                                                        header.Cell()
                                                            .Text(
                                                                "#")
                                                            .Bold();


                                                        header.Cell()
                                                            .Text(
                                                                "Supplier")
                                                            .Bold();


                                                        header.Cell()
                                                            .Text(
                                                                "D+")
                                                            .Bold();


                                                        header.Cell()
                                                            .Text(
                                                                "D-")
                                                            .Bold();


                                                        header.Cell()
                                                            .Text(
                                                                "CC")
                                                            .Bold();
                                                    });


                                                foreach (
                                                    var result
                                                    in data.TopsisResults)
                                                {
                                                    table.Cell()
                                                        .Text(
                                                            result.RankNo
                                                                .ToString());


                                                    table.Cell()
                                                        .Text(
                                                            $"{result.Supplier.SupplierCode} · {result.Supplier.SupplierName}");


                                                    table.Cell()
                                                        .Text(
                                                            result.DistanceToPis
                                                                .ToString(
                                                                    "0.000000",
                                                                    CultureInfo.InvariantCulture));


                                                    table.Cell()
                                                        .Text(
                                                            result.DistanceToNis
                                                                .ToString(
                                                                    "0.000000",
                                                                    CultureInfo.InvariantCulture));


                                                    table.Cell()
                                                        .Text(
                                                            result.ClosenessCoefficient
                                                                .ToString(
                                                                    "0.000000",
                                                                    CultureInfo.InvariantCulture));
                                                }
                                            });


                                    column.Item()
                                        .PaddingTop(
                                            8)
                                        .Text(
                                            "Final Decision")
                                        .Bold()
                                        .FontSize(
                                            13);


                                    if (
                                        data.Approval != null)
                                    {
                                        column.Item()
                                            .Text(
                                                $"Supplier: {data.Approval.SelectedSupplier?.SupplierCode ?? "—"} · " +
                                                $"{data.Approval.SelectedSupplier?.SupplierName ?? "—"}");


                                        column.Item()
                                            .Text(
                                                $"Status: {data.Approval.Status}");


                                        column.Item()
                                            .Text(
                                                $"Approved by: {data.Approval.ApprovedByNavigation?.FullName ?? "—"}");


                                        column.Item()
                                            .Text(
                                                $"Approval date: {data.Approval.ApprovalDate:dd/MM/yyyy HH:mm}");


                                        if (
                                            !string.IsNullOrWhiteSpace(
                                                data.Approval.Notes))
                                        {
                                            column.Item()
                                                .Text(
                                                    $"Notes: {data.Approval.Notes}");
                                        }
                                    }
                                    else
                                    {
                                        column.Item()
                                            .Text(
                                                "No final approval recorded.");
                                    }
                                });


                        page.Footer()
                            .AlignCenter()
                            .Text(
                                "Supplier Selection DSS");
                    });
            });
    }
}


// =============================================================
// SUPPORT MODELS
// =============================================================

public class GeneratedReportResult
{
    public int ReportId { get; set; }

    public string ReportType { get; set; } =
        string.Empty;

    public string FileName { get; set; } =
        string.Empty;

    public string AbsolutePath { get; set; } =
        string.Empty;
}


public class ReportDownloadResult
{
    public string ReportType { get; set; } =
        string.Empty;

    public string FilePath { get; set; } =
        string.Empty;

    public string ContentType { get; set; } =
        string.Empty;

    public string DownloadName { get; set; } =
        string.Empty;
}


public class ReportData
{
    public EvaluationBatch Batch { get; set; } = null!;

    public List<EvaluationScore> Scores { get; set; } = new();

    public Ahpresult? AhpResult { get; set; }

    public List<ReportAHPWeight> AhpWeights { get; set; } = new();

    public List<AhppairwiseComparison> AhpComparisons { get; set; } = new();

    public List<TopsisIdealSolution> Ideals { get; set; } = new();

    public List<TopsisResult> TopsisResults { get; set; } = new();

    public Approval? Approval { get; set; }
}


public class ReportAHPWeight
{
    public string CriteriaCode { get; set; } =
        string.Empty;

    public string CriteriaName { get; set; } =
        string.Empty;

    public decimal WeightValue { get; set; }
}