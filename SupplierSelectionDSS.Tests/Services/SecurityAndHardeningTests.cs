using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Services;

public class SecurityAndHardeningTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly string _testRoot;
    private readonly string _appDataReports;
    private readonly string _webRootReports;

    public SecurityAndHardeningTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _testRoot = Path.Combine(Path.GetTempPath(), "DSS_SecurityTests_" + Guid.NewGuid());
        _appDataReports = Path.Combine(_testRoot, "App_Data", "reports");
        _webRootReports = Path.Combine(_testRoot, "wwwroot", "reports");

        Directory.CreateDirectory(_appDataReports);
        Directory.CreateDirectory(_webRootReports);
    }

    public void Dispose()
    {
        _context.Dispose();
        try
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in temp
        }
    }

    private ReportService CreateReportService()
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_testRoot);
        mockEnv.Setup(e => e.WebRootPath).Returns(Path.Combine(_testRoot, "wwwroot"));

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Reports:Directory"]).Returns((string?)null);

        var mockLogger = new Mock<ILogger<ReportService>>();
        var auditLogService = new AuditLogService(_context);

        return new ReportService(_context, mockEnv.Object, auditLogService, mockConfig.Object, mockLogger.Object);
    }

    [Fact]
    public async Task ReportService_GetDownloadAsync_PathTraversal_ReturnsNull()
    {
        // Giả lập bản ghi Report có FilePath trỏ ra ngoài thư mục báo cáo (Directory Traversal)
        var report = new Report
        {
            ReportId = 1,
            BatchId = 1,
            ReportType = "PDF",
            GeneratedBy = 1,
            GeneratedAt = DateTime.UtcNow,
            FilePath = "../../Windows/System32/drivers/etc/hosts"
        };
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        var service = CreateReportService();
        var result = await service.GetDownloadAsync(1);

        // Bắt buộc phải chặn và trả về null
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReportService_GetDownloadAsync_FileNotFound_ReturnsNull()
    {
        var report = new Report
        {
            ReportId = 2,
            BatchId = 1,
            ReportType = "PDF",
            GeneratedBy = 1,
            GeneratedAt = DateTime.UtcNow,
            FilePath = "App_Data/reports/non_existent.pdf"
        };
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        var service = CreateReportService();
        var result = await service.GetDownloadAsync(2);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReportService_GetDownloadAsync_ValidFileInAppData_ReturnsDownloadResult()
    {
        var fileName = "valid_report_123.pdf";
        var diskFilePath = Path.Combine(_appDataReports, fileName);
        await File.WriteAllTextAsync(diskFilePath, "%PDF-1.4 Mock content");

        var report = new Report
        {
            ReportId = 3,
            BatchId = 1,
            ReportType = "PDF",
            GeneratedBy = 1,
            GeneratedAt = DateTime.UtcNow,
            FilePath = $"App_Data/reports/{fileName}"
        };
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        var service = CreateReportService();
        var result = await service.GetDownloadAsync(3);

        result.Should().NotBeNull();
        result!.DownloadName.Should().Be(fileName);
        result.ContentType.Should().Be("application/pdf");
        File.Exists(result.FilePath).Should().BeTrue();
    }

    [Fact]
    public async Task UserService_UpdateAsync_DemoteLastActiveAdmin_ThrowsInvalidOperationException()
    {
        // 1 active admin duy nhất
        var admin = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = "hash",
            FullName = "Administrator",
            Role = UserRole.Administrator,
            Status = EntityStatus.Active
        };
        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        var userService = new UserService(_context);

        var model = new UserFormViewModel
        {
            UserId = 1,
            Username = "admin",
            FullName = "Administrator",
            Role = UserRole.PurchasingStaff, // Cố tình hạ quyền xuống PurchasingStaff
            Status = EntityStatus.Active
        };

        // Khi chỉ còn 1 admin active, hạ quyền phải ném ngoại lệ bảo vệ hệ thống
        var act = async () => await userService.UpdateAsync(model, currentUserId: 1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Administrator đang hoạt động cuối cùng*");
    }

    [Fact]
    public async Task UserService_UpdateAsync_DeactivateLastActiveAdmin_ThrowsInvalidOperationException()
    {
        var admin = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = "hash",
            FullName = "Administrator",
            Role = UserRole.Administrator,
            Status = EntityStatus.Active
        };
        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        var userService = new UserService(_context);

        var model = new UserFormViewModel
        {
            UserId = 1,
            Username = "admin",
            FullName = "Administrator",
            Role = UserRole.Administrator,
            Status = EntityStatus.Inactive // Khóa tài khoản
        };

        var act = async () => await userService.UpdateAsync(model, currentUserId: 99);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Administrator đang hoạt động cuối cùng*");
    }

    [Fact]
    public async Task UserService_UpdateAsync_MultipleAdmins_AllowsRoleChange()
    {
        var admin1 = new User
        {
            UserId = 1,
            Username = "admin1",
            PasswordHash = "hash",
            FullName = "Admin One",
            Role = UserRole.Administrator,
            Status = EntityStatus.Active
        };
        var admin2 = new User
        {
            UserId = 2,
            Username = "admin2",
            PasswordHash = "hash",
            FullName = "Admin Two",
            Role = UserRole.Administrator,
            Status = EntityStatus.Active
        };
        _context.Users.AddRange(admin1, admin2);
        await _context.SaveChangesAsync();

        var userService = new UserService(_context);

        var model = new UserFormViewModel
        {
            UserId = 1,
            Username = "admin1",
            FullName = "Admin One",
            Role = UserRole.Manager, // Đổi sang Manager khi vẫn còn admin2
            Status = EntityStatus.Active
        };

        // Thành công do hệ thống vẫn còn admin2
        await userService.UpdateAsync(model, currentUserId: 2);

        var updated = await _context.Users.FindAsync(1);
        updated!.Role.Should().Be(UserRole.Manager);
    }
}
