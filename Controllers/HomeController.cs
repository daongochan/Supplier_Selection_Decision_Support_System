using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using System.Diagnostics;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly DashboardService _dashboardService;


    public HomeController(
        DashboardService dashboardService)
    {
        _dashboardService =
            dashboardService;
    }


    // =========================================================
    // REDIRECT TO DASHBOARD
    // =========================================================

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Dashboard");
    }


    // =========================================================
    // ERROR
    // =========================================================

    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location =
            ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id
                    ??
                    HttpContext.TraceIdentifier
            });
    }
}