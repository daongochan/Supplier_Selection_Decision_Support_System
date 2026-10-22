using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using System.Security.Claims;
using System.Text.Json;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Web.Extensions;

namespace SupplierSelectionDSS.Controllers;

[Authorize(Roles = UserRole.Administrator + "," + UserRole.Expert)]
public class TOPSISController : Controller
{
    private readonly TOPSISService _topsisService;

    private readonly AuditLogService _auditLogService;


    public TOPSISController(
        TOPSISService topsisService,
        AuditLogService auditLogService)
    {
        _topsisService =
            topsisService;

        _auditLogService =
            auditLogService;
    }


    // =========================================================
    // GET
    // /EvaluationBatch/{batchId}/TOPSIS
    // =========================================================

    [HttpGet]
    [Route("EvaluationBatch/{batchId:int}/TOPSIS")]
    public async Task<IActionResult> Index(
        int batchId)
    {
        if (batchId <= 0)
        {
            return BadRequest(
                "Batch ID không hợp lệ.");
        }


        var model =
            await _topsisService
                .GetWorkspaceAsync(
                    batchId);


        if (model == null)
        {
            return NotFound(
                $"Không tìm thấy Evaluation Batch với ID = {batchId}.");
        }


        return View(model);
    }


    // =========================================================
    // CALCULATE
    // /EvaluationBatch/{batchId}/TOPSIS/Calculate
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(
        "EvaluationBatch/{batchId:int}/TOPSIS/Calculate")]
    public async Task<IActionResult> Calculate(
        int batchId)
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


        // =====================================================
        // CURRENT USER
        // =====================================================

        var calculatedBy = User.GetUserId();
        if (!calculatedBy.HasValue)
        {
            return Unauthorized();
        }


        try
        {
            // =================================================
            // CAPTURE PREVIOUS RESULT
            // =================================================

            TOPSISViewModel? beforeModel =
                null;


            try
            {
                beforeModel =
                    await _topsisService
                        .GetWorkspaceAsync(
                            batchId);
            }
            catch
            {
                // Audit snapshot không được làm hỏng
                // workflow TOPSIS.
            }


            // =================================================
            // CALCULATE
            // =================================================

            await _topsisService
                .CalculateAsync(
                    batchId);


            // =================================================
            // CAPTURE NEW RESULT
            // =================================================

            TOPSISViewModel? afterModel =
                null;


            try
            {
                afterModel =
                    await _topsisService
                        .GetWorkspaceAsync(
                            batchId);
            }
            catch
            {
                // Audit snapshot không được làm hỏng
                // thao tác Calculate TOPSIS đã thành công.
            }


            // =================================================
            // AUDIT LOG
            // =================================================

            try
            {
                var oldValue =
                    CreateTopsisSnapshot(
                        beforeModel);


                var newValue =
                    CreateTopsisSnapshot(
                        afterModel);


                var rankingCount =
                    afterModel?.Ranking.Count
                    ?? 0;


                var criteriaCount =
                    afterModel?.Criteria.Count
                    ?? 0;


                var supplierCount =
                    afterModel?.Suppliers.Count
                    ?? 0;


                var description =
                    $"Tính TOPSIS cho Batch #{batchId} thành công. " +
                    $"Supplier: {supplierCount}; " +
                    $"Criteria: {criteriaCount}; " +
                    $"Ranking: {rankingCount}.";


                await _auditLogService
                    .LogAsync(
                        calculatedBy,
                        AuditLogService
                            .Actions
                            .CalculateTOPSIS,
                        "TopsisResults",
                        batchId,
                        oldValue,
                        newValue,
                        description);
            }
            catch
            {
                // Audit log không được làm hỏng
                // workflow TOPSIS đã thành công.
            }


            TempData["SuccessMessage"] =
                "TOPSIS đã được tính toán và xếp hạng nhà cung cấp thành công.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (InvalidOperationException ex)
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
    // CREATE TOPSIS SNAPSHOT
    // =========================================================

    private static string?
        CreateTopsisSnapshot(
            TOPSISViewModel? model)
    {
        if (model == null)
        {
            return null;
        }


        var idealSolutions =
            model.IdealSolutions
                .OrderBy(
                    x =>
                        x.CriteriaCode)
                .Select(
                    x =>
                        new
                        {
                            x.CriteriaId,
                            x.CriteriaCode,
                            x.CriteriaName,
                            x.CriteriaType,
                            x.Unit,
                            x.PISValue,
                            x.NISValue
                        })
                .ToList();


        var ranking =
            model.Ranking
                .OrderBy(
                    x =>
                        x.RankNo)
                .Select(
                    x =>
                        new
                        {
                            x.SupplierId,
                            x.SupplierCode,
                            x.SupplierName,
                            x.RankNo,
                            x.DistanceToPIS,
                            x.DistanceToNIS,
                            x.ClosenessCoefficient
                        })
                .ToList();


        return JsonSerializer.Serialize(
            new
            {
                model.BatchId,
                model.BatchCode,
                model.BatchName,

                AHP =
                    new
                    {
                        model.HasAHPResult,
                        model.IsAHPConsistent,
                        model.AHPConsistencyRatio
                    },

                IdealSolutions =
                    idealSolutions,

                Ranking =
                    ranking
            });
    }
}
