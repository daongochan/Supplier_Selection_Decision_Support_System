using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using Xunit;

namespace SupplierSelectionDSS.Tests.Integration;

public class CriteriaWorkflowTests : IClassFixture<CriteriaWorkflowTests.TestFactory>
{
    private readonly TestFactory _factory;

    public CriteriaWorkflowTests(TestFactory factory)
    {
        _factory = factory;
    }

    public class TestFactory : WebApplicationFactory<Program>
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
                    options.UseInMemoryDatabase("CriteriaTestDb_" + Guid.NewGuid());
                });

                services.AddAuthentication("TestCriteriaAuth")
                    .AddScheme<AuthenticationSchemeOptions, TestCriteriaAuthHandler>("TestCriteriaAuth", _ => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "TestCriteriaAuth";
                    options.DefaultChallengeScheme = "TestCriteriaAuth";
                    options.DefaultScheme = "TestCriteriaAuth";
                    options.DefaultSignInScheme = "TestCriteriaAuth";
                });
            });
        }
    }

    public class TestCriteriaAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestCriteriaAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, UserRole.Administrator),
                new Claim(ClaimTypes.GivenName, "System Admin")
            };

            var identity = new ClaimsIdentity(claims, "TestCriteriaAuth");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestCriteriaAuth");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    [Fact]
    public async Task GetCriteriaCreate_WhenAuthenticated_Returns200WithFullFormAndSidebar()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Criteria/Create");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Must contain layout shell, navigation, and form fields
        html.Should().Contain("app-shell");
        html.Should().Contain("app-sidebar");
        html.Should().Contain("form-card");
        html.Should().Contain("CriteriaCode");
        html.Should().Contain("CriteriaName");
        html.Should().Contain("CriteriaType");
        html.Should().Contain("Unit");
        html.Should().Contain("Description");
        html.Should().Contain("Add Evaluation Criteria");
    }

    [Fact]
    public async Task GetCriteriaCreate_IncludesJQueryInValidationScripts()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Criteria/Create");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("jquery.min.js");
        html.Should().Contain("jquery.validate.min.js");
    }
}
