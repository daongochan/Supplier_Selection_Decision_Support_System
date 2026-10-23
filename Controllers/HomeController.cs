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
                model.Title = "Không tìm thấy trang (404 Not Found)";
                model.Message = "Trang hoặc tài nguyên bạn đang tìm kiếm không tồn tại, đã bị xóa hoặc đã được di chuyển.";
                model.Description = "Vui lòng kiểm tra lại đường dẫn URL hoặc quay về bảng điều khiển để tiếp tục làm việc.";
                break;
            case 403:
                model.Title = "Truy cập bị từ chối (403 Forbidden)";
                model.Message = "Bạn không có quyền hạn để truy cập vào tài nguyên hoặc thực hiện hành động này.";
                model.Description = "Vui lòng liên hệ Quản trị viên hệ thống nếu bạn cho rằng đây là một sự nhầm lẫn.";
                break;
            case 400:
                model.Title = "Yêu cầu không hợp lệ (400 Bad Request)";
                model.Message = "Dữ liệu gửi lên không đúng định dạng hoặc phiên làm việc bảo mật đã hết hạn.";
                model.Description = "Vui lòng làm mới lại trang hoặc quay lại biểu mẫu trước đó để thực hiện lại.";
                break;
            case 429:
                model.Title = "Quá nhiều yêu cầu (429 Too Many Requests)";
                model.Message = "Hệ thống phát hiện tần suất gửi yêu cầu quá nhanh từ địa chỉ mạng của bạn.";
                model.Description = "Vui lòng tạm dừng và thử lại sau ít phút để bảo vệ an toàn cho hệ thống.";
                break;
            default:
                model.Title = "Lỗi máy chủ nội bộ (500 Internal Server Error)";
                model.Message = "Hệ thống gặp sự cố ngoài dự kiến trong khi xử lý yêu cầu.";
                model.Description = "Đội ngũ kỹ thuật đã ghi nhận mã yêu cầu để kiểm tra và khắc phục.";
                break;
        }

        return View(model);
    }
}
