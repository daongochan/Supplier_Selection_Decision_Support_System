using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Web.Extensions;
using System.Security.Claims;
using System.Text.Json;

namespace SupplierSelectionDSS.Controllers;

[Authorize]
public class DecisionResultController : Controller
{
    private readonly DecisionResultService _service;
    private readonly AuditLogService _auditLogService;
    private readonly ILogger<DecisionResultController> _logger;

    public DecisionResultController(
        DecisionResultService service,
        AuditLogService auditLogService,
        ILogger<DecisionResultController>? logger = null)
    {
        _service = service;
        _auditLogService = auditLogService;
        _logger = logger ?? NullLogger<DecisionResultController>.Instance;
    }


    // =========================================================
    // GET
    // /EvaluationBatch/{batchId}/Result
    // =========================================================

    [HttpGet]
    [Route("EvaluationBatch/{batchId:int}/Result")]
    [Route("DecisionResult")]
    [Route("DecisionResult/Index")]
    public async Task<IActionResult> Index(
        int batchId)
    {
        if (batchId <= 0)
        {
            TempData["WarningMessage"] = "Please select an evaluation batch to view decision results and approval.";
            return RedirectToAction("Index", "EvaluationBatch");
        }


        var model =
            await _service
                .GetResultAsync(
                    batchId);


        if (model == null)
        {
            TempData["WarningMessage"] = $"Evaluation batch with ID = {batchId} not found.";
            return RedirectToAction("Index", "EvaluationBatch");
        }


        // =====================================================
        // RE-EVALUATION IN PROGRESS
        // =====================================================

        if (
            string.Equals(
                model.ApprovalStatus,
                ApprovalStatus.PendingReevaluation,
                StringComparison.OrdinalIgnoreCase)
            &&
            !model.Ranking.Any())
        {
            TempData["ErrorMessage"] =
                "Batch is in re-evaluation state. " +
                "Please complete Evaluation and TOPSIS before viewing Decision Results.";


            return RedirectToAction(
                "Index",
                "Evaluation",
                new
                {
                    batchId
                });
        }


        // =====================================================
        // NO TOPSIS RESULT
        // =====================================================

        if (!model.Ranking.Any())
        {
            TempData["ErrorMessage"] =
                "TOPSIS results are not yet available. " +
                "Please complete the TOPSIS step first.";


            return RedirectToAction(
                "Index",
                "TOPSIS",
                new
                {
                    batchId
                });
        }


        return View(model);
    }


    // =========================================================
    // APPROVE
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            UserRole.Administrator + "," + UserRole.Manager)]
    [Route(
        "EvaluationBatch/{batchId:int}/Result/Approve")]
    public async Task<IActionResult> Approve(
        int batchId,
        int supplierId,
        string? notes,
        string? rowVersion = null)
    {
        if (
            batchId <= 0 ||
            supplierId <= 0)
        {
            TempData["ErrorMessage"] =
                "Invalid batch or supplier information.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }


        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        var approvedBy = userId.Value;


        try
        {
            // =================================================
            // APPROVE
            // =================================================

            var approvalId =
                await _service
                    .ApproveAsync(
                        batchId,
                        supplierId,
                        approvedBy,
                        notes,
                        rowVersion);


            // =================================================
            // AUDIT LOG
            // =================================================

            try
            {
                var result =
                    await _service
                        .GetResultAsync(
                            batchId);


                var selectedSupplier =
                    result?.Ranking
                        .FirstOrDefault(
                            x =>
                                x.SupplierId ==
                                supplierId);


                var newValue =
                    JsonSerializer.Serialize(
                        new
                        {
                            ApprovalId =
                                approvalId,

                            BatchId =
                                batchId,

                            SupplierId =
                                supplierId,

                            SupplierCode =
                                selectedSupplier?.SupplierCode,

                            SupplierName =
                                selectedSupplier?.SupplierName,

                            RankNo =
                                selectedSupplier?.RankNo,

                            ClosenessCoefficient =
                                selectedSupplier
                                    ?.ClosenessCoefficient,

                            Status =
                                ApprovalStatus.Approved,

                            IsFinal =
                                true,

                            Notes =
                                string.IsNullOrWhiteSpace(
                                    notes)
                                    ? null
                                    : notes.Trim(),

                            ApprovedBy =
                                approvedBy,

                            ApprovalDate =
                                DateTime.UtcNow
                        });


                await _auditLogService
                    .LogAsync(
                        approvedBy,
                        AuditLogService
                            .Actions
                            .Approve,
                        "Approvals",
                        approvalId,
                        null,
                        newValue,
                        $"Approve supplier " +
                        $"{selectedSupplier?.SupplierCode ?? supplierId.ToString()} " +
                        $"for Batch #{batchId}.");
            }
            catch
            {
                // Audit log không được làm hỏng
                // nghiệp vụ Approve đã thành công.
            }


            TempData["SuccessMessage"] =
                "Supplier has been approved.";
        }
        catch (
            BusinessRuleException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (
            ArgumentException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Lỗi khi phê duyệt kết quả cho đợt {BatchId}.",
                batchId);
            TempData["ErrorMessage"] =
                "An error occurred while approving the result. Please try again later.";
        }


        return RedirectToAction(
            nameof(Index),
            new
            {
                batchId
            });
    }


    // =========================================================
    // REJECT
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            UserRole.Administrator + "," + UserRole.Manager)]
    [Route(
        "EvaluationBatch/{batchId:int}/Result/Reject")]
    public async Task<IActionResult> Reject(
        int batchId,
        string? notes,
        string? rowVersion = null)
    {
        if (batchId <= 0)
        {
            TempData["ErrorMessage"] =
                "Invalid evaluation batch.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }


        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        var approvedBy = userId.Value;


        try
        {
            // =================================================
            // REJECT
            // =================================================

            var approvalId =
                await _service
                    .RejectAsync(
                        batchId,
                        notes,
                        approvedBy,
                        rowVersion);


            // =================================================
            // AUDIT LOG
            // =================================================

            try
            {
                var newValue =
                    JsonSerializer.Serialize(
                        new
                        {
                            ApprovalId =
                                approvalId,

                            BatchId =
                                batchId,

                            Status =
                                ApprovalStatus.Rejected,

                            IsFinal =
                                false,

                            Notes =
                                string.IsNullOrWhiteSpace(
                                    notes)
                                    ? null
                                    : notes.Trim(),

                            ApprovedBy =
                                approvedBy,

                            ApprovalDate =
                                DateTime.UtcNow
                        });


                await _auditLogService
                    .LogAsync(
                        approvedBy,
                        AuditLogService
                            .Actions
                            .Reject,
                        "Approvals",
                        approvalId,
                        null,
                        newValue,
                        $"Reject decision result for Batch #{batchId}.");
            }
            catch
            {
                // Audit log không được làm hỏng
                // nghiệp vụ Reject đã thành công.
            }


            TempData["SuccessMessage"] =
                "Selection decision has been rejected. " +
                "You may request a re-evaluation.";
        }
        catch (
            BusinessRuleException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (
            ArgumentException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Lỗi khi từ chối kết quả cho đợt {BatchId}.",
                batchId);
            TempData["ErrorMessage"] =
                "An error occurred while rejecting the result. Please try again later.";
        }


        return RedirectToAction(
            nameof(Index),
            new
            {
                batchId
            });
    }


    // =========================================================
    // RE-EVALUATION
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            UserRole.Administrator + "," + UserRole.Manager)]
    [Route(
        "EvaluationBatch/{batchId:int}/Result/Reevaluate")]
    public async Task<IActionResult> Reevaluate(
        int batchId,
        string? notes,
        string? rowVersion = null)
    {
        if (batchId <= 0)
        {
            TempData["ErrorMessage"] =
                "Invalid evaluation batch.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }


        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        var approvedBy = userId.Value;


        try
        {
            // =================================================
            // REQUEST RE-EVALUATION
            // =================================================

            var approvalId =
                await _service
                    .RequestReevaluationAsync(
                        batchId,
                        notes,
                        approvedBy,
                        rowVersion);


            // =================================================
            // AUDIT LOG
            // =================================================

            try
            {
                var newValue =
                    JsonSerializer.Serialize(
                        new
                        {
                            ApprovalId =
                                approvalId,

                            BatchId =
                                batchId,

                            Status =
                                ApprovalStatus.PendingReevaluation,

                            IsFinal =
                                false,

                            Notes =
                                string.IsNullOrWhiteSpace(
                                    notes)
                                    ? null
                                    : notes.Trim(),

                            ApprovedBy =
                                approvedBy,

                            ApprovalDate =
                                DateTime.UtcNow
                        });


                await _auditLogService
                    .LogAsync(
                        approvedBy,
                        AuditLogService
                            .Actions
                            .Reevaluate,
                        "Approvals",
                        approvalId,
                        null,
                        newValue,
                        $"Request re-evaluation for Batch #{batchId}. " +
                        "Previous Evaluation, AHP, and TOPSIS data have been invalidated.");
            }
            catch
            {
                // Audit log không được làm hỏng
                // nghiệp vụ Re-evaluate đã thành công.
            }


            TempData["SuccessMessage"] =
                "Batch has been marked for re-evaluation. " +
                "Previous Evaluation, AHP, and TOPSIS data have been invalidated.";
        }
        catch (
            BusinessRuleException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (
            ArgumentException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Lỗi khi yêu cầu đánh giá lại cho đợt {BatchId}.",
                batchId);
            TempData["ErrorMessage"] =
                "An error occurred while requesting re-evaluation. Please try again later.";
        }


        return RedirectToAction(
            "Index",
            "Evaluation",
            new
            {
                batchId
            });
    }
}
