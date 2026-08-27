using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Controllers;

[Authorize(
    Roles =
        UserRole.Administrator + "," + UserRole.PurchasingStaff + "," + UserRole.Expert + "," + UserRole.Manager)]
public class EvaluationController : Controller
{
    private readonly EvaluationService _evaluationService;

    private readonly AuditLogService _auditLogService;


    public EvaluationController(
        EvaluationService evaluationService,
        AuditLogService auditLogService)
    {
        _evaluationService =
            evaluationService;

        _auditLogService =
            auditLogService;
    }


    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        int batchId)
    {
        var model =
            await _evaluationService
                .GetEvaluationAsync(batchId);


        if (model == null)
        {
            return NotFound();
        }


        return View(model);
    }


    // =========================================================
    // SAVE EVALUATION
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            UserRole.Administrator + "," + UserRole.PurchasingStaff + "," + UserRole.Expert)]
    public async Task<IActionResult> Save(
        EvaluationScoreInputViewModel model)
    {
        var userIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);


        if (
            userIdClaim == null ||
            !int.TryParse(
                userIdClaim.Value,
                out var evaluatedBy))
        {
            return Unauthorized();
        }


        if (
            model == null ||
            model.BatchId <= 0)
        {
            return BadRequest();
        }


        // =====================================================
        // CAPTURE OLD EVALUATION
        // =====================================================

        EvaluationViewModel? beforeModel = null;


        try
        {
            beforeModel =
                await _evaluationService
                    .GetEvaluationAsync(
                        model.BatchId);
        }
        catch
        {
            // Không để việc lấy snapshot audit
            // làm hỏng thao tác Save Evaluation.
        }


        try
        {
            // =================================================
            // SAVE
            // =================================================

            await _evaluationService
                .SaveAsync(
                    model,
                    evaluatedBy);


            TempData["SuccessMessage"] =
                "Dữ liệu đánh giá đã được lưu.";


            // =================================================
            // CAPTURE NEW EVALUATION
            // =================================================

            EvaluationViewModel? afterModel = null;


            try
            {
                afterModel =
                    await _evaluationService
                        .GetEvaluationAsync(
                            model.BatchId);
            }
            catch
            {
                // Audit chỉ là lớp bổ sung.
                // Không làm hỏng workflow chính.
            }


            // =================================================
            // AUDIT LOG
            // =================================================

            try
            {
                var oldValue =
                    CreateEvaluationSnapshot(
                        beforeModel);


                var newValue =
                    CreateEvaluationSnapshot(
                        afterModel);


                var changedCellCount =
                    CountChangedCells(
                        beforeModel,
                        afterModel);


                var scoreCellCount =
                    CountScoreCells(
                        afterModel);


                var description =
                    $"Lưu dữ liệu Evaluation cho Batch #{model.BatchId}. " +
                    $"Tổng số ô có giá trị: {scoreCellCount}. " +
                    $"Số ô thay đổi: {changedCellCount}.";


                await _auditLogService
                    .LogAsync(
                        evaluatedBy,
                        AuditLogService
                            .Actions
                            .SaveEvaluation,
                        "EvaluationScores",
                        model.BatchId,
                        oldValue,
                        newValue,
                        description);
            }
            catch
            {
                // Audit log không được làm hỏng
                // thao tác Save Evaluation đã thành công.
            }
        }
        catch (
            InvalidOperationException ex)
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


        return RedirectToAction(
            nameof(Index),
            new
            {
                batchId =
                    model.BatchId
            });
    }


    // =========================================================
    // CREATE EVALUATION SNAPSHOT
    // =========================================================

    private static string?
        CreateEvaluationSnapshot(
            EvaluationViewModel? model)
    {
        if (model == null)
        {
            return null;
        }


        var suppliers =
            model.Suppliers
                .OrderBy(
                    x =>
                        x.SupplierCode)
                .Select(
                    supplier =>
                        new
                        {
                            supplier.SupplierId,
                            supplier.SupplierCode,
                            Scores =
                                model.Criteria
                                    .OrderBy(
                                        x =>
                                            x.CriteriaCode)
                                    .Select(
                                        criterion =>
                                            new
                                            {
                                                criterion.CriteriaId,
                                                criterion.CriteriaCode,
                                                Score =
                                                    supplier.Scores
                                                        .TryGetValue(
                                                            criterion.CriteriaId,
                                                            out var cell)
                                                            ? cell.Score
                                                            : null
                                            })
                                    .ToList()
                        })
                .ToList();


        return JsonSerializer.Serialize(
            new
            {
                model.BatchId,
                Suppliers =
                    suppliers
            });
    }


    // =========================================================
    // COUNT SCORE CELLS
    // =========================================================

    private static int
        CountScoreCells(
            EvaluationViewModel? model)
    {
        if (model == null)
        {
            return 0;
        }


        var count = 0;


        foreach (
            var supplier
            in model.Suppliers)
        {
            foreach (
                var criterion
                in model.Criteria)
            {
                if (
                    supplier.Scores.TryGetValue(
                        criterion.CriteriaId,
                        out var cell)
                    &&
                    cell.Score.HasValue)
                {
                    count++;
                }
            }
        }


        return count;
    }


    // =========================================================
    // COUNT CHANGED CELLS
    // =========================================================

    private static int
        CountChangedCells(
            EvaluationViewModel? beforeModel,
            EvaluationViewModel? afterModel)
    {
        if (
            beforeModel == null &&
            afterModel == null)
        {
            return 0;
        }


        if (beforeModel == null)
        {
            return CountScoreCells(
                afterModel);
        }


        if (afterModel == null)
        {
            return CountScoreCells(
                beforeModel);
        }


        var beforeLookup =
            BuildScoreLookup(
                beforeModel);


        var afterLookup =
            BuildScoreLookup(
                afterModel);


        var allKeys =
            beforeLookup.Keys
                .Union(
                    afterLookup.Keys)
                .ToHashSet();


        var changed = 0;


        foreach (
            var key
            in allKeys)
        {
            beforeLookup.TryGetValue(
                key,
                out var beforeValue);


            afterLookup.TryGetValue(
                key,
                out var afterValue);


            if (beforeValue != afterValue)
            {
                changed++;
            }
        }


        return changed;
    }


    // =========================================================
    // BUILD SCORE LOOKUP
    // =========================================================

    private static Dictionary<
        (int SupplierId, int CriteriaId),
        decimal?>
        BuildScoreLookup(
            EvaluationViewModel model)
    {
        var lookup =
            new Dictionary<
                (int SupplierId, int CriteriaId),
                decimal?>();


        foreach (
            var supplier
            in model.Suppliers)
        {
            foreach (
                var criterion
                in model.Criteria)
            {
                var score =
                    supplier.Scores.TryGetValue(
                        criterion.CriteriaId,
                        out var cell)
                            ? cell.Score
                            : null;


                lookup[
                    (
                        supplier.SupplierId,
                        criterion.CriteriaId
                    )] =
                    score;
            }
        }


        return lookup;
    }
}