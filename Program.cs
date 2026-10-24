using System.IO.Compression;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Infrastructure;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Services;

using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Infrastructure.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// APPLICATION CONFIGURATION
// =========================================================

QuestPDF.Settings.License = LicenseType.Community;

// Performance diagnostics configuration (toggleable via appsettings / env)
builder.Services.Configure<PerformanceDiagnosticsOptions>(
    builder.Configuration.GetSection(PerformanceDiagnosticsOptions.SectionName));
builder.Services.AddSingleton<PerformanceCommandInterceptor>();
builder.Services.AddSingleton<InFlightLockDiagnosticsSampler>();

// Memory cache with size limit for security components (LoginAttemptService, etc.)
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 10_000;
});

builder.Services.AddSingleton<ILoginAttemptService, LoginAttemptService>();

// Rate limiter for authentication endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginLimiter", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// =========================================================
// MVC
// =========================================================

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Response compression for static and dynamic assets (Brotli + Gzip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
        "image/svg+xml",
        "application/json",
        "text/css",
        "application/javascript"
    ]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.AddHealthChecks();

builder.Services.AddAuthorization();

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

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
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

    var diagOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PerformanceDiagnosticsOptions>>().Value;
    if (diagOptions.Enabled)
    {
        options.AddInterceptors(sp.GetRequiredService<PerformanceCommandInterceptor>());
    }
});

// =========================================================
// APPLICATION SERVICES
// =========================================================

builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<ProductGroupService>();
builder.Services.AddScoped<CriteriaService>();

builder.Services.AddScoped<EvaluationBatchService>();
builder.Services.AddScoped<EvaluationService>();
builder.Services.AddScoped<BatchWorkflowReadinessService>();

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
builder.Services.AddScoped<SensitivityAnalysisService>();

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

        options.Events.OnValidatePrincipal = async context =>
        {
            var authSw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var userIdStr = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out var userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var cache = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var cacheKey = $"AuthUserSnapshot_{userId}";

                if (!cache.TryGetValue(cacheKey, out AuthUserValidationSnapshot? liveUser) || liveUser == null)
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    liveUser = await db.Users
                        .AsNoTracking()
                        .Where(x => x.UserId == userId)
                        .Select(x => new AuthUserValidationSnapshot(
                            x.UserId,
                            x.Username,
                            x.FullName,
                            x.Role,
                            x.Status,
                            x.PasswordHash))
                        .FirstOrDefaultAsync();

                    if (liveUser != null)
                    {
                        cache.Set(cacheKey, liveUser, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                            Size = 1
                        });
                    }
                }

                if (liveUser == null || !string.Equals(liveUser.Status, EntityStatus.Active, StringComparison.OrdinalIgnoreCase))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var cookiePwdStamp = context.Principal?.FindFirstValue(AuthClaimTypes.PasswordStamp);
                if (!string.IsNullOrEmpty(cookiePwdStamp))
                {
                    var livePwdStamp = AuthClaimTypes.ComputePasswordStamp(liveUser.PasswordHash);
                    if (!string.Equals(cookiePwdStamp, livePwdStamp, StringComparison.Ordinal))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return;
                    }
                }

                var cookieRole = context.Principal?.FindFirstValue(ClaimTypes.Role);
                var cookieGivenName = context.Principal?.FindFirstValue(ClaimTypes.GivenName);
                if (!string.Equals(cookieRole, liveUser.Role, StringComparison.Ordinal)
                    || !string.Equals(cookieGivenName, liveUser.FullName, StringComparison.Ordinal))
                {
                    var updatedClaims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, liveUser.UserId.ToString()),
                        new(ClaimTypes.Name, liveUser.Username),
                        new(ClaimTypes.GivenName, liveUser.FullName),
                        new(ClaimTypes.Role, liveUser.Role),
                        new(AuthClaimTypes.PasswordStamp, AuthClaimTypes.ComputePasswordStamp(liveUser.PasswordHash))
                    };

                    var updatedIdentity = new ClaimsIdentity(updatedClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                    context.ReplacePrincipal(new ClaimsPrincipal(updatedIdentity));
                    context.ShouldRenew = true;
                }
            }
            finally
            {
                authSw.Stop();
                RequestDiagnosticsContext.Current?.RecordAuthValidation(authSw.Elapsed);
            }
        };

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

app.UseMiddleware<PerformanceDiagnosticsMiddleware>();

// Cấu hình Localization: Hỗ trợ tiếng Việt với dấu phân cách thập phân chuẩn để model binder luôn đọc đúng số
var defaultCulture = new System.Globalization.CultureInfo("vi-VN");
defaultCulture.NumberFormat.NumberDecimalSeparator = ".";
defaultCulture.NumberFormat.NumberGroupSeparator = ",";

var supportedCultures = new[] { defaultCulture, System.Globalization.CultureInfo.InvariantCulture, new System.Globalization.CultureInfo("en-US") };

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Response Compression (Brotli/Gzip)
app.UseResponseCompression();

// Bắt và chuyển hướng toàn bộ các HTTP status code không có body (400-599) sang Error view, chống màn hình trắng
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

// Chỉ áp dụng StatusCodePages cho GET và HEAD để render giao diện chống màn hình trắng.
// Các request POST/PUT/DELETE bị lỗi (như CSRF 400) giữ nguyên response code gốc mà không re-execute view.
app.Use(async (context, next) =>
{
    if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>();
        if (feature != null)
        {
            feature.Enabled = false;
        }
    }
    await next();
});

// Chặn tuyệt đối mọi truy cập trực tiếp tới file báo cáo qua static webroot TRƯỚC UseStaticFiles
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/reports", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Static files requested with ?v= (asp-append-version cache buster)
        if (ctx.Context.Request.Query.ContainsKey("v"))
        {
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
        else
        {
            // Direct assets (fonts, images, etc.) cache for 7 days
            ctx.Context.Response.Headers.CacheControl = "public, max-age=604800";
        }
    }
});

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// =========================================================
// ROUTING & ENDPOINTS
// =========================================================

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Expose Program for WebApplicationFactory integration testing
public partial class Program { }