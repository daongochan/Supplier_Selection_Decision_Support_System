using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Services;

namespace SupplierSelectionDSS.Tools.DemoSeeder;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("================================================================================");
        Console.WriteLine("  SUPPLIER SELECTION DECISION SUPPORT SYSTEM - DEMO DATASET SEEDER & VERIFIER  ");
        Console.WriteLine("================================================================================");

        var mode = "help";
        string? customConnStr = null;
        DateTime? customAsOfDate = null;
        string? customPassword = null;
        var dryRun = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--seed", StringComparison.OrdinalIgnoreCase))
            {
                mode = "seed";
            }
            else if (arg.Equals("--verify", StringComparison.OrdinalIgnoreCase))
            {
                mode = "verify";
            }
            else if (arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                dryRun = true;
            }
            else if (arg.Equals("--connection-string", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                customConnStr = args[++i];
            }
            else if (arg.Equals("--as-of-date", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (DateTime.TryParse(args[++i], out var parsedDate))
                {
                    customAsOfDate = parsedDate;
                }
                else
                {
                    Console.Error.WriteLine($"[Error] Invalid date format for --as-of-date: {args[i]}. Use yyyy-MM-dd.");
                    return 1;
                }
            }
            else if (arg.Equals("--password", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                customPassword = args[++i];
            }
            else if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                PrintUsage();
                return 0;
            }
        }

        if (mode == "help")
        {
            PrintUsage();
            return 0;
        }

        // Resolve Connection String
        var connStr = customConnStr
            ?? Environment.GetEnvironmentVariable("DSS_DEMO_CONNECTION_STRING")
            ?? "Server=.;Database=SupplierSelectionDSS_Demo;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        // CRITICAL SAFETY CHECK BEFORE ANY ACTION
        try
        {
            DatabaseSafetyGuard.AssertSafeDemoConnection(connStr);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"\n[SAFETY ABORT] {ex.Message}\n");
            Console.ResetColor();
            return 2;
        }

        var asOfDate = customAsOfDate ?? DateTime.UtcNow.Date;
        var rawPassword = customPassword ?? Environment.GetEnvironmentVariable("DEMO_PASSWORD") ?? "Demo@123456";

        // Resolve Content Root Path (Find root containing SupplierSelectionDSS.csproj)
        var repoRoot = FindRepoRoot();
        if (repoRoot == null)
        {
            Console.Error.WriteLine("[Error] Could not locate repository root directory.");
            return 1;
        }

        // Initialize QuestPDF Community License
        QuestPDF.Settings.License = LicenseType.Community;

        // Build DI Container
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connStr, sqlOptions =>
            {
                sqlOptions.CommandTimeout(60);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        // Register Application Services
        services.AddScoped<AHPCalculationService>();
        services.AddScoped<AHPService>();
        services.AddScoped<TOPSISCalculationService>();
        services.AddScoped<SensitivityAnalysisService>();
        services.AddScoped<TOPSISService>();
        services.AddScoped<DecisionResultService>();
        services.AddScoped<BatchWorkflowReadinessService>();
        services.AddScoped<AuditLogService>();

        // Environment & Configuration for Reports
        services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(new DemoHostEnvironment(repoRoot));
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Reports:Directory", Path.Combine(repoRoot, "App_Data", "reports") }
        });
        services.AddSingleton<IConfiguration>(configBuilder.Build());
        services.AddScoped<ReportService>();

        // Register Seeder & Verifier
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<DemoDataVerifier>(sp => new DemoDataVerifier(sp, repoRoot));

        var serviceProvider = services.BuildServiceProvider();

        if (mode == "seed")
        {
            try
            {
                var seeder = serviceProvider.GetRequiredService<DemoDataSeeder>();
                await seeder.SeedAsync(asOfDate, rawPassword, dryRun);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[SUCCESS] Demo dataset has been seeded and initialized successfully.");
                Console.ResetColor();

                if (!dryRun)
                {
                    Console.WriteLine("\nRunning post-seed verification checks automatically...");
                    var verifier = serviceProvider.GetRequiredService<DemoDataVerifier>();
                    var vResult = await verifier.VerifyAsync(rawPassword);
                    PrintVerificationResult(vResult);
                    return vResult.Success ? 0 : 3;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"\n[ERROR] Seeding failed: {ex.Message}");
                var cur = ex.InnerException;
                while (cur != null)
                {
                    Console.Error.WriteLine($"  -> Inner: {cur.Message}");
                    cur = cur.InnerException;
                }
                Console.Error.WriteLine(ex.StackTrace);
                Console.ResetColor();
                return 1;
            }
        }
        else if (mode == "verify")
        {
            try
            {
                var verifier = serviceProvider.GetRequiredService<DemoDataVerifier>();
                var vResult = await verifier.VerifyAsync(rawPassword);
                PrintVerificationResult(vResult);
                return vResult.Success ? 0 : 3;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"\n[ERROR] Verification failed: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                Console.ResetColor();
                return 1;
            }
        }

        return 0;
    }

    private static void PrintVerificationResult(DemoVerificationResult result)
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine("  VERIFICATION AUDIT RESULTS  ");
        Console.WriteLine("--------------------------------------------------------------------------------");

        foreach (var check in result.ChecksPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  [PASS] ");
            Console.ResetColor();
            Console.WriteLine(check);
        }

        if (result.Violations.Count > 0)
        {
            Console.WriteLine();
            foreach (var violation in result.Violations)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine(violation);
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nFAILED: Found {result.Violations.Count} verification violation(s).");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nSUCCESS: All {result.ChecksPassed.Count} verification checks passed with 100% compliance!");
            Console.ResetColor();
        }
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "SupplierSelectionDSS.csproj")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }
        // Fallback to current working directory
        var cwd = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(cwd, "SupplierSelectionDSS.csproj")))
        {
            return cwd;
        }
        return null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tools/demo/DemoSeeder/DemoSeeder.csproj -- [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --seed                     Populate complete demonstration dataset into SupplierSelectionDSS_Demo.");
        Console.WriteLine("  --verify                   Run deep verification checks on demo entities, AHP/TOPSIS math, and reports.");
        Console.WriteLine("  --dry-run                  Simulate seeding without committing changes.");
        Console.WriteLine("  --connection-string <cs>   Target SQL Server connection string (MUST target SupplierSelectionDSS_Demo).");
        Console.WriteLine("  --as-of-date <yyyy-MM-dd>  Anchor date for historical evaluation batch timelines (defaults to today).");
        Console.WriteLine("  --password <pwd>           Default password for all demo accounts (defaults to Demo@123456).");
        Console.WriteLine("  --help, -h                 Display this help reference.");
        Console.WriteLine();
        Console.WriteLine("Safety Guard:");
        Console.WriteLine("  Refuses to execute if the target database is not 'SupplierSelectionDSS_Demo'.");
        Console.WriteLine("  Under no circumstances will operational or performance databases be mutated.");
    }
}
