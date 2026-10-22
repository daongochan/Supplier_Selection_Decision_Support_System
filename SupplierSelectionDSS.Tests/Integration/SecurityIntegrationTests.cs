using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupplierSelectionDSS.Data;
using Xunit;

namespace SupplierSelectionDSS.Tests.Integration;

public class SecurityIntegrationTests : IClassFixture<SecurityIntegrationTests.CustomTestApplicationFactory>
{
    private readonly CustomTestApplicationFactory _factory;

    public SecurityIntegrationTests(CustomTestApplicationFactory factory)
    {
        _factory = factory;
    }

    public class CustomTestApplicationFactory : WebApplicationFactory<Program>
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
                    options.UseInMemoryDatabase("SecurityIntegrationTestDb_" + Guid.NewGuid());
                });
            });
        }
    }

    [Theory]
    [InlineData("/reports/report_batch_1.pdf")]
    [InlineData("/Reports/Test.xlsx")]
    [InlineData("/reports/any/nested/file.pdf")]
    public async Task DirectAccessToReportsDirectory_AlwaysReturns404NotFound(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "mọi truy cập trực tiếp vào /reports đều phải bị chặn ở middleware trước static files");
    }

    [Fact]
    public async Task SecurityHeaders_ArePresentInHttpResponses()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");

        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").First().Should().Be("nosniff");

        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        response.Headers.GetValues("X-Frame-Options").First().Should().Be("SAMEORIGIN");

        response.Headers.Contains("Referrer-Policy").Should().BeTrue();
        response.Headers.GetValues("Referrer-Policy").First().Should().Be("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task UnauthenticatedAccessToReportDownload_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Report/Download/1");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task PostRequest_WithoutAntiforgeryToken_IsRejectedWithBadRequest()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var postContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Username", "admin" },
            { "Password", "WrongPassword123" }
        });

        var response = await client.PostAsync("/Account/Login", postContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "AutoValidateAntiforgeryTokenAttribute toàn cục phải từ chối request POST không có token hợp lệ");
    }

    [Fact]
    public async Task StaticFiles_WithVersionQueryString_HaveImmutableCacheControlHeader()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/css/site.css?v=testversion123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.ToString().Should().Contain("max-age=31536000");
        response.Headers.CacheControl.ToString().Should().Contain("immutable");
    }
}
