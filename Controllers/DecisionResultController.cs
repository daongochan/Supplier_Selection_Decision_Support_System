using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Services;
using System.Security.Claims;
using System.Text.Json;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Controllers;

[Authorize]
public class DecisionResultController : Controller
{
    private readonly DecisionResultService _service;

    private readonly AuditLogService _auditLogService;


    public DecisionResultController(
        DecisionResultService service,
        AuditLogService auditLogService)
    {
        _service =
            service;

        _auditLogService =
            auditLogService;
    }


    // =========================================================
    // GET
    // /EvaluationBatch/{batchId}/Result
    // =========================================================

    [HttpGet]
    [Route("EvaluationBatch/{batchId:int}/Result")]
    public async Task<IActionResult> Index(
        int batchId)
    {
        if (batchId <= 0)
        {
            return NotFound();
        }


        var model =
            await _service
                .GetResultAsync(
                    batchId);


        if (model == null)
        {
            return NotFound();
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
                "Batch đang ở trạng thái đánh giá lại. " +
                "Hãy hoàn thiện lại Evaluation và TOPSIS trước khi xem Decision Result.";


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
                "Chưa có kết quả TOPSIS. " +
                "Hãy hoàn thành bước TOPSIS trước.";


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
        string? notes)
    {
        if (
            batchId <= 0 ||
            supplierId <= 0)
        {
            TempData["ErrorMessage"] =
                "Thông tin Batch hoặc nhà cung cấp không hợp lệ.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }


        var claim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);


        if (
            claim == null ||
            !int.TryParse(
                claim.Value,
                out var approvedBy) ||
            approvedBy <= 0)
        {
            return Unauthorized();
        }


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
                        notes);


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
                        $"Phê duyệt nhà cung cấp " +
                        $"{selectedSupplier?.SupplierCode ?? supplierId.ToString()} " +
                        $"cho Batch #{batchId}.");
            }
            catch
            {
                // Audit log không được làm hỏng
                // nghiệp vụ Approve đã thành công.
            }


            TempData["SuccessMessage"] =
                "Nhà cung cấp đã được phê duyệt.";
        }
        catch (
            ArgumentException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (
            InvalidOperationException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
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
        string? notes)
    {
        if (batchId <= 0)
        {
            TempData["ErrorMessage"] =
                "Evaluation Batch không hợp lệ.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }


        var claim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);


        if (
            claim == null ||
            !int.TryParse(
                claim.Value,
                out var approvedBy) ||
            approvedBy <= 0)
        {
            return Unauthorized();
        }


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
                        approvedBy);


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
                        $"Từ chối kết quả Decision của Batch #{batchId}.");
            }
            catch
            {
                // Audit log không được làm hỏng
                // nghiệp vụ Reject đã thành công.
            }


            TempData["SuccessMessage"] =
                "Kết quả lựa chọn đã bị từ chối. " +
                "Bạn có thể yêu cầu đánh giá lại.";
        }
        catch (
            ArgumentException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (
            InvalidOperationException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
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
        string? notes)
    {
        if (batchId <= 0)
        {
            TempData["ErrorMessage"] =
                "Evaluation Batch không hợp lệ.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }


        var claim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);


        if (
            claim == null ||
            !int.TryParse(
                claim.Value,
                out var approvedBy) ||
            approvedBy <= 0)
        {
            return Unauthorized();
        }


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
                        approvedBy);


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
                        $"Yêu cầu đánh giá lại Batch #{batchId}. " +
                        "Evaluation, AHP và TOPSIS cũ đã được vô hiệu hóa.");
            }
            catch
            {
                // Audit log không được làm hỏng
                // nghiệp vụ Re-evaluate đã thành công.
            }


            TempData["SuccessMessage"] =
                "Batch đã được yêu cầu đánh giá lại. " +
                "Dữ liệu Evaluation, AHP và TOPSIS cũ đã được vô hiệu hóa.";
        }
        catch (
            ArgumentException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (
            InvalidOperationException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
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