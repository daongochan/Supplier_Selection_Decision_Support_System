using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Controllers;

[Authorize(Roles = UserRole.Administrator + "," + UserRole.PurchasingStaff)]
public class CriteriaController : Controller
{
    private readonly CriteriaService _criteriaService;

    public CriteriaController(CriteriaService criteriaService)
    {
        _criteriaService = criteriaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword,
        string type = "All",
        string status = "All")
    {
        var model = new CriteriaIndexViewModel
        {
            Keyword = keyword,
            Type = type,
            Status = status,
            Criteria = await _criteriaService.SearchAsync(keyword, type, status)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CriteriaFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CriteriaFormViewModel model)
    {
        Normalize(model);

        ValidateCriteriaType(model);

        if (await _criteriaService.CriteriaCodeExistsAsync(model.CriteriaCode))
        {
            ModelState.AddModelError(
                nameof(model.CriteriaCode),
                "Mã tiêu chí đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var criterion = new Criterion
        {
            CriteriaCode = model.CriteriaCode,
            CriteriaName = model.CriteriaName,
            Description = model.Description,
            CriteriaType = model.CriteriaType,
            Unit = model.Unit
        };

        try
        {
            await _criteriaService.CreateAsync(criterion);

            TempData["SuccessMessage"] =
                "Tiêu chí đã được tạo thành công.";

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.CriteriaCode), ex.Message);
            return View(model);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var criterion = await _criteriaService.GetByIdAsync(id);

        if (criterion == null)
        {
            return NotFound();
        }

        var model = new CriteriaFormViewModel
        {
            CriteriaId = criterion.CriteriaId,
            CriteriaCode = criterion.CriteriaCode,
            CriteriaName = criterion.CriteriaName,
            Description = criterion.Description,
            CriteriaType = criterion.CriteriaType,
            Unit = criterion.Unit
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CriteriaFormViewModel model)
    {
        Normalize(model);

        ValidateCriteriaType(model);

        if (await _criteriaService.CriteriaCodeExistsAsync(
                model.CriteriaCode,
                model.CriteriaId))
        {
            ModelState.AddModelError(
                nameof(model.CriteriaCode),
                "Mã tiêu chí đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var criterion = await _criteriaService.GetByIdAsync(model.CriteriaId);

        if (criterion == null)
        {
            return NotFound();
        }

        criterion.CriteriaCode = model.CriteriaCode;
        criterion.CriteriaName = model.CriteriaName;
        criterion.Description = model.Description;
        criterion.CriteriaType = model.CriteriaType;
        criterion.Unit = model.Unit;

        try
        {
            await _criteriaService.UpdateAsync(criterion);

            TempData["SuccessMessage"] =
                "Tiêu chí đã được cập nhật.";

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var result = await _criteriaService.ToggleStatusAsync(id);

            if (!result)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] =
                "Trạng thái tiêu chí đã được cập nhật.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private static void Normalize(CriteriaFormViewModel model)
    {
        model.CriteriaCode = model.CriteriaCode?.Trim() ?? string.Empty;
        model.CriteriaName = model.CriteriaName?.Trim() ?? string.Empty;
        model.Description = model.Description?.Trim();
        model.Unit = model.Unit?.Trim();
        model.CriteriaType = model.CriteriaType?.Trim() ?? string.Empty;
    }

    private void ValidateCriteriaType(CriteriaFormViewModel model)
    {
        if (!string.Equals(model.CriteriaType, CriteriaType.Benefit, StringComparison.Ordinal) &&
            !string.Equals(model.CriteriaType, CriteriaType.Cost, StringComparison.Ordinal))
        {
            ModelState.AddModelError(
                nameof(model.CriteriaType),
                "Loại tiêu chí không hợp lệ. Chỉ chấp nhận Benefit hoặc Cost.");
        }
    }
}