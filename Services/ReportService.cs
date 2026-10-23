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


    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

        // Check if a FinalApproval frozen snapshot is available for reproducible historical reporting
        var finalSnapshotRow = await _context.BatchRoundSnapshots
            .AsNoTracking()
            .Include(x => x.CreatedByNavigation)
            .Where(x => x.BatchId == batchId && x.SnapshotType == "FinalApproval")
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.SnapshotId)
            .FirstOrDefaultAsync();

        if (finalSnapshotRow != null && !string.IsNullOrWhiteSpace(finalSnapshotRow.SnapshotData))
        {
            BatchSnapshotPayload? snap = null;
            try
            {
                snap = JsonSerializer.Deserialize<BatchSnapshotPayload>(finalSnapshotRow.SnapshotData, SnapshotJsonOptions);
            }
            catch
            {
                snap = null;
            }

            if (snap != null
                && snap.Suppliers.Count > 0
                && snap.Criteria.Count > 0
                && snap.TopsisResults.Count > 0
                && snap.TopsisResults.All(r => !string.IsNullOrWhiteSpace(r.SupplierCode)))
            {
                return await BuildReportDataFromSnapshotAsync(batch, finalSnapshotRow, snap);
            }
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

    private async Task<ReportData> BuildReportDataFromSnapshotAsync(
        EvaluationBatch liveBatch,
        BatchRoundSnapshot snapshotRow,
        BatchSnapshotPayload snap)
    {
        var frozenSuppliers = snap.Suppliers
            .Select(s => new Supplier
            {
                SupplierId = s.SupplierId,
                SupplierCode = s.SupplierCode,
                SupplierName = s.SupplierName
            })
            .ToDictionary(s => s.SupplierId, s => s);

        var frozenCriteria = snap.Criteria
            .Select(c => new Criterion
            {
                CriteriaId = c.CriteriaId,
                CriteriaCode = c.CriteriaCode,
                CriteriaName = c.CriteriaName,
                CriteriaType = c.CriteriaType,
                Unit = c.Unit
            })
            .ToDictionary(c => c.CriteriaId, c => c);

        var frozenBatch = new EvaluationBatch
        {
            BatchId = liveBatch.BatchId,
            BatchCode = string.IsNullOrWhiteSpace(snap.BatchCode) ? liveBatch.BatchCode : snap.BatchCode,
            BatchName = string.IsNullOrWhiteSpace(snap.BatchName) ? liveBatch.BatchName : snap.BatchName,
            Status = liveBatch.Status,
            GroupId = liveBatch.GroupId,
            Group = (!string.IsNullOrWhiteSpace(snap.GroupCode) || !string.IsNullOrWhiteSpace(snap.GroupName))
                ? new ProductGroup
                {
                    GroupId = liveBatch.GroupId,
                    GroupCode = snap.GroupCode ?? liveBatch.Group?.GroupCode ?? string.Empty,
                    GroupName = snap.GroupName ?? liveBatch.Group?.GroupName ?? string.Empty
                }
                : liveBatch.Group,
            Suppliers = frozenSuppliers.Values.ToList(),
            Criteria = frozenCriteria.Values.ToList()
        };

        var scores = snap.EvaluationScores
            .Select(s =>
            {
                if (!frozenSuppliers.TryGetValue(s.SupplierId, out var sup))
                {
                    sup = new Supplier
                    {
                        SupplierId = s.SupplierId,
                        SupplierCode = s.SupplierCode ?? string.Empty,
                        SupplierName = s.SupplierName ?? string.Empty
                    };
                }

                if (!frozenCriteria.TryGetValue(s.CriteriaId, out var crit))
                {
                    crit = new Criterion
                    {
                        CriteriaId = s.CriteriaId,
                        CriteriaCode = s.CriteriaCode ?? string.Empty,
                        CriteriaName = s.CriteriaName ?? string.Empty
                    };
                }

                return new EvaluationScore
                {
                    BatchId = liveBatch.BatchId,
                    SupplierId = s.SupplierId,
                    Supplier = sup,
                    CriteriaId = s.CriteriaId,
                    Criteria = crit,
                    ScoreValue = s.ScoreValue
                };
            })
            .OrderBy(s => s.Supplier.SupplierCode)
            .ThenBy(s => s.Criteria.CriteriaCode)
            .ToList();

        var ahpResult = snap.AhpResult == null
            ? null
            : new Ahpresult
            {
                BatchId = liveBatch.BatchId,
                LambdaMax = snap.AhpResult.LambdaMax,
                Civalue = snap.AhpResult.Civalue,
                Rivalue = snap.AhpResult.Rivalue,
                Crvalue = snap.AhpResult.Crvalue,
                IsConsistent = snap.AhpResult.IsConsistent
            };

        var ahpWeights = snap.AhpWeights
            .Select(w =>
            {
                frozenCriteria.TryGetValue(w.CriteriaId, out var crit);
                return new ReportAHPWeight
                {
                    CriteriaCode = w.CriteriaCode ?? crit?.CriteriaCode ?? string.Empty,
                    CriteriaName = w.CriteriaName ?? crit?.CriteriaName ?? string.Empty,
                    WeightValue = w.WeightValue
                };
            })
            .OrderBy(w => w.CriteriaCode)
            .ToList();

        var ahpComparisons = snap.AhpComparisons
            .Select(c =>
            {
                frozenCriteria.TryGetValue(c.CriteriaRowId, out var rowCrit);
                frozenCriteria.TryGetValue(c.CriteriaColId, out var colCrit);
                return new AhppairwiseComparison
                {
                    BatchId = liveBatch.BatchId,
                    CriteriaRowId = c.CriteriaRowId,
                    CriteriaRow = rowCrit ?? new Criterion { CriteriaId = c.CriteriaRowId, CriteriaCode = c.CriteriaRowCode ?? string.Empty },
                    CriteriaColId = c.CriteriaColId,
                    CriteriaCol = colCrit ?? new Criterion { CriteriaId = c.CriteriaColId, CriteriaCode = c.CriteriaColCode ?? string.Empty },
                    ComparisonValue = c.ComparisonValue
                };
            })
            .OrderBy(c => c.CriteriaRow.CriteriaCode)
            .ThenBy(c => c.CriteriaCol.CriteriaCode)
            .ToList();

        var ideals = snap.TopsisIdeals
            .Select(i =>
            {
                if (!frozenCriteria.TryGetValue(i.CriteriaId, out var crit))
                {
                    crit = new Criterion
                    {
                        CriteriaId = i.CriteriaId,
                        CriteriaCode = i.CriteriaCode ?? string.Empty,
                        CriteriaName = i.CriteriaName ?? string.Empty,
                        CriteriaType = i.CriteriaType ?? string.Empty,
                        Unit = i.Unit
                    };
                }

                return new TopsisIdealSolution
                {
                    BatchId = liveBatch.BatchId,
                    CriteriaId = i.CriteriaId,
                    Criteria = crit,
                    Pisvalue = i.Pisvalue,
                    Nisvalue = i.Nisvalue
                };
            })
            .OrderBy(i => i.Criteria.CriteriaCode)
            .ToList();

        var topsisResults = snap.TopsisResults
            .Select(r =>
            {
                if (!frozenSuppliers.TryGetValue(r.SupplierId, out var sup))
                {
                    sup = new Supplier
                    {
                        SupplierId = r.SupplierId,
                        SupplierCode = r.SupplierCode ?? string.Empty,
                        SupplierName = r.SupplierName ?? string.Empty
                    };
                }

                return new TopsisResult
                {
                    BatchId = liveBatch.BatchId,
                    SupplierId = r.SupplierId,
                    Supplier = sup,
                    DistanceToPis = r.DistanceToPis,
                    DistanceToNis = r.DistanceToNis,
                    ClosenessCoefficient = r.ClosenessCoefficient,
                    RankNo = r.RankNo
                };
            })
            .OrderBy(r => r.RankNo)
            .ToList();

        var liveApproval = await _context.Approvals
            .AsNoTracking()
            .Include(x => x.ApprovedByNavigation)
            .Where(x => x.BatchId == liveBatch.BatchId && x.IsFinal && x.Status == ApprovalStatus.Approved)
            .OrderByDescending(x => x.ApprovalDate)
            .ThenByDescending(x => x.ApprovalId)
            .FirstOrDefaultAsync();

        Supplier? selectedSup = null;
        if (snap.SelectedSupplierId.HasValue)
        {
            if (!frozenSuppliers.TryGetValue(snap.SelectedSupplierId.Value, out selectedSup))
            {
                selectedSup = new Supplier
                {
                    SupplierId = snap.SelectedSupplierId.Value,
                    SupplierCode = snap.SelectedSupplierCode ?? string.Empty,
                    SupplierName = snap.SelectedSupplierName ?? string.Empty
                };
            }
        }

        var resolvedApproverId = snap.ApprovedBy ?? liveApproval?.ApprovedBy ?? snapshotRow.CreatedBy ?? 0;

        var approval = new Approval
        {
            ApprovalId = liveApproval?.ApprovalId ?? 0,
            BatchId = liveBatch.BatchId,
            SelectedSupplierId = snap.SelectedSupplierId ?? liveApproval?.SelectedSupplierId,
            SelectedSupplier = selectedSup,
            ApprovedBy = resolvedApproverId,
            ApprovedByNavigation = new User
            {
                UserId = resolvedApproverId,
                FullName = snap.ActorName
                    ?? liveApproval?.ApprovedByNavigation?.FullName
                    ?? snapshotRow.CreatedByNavigation?.FullName
                    ?? string.Empty
            },
            ApprovalDate = snap.ApprovedAt ?? liveApproval?.ApprovalDate ?? snapshotRow.CreatedAt,
            Notes = snap.Notes ?? liveApproval?.Notes,
            Status = ApprovalStatus.Approved,
            IsFinal = true
        };

        return new ReportData
        {
            Batch = frozenBatch,
            Scores = scores,
            AhpResult = ahpResult,
            AhpWeights = ahpWeights,
            AhpComparisons = ahpComparisons,
            Ideals = ideals,
            TopsisResults = topsisResults,
            Approval = approval,
            NormalizedMatrix = snap.NormalizedMatrix ?? new List<TOPSISMatrixRowViewModel>(),
            WeightedMatrix = snap.WeightedMatrix ?? new List<TOPSISMatrixRowViewModel>(),
            SensitivityAnalysis = snap.SensitivityAnalysis
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
