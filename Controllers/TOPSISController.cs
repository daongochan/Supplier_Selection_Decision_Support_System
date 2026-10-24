using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Web.Extensions;
using System.Security.Claims;
using System.Text.Json;

namespace SupplierSelectionDSS.Controllers;

[Authorize(Roles = UserRole.Administrator + "," + UserRole.Expert)]
public class TOPSISController : Controller
{
    private readonly TOPSISService _topsisService;
    private readonly AuditLogService _auditLogService;
    private readonly ILogger<TOPSISController> _logger;

    public TOPSISController(
        TOPSISService topsisService,
        AuditLogService auditLogService,
        ILogger<TOPSISController>? logger = null)
    {
        _topsisService = topsisService;
        _auditLogService = auditLogService;
        _logger = logger ?? NullLogger<TOPSISController>.Instance;
    }


    // =========================================================
    // GET
    // /EvaluationBatch/{batchId}/TOPSIS
    // =========================================================

    [HttpGet]
    [Route("EvaluationBatch/{batchId:int}/TOPSIS")]
    [Route("TOPSIS")]
    [Route("TOPSIS/Index")]
    public async Task<IActionResult> Index(
        int batchId)
    {
        if (batchId <= 0)
        {
            TempData["WarningMessage"] = "Please select an evaluation batch to view TOPSIS rankings.";
            return RedirectToAction("Index", "EvaluationBatch");
        }


        var model =
            await _topsisService
                .GetWorkspaceAsync(
                    batchId);


        if (model == null)
        {
            TempData["WarningMessage"] = $"Evaluation batch with ID = {batchId} not found.";
            return RedirectToAction("Index", "EvaluationBatch");
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
        int batchId,
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
                    batchId,
                    rowVersion);


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
                    $"TOPSIS calculation for Batch #{batchId} completed successfully. " +
                    $"Suppliers: {supplierCount}; " +
                    $"Criteria: {criteriaCount}; " +
                    $"Rankings: {rankingCount}.";


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
                "TOPSIS calculated and suppliers ranked successfully.";
        }
        catch (BusinessRuleException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Lỗi khi tính toán TOPSIS cho đợt {BatchId}.",
                batchId);
            TempData["ErrorMessage"] =
                "An error occurred during TOPSIS calculation. Please try again later.";
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
