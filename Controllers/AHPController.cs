using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Exceptions;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Web.Extensions;
using System.Text.Json;

namespace SupplierSelectionDSS.Controllers;

[Authorize(
    Roles =
        UserRole.Administrator + "," + UserRole.Expert)]
public class AHPController : Controller
{
    private readonly AHPService _ahpService;
    private readonly AuditLogService _auditLogService;
    private readonly ILogger<AHPController> _logger;

    public AHPController(
        AHPService ahpService,
        AuditLogService auditLogService,
        ILogger<AHPController>? logger = null)
    {
        _ahpService = ahpService;
        _auditLogService = auditLogService;
        _logger = logger ?? NullLogger<AHPController>.Instance;
    }


    // =========================================================
    // GET
    // /EvaluationBatch/{batchId}/AHP
    // =========================================================

    [HttpGet]
    [Route("EvaluationBatch/{batchId:int}/AHP")]
    [Route("AHP")]
    [Route("AHP/Index")]
    public async Task<IActionResult> Index(
        int batchId)
    {
        if (batchId <= 0)
        {
            TempData["WarningMessage"] = "Please select an evaluation batch to perform AHP matrix analysis.";
            return RedirectToAction("Index", "EvaluationBatch");
        }


        var model =
            await _ahpService
                .GetComparisonMatrixAsync(
                    batchId);


        if (model == null)
        {
            TempData["WarningMessage"] = $"Evaluation batch with ID = {batchId} not found.";
            return RedirectToAction("Index", "EvaluationBatch");
        }


        return View(model);
    }


    // =========================================================
    // SAVE PAIRWISE COMPARISON
    // /EvaluationBatch/{batchId}/AHP/Save
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(
        "EvaluationBatch/{batchId:int}/AHP/Save")]
    public async Task<IActionResult> Save(
        int batchId,
        AHPComparisonInputViewModel model)
    {
        if (batchId <= 0)
        {
            return NotFound();
        }


        if (model == null)
        {
            TempData["ErrorMessage"] =
                "Invalid AHP matrix data.";


            return RedirectToAction(
                nameof(Index),
                new
                {
                    batchId
                });
        }


        // =====================================================
        // BATCH ID FROM ROUTE
        // =====================================================

        model.BatchId =
            batchId;


        // =====================================================
        // CURRENT USER
        // =====================================================

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }
        var evaluatedBy = userId.Value;


        // =====================================================
        // CAPTURE OLD AHP STATE
        // =====================================================

        AHPComparisonViewModel? beforeModel =
            null;


        try
        {
            beforeModel =
                await _ahpService
                    .GetComparisonMatrixAsync(
                        batchId);
        }
        catch
        {
            // Không để snapshot audit
            // ảnh hưởng workflow AHP.
        }


        try
        {
            // =================================================
            // SAVE
            // =================================================

            await _ahpService
                .SaveComparisonsAsync(
                    model,
                    evaluatedBy);


            TempData["SuccessMessage"] =
                "AHP comparison matrix has been saved.";


            // =================================================
            // CAPTURE NEW AHP STATE
            // =================================================

            AHPComparisonViewModel? afterModel =
                null;


            try
            {
                afterModel =
                    await _ahpService
                        .GetComparisonMatrixAsync(
                            batchId);
            }
            catch
            {
                // Audit không được làm hỏng
                // thao tác Save đã thành công.
            }


            // =================================================
            // AUDIT LOG
            // =================================================

            try
            {
                var oldValue =
                    CreateComparisonSnapshot(
                        beforeModel);


                var newValue =
                    CreateComparisonSnapshot(
                        afterModel);


                var comparisonCount =
                    afterModel?.Values
                        .Count(
                            x =>
                                IsNonDiagonalComparison(
                                    x.Key,
                                    afterModel.Criteria))
                    ?? 0;


                var description =
                    $"Save AHP comparison matrix for Batch #{batchId}. " +
                    $"Direct pairwise comparisons: {comparisonCount / 2}.";


                await _auditLogService
                    .LogAsync(
                        evaluatedBy,
                        AuditLogService
                            .Actions
                            .SaveAHP,
                        "AHPPairwiseComparisons",
                        batchId,
                        oldValue,
                        newValue,
                        description);
            }
            catch
            {
                // Audit log không được làm hỏng
                // thao tác AHP đã thành công.
            }
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
                "Lỗi khi lưu ma trận AHP cho đợt {BatchId}.",
                batchId);
            TempData["ErrorMessage"] =
                "An error occurred while saving the AHP matrix. Please try again later.";
        }


        return RedirectToAction(
            nameof(Index),
            new
            {
                batchId
            });
    }


    // =========================================================
    // CALCULATE AHP
    // /EvaluationBatch/{batchId}/AHP/Calculate
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(
        "EvaluationBatch/{batchId:int}/AHP/Calculate")]
    public async Task<IActionResult> Calculate(
        int batchId,
        string? rowVersion = null)
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
        var calculatedBy = userId.Value;


        try
        {
            // =================================================
            // CALCULATE
            // =================================================

            var result =
                await _ahpService
                    .CalculateAsync(
                        batchId,
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
                            result.BatchId,
                            result.CriteriaCount,
                            result.LambdaMax,
                            result.CIValue,
                            result.RIValue,
                            result.CRValue,
                            result.IsConsistent,
                            result.CalculatedAt
                        });


                var description =
                    result.IsConsistent
                        ? $"AHP calculation for Batch #{batchId} completed successfully. " +
                          $"CR = {result.CRValue:0.0000}. " +
                          "Matrix is consistent."
                        : $"AHP calculation for Batch #{batchId} completed, but " +
                          $"matrix is inconsistent. " +
                          $"CR = {result.CRValue:0.0000}.";


                await _auditLogService
                    .LogAsync(
                        calculatedBy,
                        AuditLogService
                            .Actions
                            .CalculateAHP,
                        "AHPResults",
                        batchId,
                        null,
                        newValue,
                        description);
            }
            catch
            {
                // Audit không được làm hỏng
                // thao tác Calculate AHP đã thành công.
            }


            if (result.IsConsistent)
            {
                TempData["SuccessMessage"] =
                    $"AHP calculated successfully. " +
                    $"CR = {result.CRValue:0.0000}. " +
                    "The matrix is consistent.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    $"AHP calculated, but the matrix " +
                    $"is inconsistent. " +
                    $"CR = {result.CRValue:0.0000}. " +
                    "Please review the pairwise comparisons.";
            }
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
                "Lỗi khi tính toán AHP cho đợt {BatchId}.",
                batchId);
            TempData["ErrorMessage"] =
                "An error occurred during AHP calculation. Please try again later.";
        }


        return RedirectToAction(
            nameof(Index),
            new
            {
                batchId
            });
    }


    // =========================================================
    // SNAPSHOT COMPARISON MATRIX
    // =========================================================

    private static string?
        CreateComparisonSnapshot(
            AHPComparisonViewModel? model)
    {
        if (model == null)
        {
            return null;
        }


        var values =
            model.Values
                .OrderBy(
                    x =>
                        x.Key)
                .ToDictionary(
                    x =>
                        x.Key,
                    x =>
                        x.Value);


        return JsonSerializer.Serialize(
            new
            {
                model.BatchId,
                model.BatchCode,
                model.BatchName,
                Criteria =
                    model.Criteria
                        .OrderBy(
                            x =>
                                x.CriteriaCode)
                        .Select(
                            x =>
                                new
                                {
                                    x.CriteriaId,
                                    x.CriteriaCode,
                                    x.CriteriaName
                                })
                        .ToList(),
                Values =
                    values
            });
    }


    // =========================================================
    // NON-DIAGONAL COMPARISON
    // =========================================================

    private static bool
        IsNonDiagonalComparison(
            string key,
            IReadOnlyCollection<
                AHPComparisonCriteriaViewModel>
                criteria)
    {
        var parts =
            key.Split(
                '_',
                StringSplitOptions.RemoveEmptyEntries);


        if (
            parts.Length != 2 ||
            !int.TryParse(
                parts[0],
                out var rowId) ||
            !int.TryParse(
                parts[1],
                out var colId))
        {
            return false;
        }


        return rowId != colId &&
               criteria.Any(
                   x =>
                       x.CriteriaId ==
                       rowId) &&
               criteria.Any(
                   x =>
                       x.CriteriaId ==
                       colId);
    }
}
