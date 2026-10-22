using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Services;

using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Web.Authorization;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// APPLICATION CONFIGURATION
// =========================================================

QuestPDF.Settings.License = LicenseType.Community;

// =========================================================
// MVC
// =========================================================

builder.Services.AddControllersWithViews();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.ManageUsers, policy =>
        policy.RequireRole(UserRole.Administrator));

    options.AddPolicy(PolicyNames.ManageBatches, policy =>
        policy.RequireRole(UserRole.Administrator, UserRole.PurchasingStaff, UserRole.Manager));

    options.AddPolicy(PolicyNames.EvaluateSuppliers, policy =>
        policy.RequireRole(UserRole.Administrator, UserRole.PurchasingStaff, UserRole.Expert));

    options.AddPolicy(PolicyNames.PerformAHP, policy =>
        policy.RequireRole(UserRole.Administrator, UserRole.Expert));

    options.AddPolicy(PolicyNames.PerformTOPSIS, policy =>
        policy.RequireRole(UserRole.Administrator, UserRole.Expert));

    options.AddPolicy(PolicyNames.ApproveDecision, policy =>
        policy.RequireRole(UserRole.Administrator, UserRole.Manager));

    options.AddPolicy(PolicyNames.ViewReports, policy =>
        policy.RequireRole(UserRole.Administrator, UserRole.Manager));

    options.AddPolicy(PolicyNames.ExportReports, policy =>
        policy.RequireRole(UserRole.Administrator, UserRole.Manager));
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(30);
    options.IncludeSubDomains = true;
    options.Preload = false;
});

// =========================================================
// DATABASE
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found or is empty.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

// =========================================================
// APPLICATION SERVICES
// =========================================================

builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<ProductGroupService>();
builder.Services.AddScoped<CriteriaService>();

builder.Services.AddScoped<EvaluationBatchService>();
builder.Services.AddScoped<EvaluationService>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuditLogService>();

builder.Services.AddScoped<DashboardService>();

// =========================================================
// AHP
// =========================================================

builder.Services.AddScoped<AHPCalculationService>();
builder.Services.AddScoped<AHPService>();

// =========================================================
// TOPSIS
// =========================================================

builder.Services.AddScoped<TOPSISCalculationService>();
builder.Services.AddScoped<TOPSISService>();

// =========================================================
// DECISION & REPORTING
// =========================================================

builder.Services.AddScoped<DecisionResultService>();
builder.Services.AddScoped<ReportService>();

// =========================================================
// AUTHENTICATION
// =========================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultSignInScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "SupplierSelectionDSS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();

// =========================================================
// HTTP PIPELINE
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// =========================================================
// ROUTING
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();