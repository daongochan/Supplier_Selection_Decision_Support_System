using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using System.Diagnostics;

namespace SupplierSelectionDSS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly DashboardService _dashboardService;

    public HomeController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Dashboard");
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var code = statusCode ?? HttpContext.Response.StatusCode;
        if (code < 400 || code > 599)
        {
            code = 500;
        }

        HttpContext.Response.StatusCode = code;

        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code
        };

        switch (code)
        {
            case 404:
                model.Title = "Page Not Found (404 Not Found)";
                model.Message = "The page or resource you are looking for does not exist, has been removed, or has been moved.";
                model.Description = "Please check the URL or return to the dashboard to continue.";
                break;
            case 403:
                model.Title = "Access Denied (403 Forbidden)";
                model.Message = "You do not have permission to access this resource or perform this action.";
                model.Description = "Please contact your system administrator if you believe this is an error.";
                break;
            case 400:
                model.Title = "Bad Request (400 Bad Request)";
                model.Message = "The submitted data was malformed or your security session has expired.";
                model.Description = "Please refresh the page or return to the previous form to try again.";
                break;
            case 429:
                model.Title = "Too Many Requests (429 Too Many Requests)";
                model.Message = "Too many requests have been received from your network address in a short time.";
                model.Description = "Please wait a moment before trying again to ensure system stability.";
                break;
            default:
                model.Title = "Internal Server Error (500 Internal Server Error)";
                model.Message = "An unexpected error occurred while processing your request.";
                model.Description = "Our technical team has logged the request identifier for investigation.";
                break;
        }

        return View(model);
    }
}
