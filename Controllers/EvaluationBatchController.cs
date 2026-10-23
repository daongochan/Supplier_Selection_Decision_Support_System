using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Web.Extensions;

namespace SupplierSelectionDSS.Controllers;

[Authorize(
    Roles =
        UserRole.Administrator + "," + UserRole.PurchasingStaff + "," + UserRole.Expert + "," + UserRole.Manager)]
public class EvaluationBatchController : Controller
{
    private readonly EvaluationBatchService _batchService;
    private readonly AuditLogService _auditLogService;
    private readonly ILogger<EvaluationBatchController> _logger;

    public EvaluationBatchController(
        EvaluationBatchService batchService,
        AuditLogService auditLogService,
        ILogger<EvaluationBatchController>? logger = null)
    {
        _batchService = batchService;
        _auditLogService = auditLogService;
        _logger = logger ?? NullLogger<EvaluationBatchController>.Instance;
    }


    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword,
        string status = "All",
        int? groupId = null,
        int page = 1,
        int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Max(pageSize, 1);

        var paged = await _batchService.SearchAsync(
            keyword,
            status,
            groupId,
            page,
            pageSize);

        var (total, evaluating, completed, draft) = await _batchService.GetKpiSummaryAsync();
        var activeGroups = await _batchService.GetActiveProductGroupsAsync();

        var model = new EvaluationBatchIndexViewModel
        {
            Keyword = keyword,
            Status = status,
            GroupId = groupId,
            Groups = activeGroups,
            TotalBatches = total,
            EvaluatingBatches = evaluating,
            CompletedBatches = completed,
            DraftBatches = draft,
            Batches = paged.Items.ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages
        };

        return View(model);
    }


    // =========================================================
    // CREATE - GET
    // =========================================================

    [HttpGet]
    [Authorize(
        Roles =
            UserRole.Administrator + "," + UserRole.PurchasingStaff)]
    public async Task<IActionResult> Create()
    {
        ViewBag.ProductGroups =
            await _batchService
                .GetActiveProductGroupsAsync();


        ViewBag.Criteria =
            await _batchService
                .GetActiveCriteriaAsync();


        return View(
            new EvaluationBatchCreateViewModel());
    }


    // =========================================================
    // CREATE - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            UserRole.Administrator + "," + UserRole.PurchasingStaff)]
    public async Task<IActionResult> Create(
        EvaluationBatchCreateViewModel model)
    {
        model.BatchCode =
            model.BatchCode?.Trim()
            ?? string.Empty;


        model.BatchName =
            model.BatchName?.Trim()
            ?? string.Empty;


        model.Description =
            model.Description?.Trim();


        if (
            await _batchService
                .BatchCodeExistsAsync(
                    model.BatchCode))
        {
            ModelState.AddModelError(
                nameof(model.BatchCode),
                "Mã đợt đánh giá đã tồn tại.");
        }


        if (!ModelState.IsValid)
        {
            await LoadCreateDataAsync();


            if (model.GroupId.HasValue)
            {
                ViewBag.Suppliers =
                    await _batchService
                        .GetSuppliersByGroupAsync(
                            model.GroupId.Value);
            }


            return View(model);
        }


        // =====================================================
        // CURRENT USER
        // =====================================================

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        var createdBy = userId.Value;


        try
        {
            // =================================================
            // CREATE BATCH
            // =================================================

            var batchId =
                await _batchService
                    .CreateAsync(
                        model,
                        createdBy);


            // =================================================
            // AUDIT LOG
            // =================================================

            try
            {
                var newValue =
                    JsonSerializer.Serialize(
                        new
                        {
                            model.BatchCode,
                            model.BatchName,
                            model.Description,
                            model.GroupId,
                            SupplierCount =
                                model.SelectedSupplierIds
                                    .Distinct()
                                    .Count(),
                            CriteriaCount =
                                model.SelectedCriteriaIds
                                    .Distinct()
                                    .Count(),
                            model.StartDate,
                            model.EndDate,
                            Status =
                                BatchStatus.Draft
                        });


                await _auditLogService
                    .LogAsync(
                        createdBy,
                        AuditLogService
                            .Actions
                            .CreateBatch,
                        "EvaluationBatches",
                        batchId,
                        null,
                        newValue,
                        $"Tạo đợt đánh giá '{model.BatchCode}' - '{model.BatchName}'.");
            }
            catch
            {
                // Audit log không được làm hỏng
                // workflow Create Batch đã thành công.
            }


            TempData["SuccessMessage"] =
                "Đợt đánh giá đã được tạo thành công.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id =
                        batchId
                });
        }
        catch (
            BusinessRuleException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);


            await LoadCreateDataAsync();


            if (model.GroupId.HasValue)
            {
                ViewBag.Suppliers =
                    await _batchService
                        .GetSuppliersByGroupAsync(
                            model.GroupId.Value);
            }


            return View(model);
        }
        catch (
            ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);


            await LoadCreateDataAsync();


            if (model.GroupId.HasValue)
            {
                ViewBag.Suppliers =
                    await _batchService
                        .GetSuppliersByGroupAsync(
                            model.GroupId.Value);
            }


            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Lỗi khi tạo đợt đánh giá mới.");

            ModelState.AddModelError(
                string.Empty,
                "Không thể tạo đợt đánh giá. Vui lòng thử lại sau.");


            await LoadCreateDataAsync();


            if (model.GroupId.HasValue)
            {
                ViewBag.Suppliers =
                    await _batchService
                        .GetSuppliersByGroupAsync(
                            model.GroupId.Value);
            }


            return View(model);
        }
    }


    // =========================================================
    // SUPPLIERS BY GROUP - AJAX
    // =========================================================

    [HttpGet]
    [Authorize(
        Roles =
            UserRole.Administrator + "," + UserRole.PurchasingStaff)]
    public async Task<IActionResult> SuppliersByGroup(
        int groupId)
    {
        if (groupId <= 0)
        {
            return BadRequest(
                "Product Group không hợp lệ.");
        }


        var suppliers =
            await _batchService
                .GetSuppliersByGroupAsync(
                    groupId);


        return Json(suppliers);
    }


    // =========================================================
    // START EVALUATION
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            UserRole.Administrator + "," + UserRole.PurchasingStaff)]
    public async Task<IActionResult> Start(
        int id,
        string? rowVersion = null)
    {
        try
        {
            // =================================================
            // START BATCH
            // =================================================

            await _batchService
                .StartAsync(id, rowVersion);


            // =================================================
            // AUDIT USER
            // =================================================

            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }
            var startedBy = userId.Value;


            // =================================================
            // AUDIT LOG
            // =================================================

            try
            {
                var oldValue =
                    JsonSerializer.Serialize(
                        new
                        {
                            Status =
                                BatchStatus.Draft
                        });


                var newValue =
                    JsonSerializer.Serialize(
                        new
                        {
                            Status =
                                BatchStatus.Evaluating
                        });


                await _auditLogService
                    .LogAsync(
                        startedBy,
                        AuditLogService
                            .Actions
                            .StartBatch,
                        "EvaluationBatches",
                        id,
                        oldValue,
                        newValue,
                        $"Bắt đầu đợt đánh giá Batch #{id}.");
            }
            catch
            {
                // Audit log không được làm hỏng
                // workflow Start Batch đã thành công.
            }


            TempData["SuccessMessage"] =
                "Đợt đánh giá đã được bắt đầu. " +
                "Batch chuyển sang trạng thái Evaluating.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }
        catch (
            BusinessRuleException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }
        catch (
            ArgumentException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Lỗi khi bắt đầu đợt đánh giá ID {BatchId}.",
                id);

            TempData["ErrorMessage"] =
                "Không thể bắt đầu đợt đánh giá. Vui lòng thử lại sau.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }
    }


    // =========================================================
    // CANCEL
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            UserRole.Administrator + "," + UserRole.Manager)]
    public async Task<IActionResult> Cancel(
        int id,
        string? reason,
        string? rowVersion = null)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        var cancelledBy = userId.Value;

        try
        {
            var batch = await _batchService.GetDetailAsync(id);
            if (batch == null)
            {
                return NotFound();
            }

            var oldValue = JsonSerializer.Serialize(new
            {
                BatchId = batch.BatchId,
                BatchCode = batch.BatchCode,
                Status = batch.Status
            });

            await _batchService.CancelAsync(id, reason, rowVersion);

            var newValue = JsonSerializer.Serialize(new
            {
                BatchId = batch.BatchId,
                BatchCode = batch.BatchCode,
                Status = BatchStatus.Cancelled,
                Reason = reason
            });

            try
            {
                await _auditLogService.LogAsync(
                    cancelledBy,
                    AuditActions.CancelBatch,
                    "EvaluationBatches",
                    id,
                    oldValue,
                    newValue,
                    $"Hủy đợt đánh giá '{batch.BatchCode}'. Lý do: {reason ?? "Không có"}");
            }
            catch
            {
                // Audit log không được làm hỏng workflow
            }

            TempData["SuccessMessage"] = $"Đợt đánh giá '{batch.BatchCode}' đã được hủy thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hủy đợt đánh giá ID {BatchId}.", id);
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đợt đánh giá. Vui lòng thử lại sau.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }


    // =========================================================
    // DETAILS
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Details(
        int id)
    {
        var batch =
            await _batchService
                .GetDetailAsync(id);


        if (batch == null)
        {
            return NotFound();
        }


        return View(batch);
    }


    // =========================================================
    // SUPPORT
    // =========================================================

    private async Task LoadCreateDataAsync()
    {
        ViewBag.ProductGroups =
            await _batchService
                .GetActiveProductGroupsAsync();


        ViewBag.Criteria =
            await _batchService
                .GetActiveCriteriaAsync();
    }
}
