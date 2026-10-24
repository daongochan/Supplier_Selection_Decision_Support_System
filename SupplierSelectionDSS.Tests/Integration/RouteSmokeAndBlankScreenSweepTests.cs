using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupplierSelectionDSS.Data;
using Xunit;

namespace SupplierSelectionDSS.Tests.Integration;

public class RouteSmokeAndBlankScreenSweepTests : IClassFixture<RouteSmokeAndBlankScreenSweepTests.BlankScreenTestAppFactory>
{
    private readonly BlankScreenTestAppFactory _factory;

    public RouteSmokeAndBlankScreenSweepTests(BlankScreenTestAppFactory factory)
    {
        _factory = factory;
    }

    public class BlankScreenTestAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("BlankScreenSmokeDb_" + Guid.NewGuid());
                });
            });
        }
    }

    [Theory]
    [InlineData("/NonExistentPage_NotFound_Test")]
    [InlineData("/some/invalid/nested/path/to/resource")]
    public async Task NonExistentPage_Returns404_WithRichHtmlErrorPage_AndNoBlankScreen(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var body = await response.Content.ReadAsStringAsync();
        body.Length.Should().BeGreaterThanOrEqualTo(200, "trang lỗi 404 phải có đầy đủ HTML giao diện, không được rỗng (0 bytes)");
        body.Should().Contain("404");
        body.Should().Contain("main-content");

        var decodedBody = WebUtility.HtmlDecode(body);
        decodedBody.Should().Contain("Page Not Found");
        decodedBody.Should().Contain("Back to Dashboard");
    }

    [Theory]
    [InlineData("/reports/secret.pdf")]
    [InlineData("/reports/financial_audit.xlsx")]
    public async Task DirectAccessToReportsDirectory_Returns404_WithStyledErrorPage(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Length.Should().BeGreaterThanOrEqualTo(200);

        var decodedBody = WebUtility.HtmlDecode(body);
        decodedBody.Should().Contain("Page Not Found");
        decodedBody.Should().Contain("404");
    }

    [Theory]
    [InlineData(404, "Page Not Found")]
    [InlineData(403, "Access Denied")]
    [InlineData(400, "Bad Request")]
    [InlineData(429, "Too Many Requests")]
    [InlineData(500, "Internal Server Error")]
    public async Task ErrorEndpoint_RendersVietnameseDesignSystemCard_ForStatusCodes(int statusCode, string expectedSnippet)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/Home/Error?statusCode={statusCode}");

        ((int)response.StatusCode).Should().Be(statusCode);
        var body = await response.Content.ReadAsStringAsync();
        body.Length.Should().BeGreaterThanOrEqualTo(200);

        var decodedBody = WebUtility.HtmlDecode(body);
        decodedBody.Should().Contain(expectedSnippet);
        decodedBody.Should().Contain("STATUS CODE: " + statusCode);
        decodedBody.Should().Contain("Back to Dashboard");
    }
}
