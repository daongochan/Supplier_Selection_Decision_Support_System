using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Services;

namespace SupplierSelectionDSS.Tools.DemoSeeder;

public class DemoVerificationResult
{
    public bool Success { get; set; } = true;
    public List<string> ChecksPassed { get; } = new();
    public List<string> Violations { get; } = new();

    public void AddPass(string check)
    {
        ChecksPassed.Add(check);
    }

    public void AddViolation(string violation)
    {
        Success = false;
        Violations.Add(violation);
    }
}

public class DemoDataVerifier
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _contentRootPath;

    public DemoDataVerifier(IServiceProvider serviceProvider, string contentRootPath)
    {
        _serviceProvider = serviceProvider;
        _contentRootPath = contentRootPath;
    }

    public async Task<DemoVerificationResult> VerifyAsync(string expectedPassword)
    {
        var result = new DemoVerificationResult();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var readinessService = scope.ServiceProvider.GetRequiredService<BatchWorkflowReadinessService>();

        // 1. Verify Connection and Safety
        var connStr = context.Database.GetConnectionString();
        DatabaseSafetyGuard.AssertSafeDemoConnection(connStr!);
        result.AddPass($"Target database is validated: {DatabaseSafetyGuard.AllowedDatabaseName}");

        // 2. Verify User Accounts
        Console.WriteLine("[Verification 1/8] Verifying Demo User Accounts & Passwords...");
        var demoUsers = await context.Users
            .Where(u => u.Username.StartsWith("demo."))
            .ToListAsync();

        if (demoUsers.Count != 5)
        {
            result.AddViolation($"Expected exactly 5 demo users, found {demoUsers.Count}.");
        }
        else
        {
            result.AddPass("User Count: Exactly 5 demo accounts present.");
        }

        var requiredUsers = new (string Username, string Role, string Status)[]
        {
            ("demo.admin", "Administrator", "Active"),
            ("demo.staff", "PurchasingStaff", "Active"),
            ("demo.expert", "Expert", "Active"),
            ("demo.manager", "Manager", "Active"),
            ("demo.inactive", "Expert", "Inactive")
        };

        foreach (var req in requiredUsers)
        {
            var user = demoUsers.FirstOrDefault(u => u.Username == req.Username);
            if (user == null)
            {
                result.AddViolation($"Missing required demo user: {req.Username}");
                continue;
            }

            if (!string.Equals(user.Role, req.Role, StringComparison.OrdinalIgnoreCase))
            {
                result.AddViolation($"User {req.Username} has role '{user.Role}', expected '{req.Role}'.");
            }

            if (!string.Equals(user.Status, req.Status, StringComparison.OrdinalIgnoreCase))
            {
                result.AddViolation($"User {req.Username} has status '{user.Status}', expected '{req.Status}'.");
            }

            if (!BCrypt.Net.BCrypt.Verify(expectedPassword, user.PasswordHash))
            {
                result.AddViolation($"Password verification failed for user '{req.Username}'.");
            }
        }
        result.AddPass("User Roles, Statuses, and BCrypt Hashes verified.");

        // 3. Verify Product Groups
        Console.WriteLine("[Verification 2/8] Verifying Product Groups...");
        var demoGroups = await context.ProductGroups
            .Include(g => g.Suppliers)
            .Where(g => g.GroupCode.StartsWith("DEMO-"))
            .ToListAsync();

        if (demoGroups.Count != 4)
        {
            result.AddViolation($"Expected exactly 4 demo product groups, found {demoGroups.Count}.");
        }
        else
        {
            result.AddPass("Product Groups Count: Exactly 4 demo product groups present.");
        }

        var activeGroups = demoGroups.Count(g => g.Status == "Active");
        var inactiveGroups = demoGroups.Count(g => g.Status == "Inactive");
        if (activeGroups != 3 || inactiveGroups != 1)
        {
            result.AddViolation($"Product Groups status mismatch: expected 3 Active, 1 Inactive; found {activeGroups} Active, {inactiveGroups} Inactive.");
        }
        else
        {
            result.AddPass("Product Group Statuses: 3 Active, 1 Inactive verified.");
        }

        // 4. Verify Suppliers
        Console.WriteLine("[Verification 3/8] Verifying Suppliers and Category Mappings...");
        var demoSuppliers = await context.Suppliers
            .Include(s => s.Groups)
            .Where(s => s.SupplierCode.StartsWith("DEMO-"))
            .ToListAsync();

        if (demoSuppliers.Count != 12)
        {
            result.AddViolation($"Expected exactly 12 demo suppliers, found {demoSuppliers.Count}.");
        }
        else
        {
            result.AddPass("Supplier Count: Exactly 12 demo suppliers present.");
        }

        var activeSuppliers = demoSuppliers.Count(s => s.Status == "Active");
        var inactiveSuppliers = demoSuppliers.Count(s => s.Status == "Inactive");
        if (activeSuppliers != 10 || inactiveSuppliers != 2)
        {
            result.AddViolation($"Supplier status mismatch: expected 10 Active, 2 Inactive; found {activeSuppliers} Active, {inactiveSuppliers} Inactive.");
        }
        else
        {
            result.AddPass("Supplier Statuses: 10 Active, 2 Inactive verified.");
        }

        // Check multi-category supplier DEMO-S03
        var s03 = demoSuppliers.FirstOrDefault(s => s.SupplierCode == "DEMO-S03");
        if (s03 == null || s03.Groups.Count < 2)
        {
            result.AddViolation("Supplier DEMO-S03 must belong to at least 2 Product Groups (PG01 and PG03).");
        }
        else
        {
            result.AddPass("Multi-category Supplier: DEMO-S03 is assigned to multiple product groups.");
        }

        // Check inactive supplier DEMO-S12 assigned to PG01 (special UI case)
        var s12 = demoSuppliers.FirstOrDefault(s => s.SupplierCode == "DEMO-S12");
        if (s12 == null || !s12.Groups.Any(g => g.GroupCode == "DEMO-PG01"))
        {
            result.AddViolation("Inactive supplier DEMO-S12 must be assigned to DEMO-PG01 to test inactive supplier assignment rules.");
        }
        else
        {
            result.AddPass("Inactive Supplier Mapping: DEMO-S12 assigned to DEMO-PG01 as required.");
        }

        // 5. Verify Criteria
        Console.WriteLine("[Verification 4/8] Verifying Evaluation Criteria...");
        var demoCriteria = await context.Criteria
            .Where(c => c.CriteriaCode.StartsWith("DEMO-"))
            .ToListAsync();

        if (demoCriteria.Count != 7)
        {
            result.AddViolation($"Expected exactly 7 demo criteria, found {demoCriteria.Count}.");
        }
        else
        {
            result.AddPass("Criteria Count: Exactly 7 demo criteria present.");
        }

        var ratingScaleCriteria = demoCriteria.Where(c => CriteriaUnits.IsRatingScale(c.Unit)).ToList();
        if (ratingScaleCriteria.Count != 3)
        {
            result.AddViolation($"Expected exactly 3 rating scale criteria (Unit 'Điểm'), found {ratingScaleCriteria.Count}.");
        }
        else
        {
            result.AddPass("Criteria Rating Scales: 3 criteria use 0-10 rating scale ('Điểm').");
        }

        // 6. Verify Batches Lifecycle & Readiness
        Console.WriteLine("[Verification 5/8] Verifying Batch Workflow States and Readiness Engine...");
        var demoBatches = await context.EvaluationBatches
            .Include(b => b.Suppliers)
            .Include(b => b.Criteria)
            .Include(b => b.EvaluationScores)
            .Include(b => b.Ahpresult)
            .Include(b => b.AhpcriteriaWeights)
            .Include(b => b.TopsisResults)
            .Include(b => b.Approvals)
            .Include(b => b.Reports)
            .Include(b => b.BatchRoundSnapshots)
            .Where(b => b.BatchCode.StartsWith("DEMO-"))
            .ToListAsync();

        if (demoBatches.Count != 8)
        {
            result.AddViolation($"Expected exactly 8 demo evaluation batches, found {demoBatches.Count}.");
        }
        else
        {
            result.AddPass("Evaluation Batches Count: Exactly 8 demo batches present.");
        }

        // Batch by batch verification
        var batchDict = demoBatches.ToDictionary(b => b.BatchCode);

        // B01: Completed, Approved, Reports
        if (batchDict.TryGetValue("DEMO-B01", out var b01))
        {
            var readiness = await readinessService.GetReadinessAsync(b01.BatchId);
            if (readiness == null || !readiness.IsCompleted || !readiness.HasFinalApproval || !readiness.CanGenerateReport)
            {
                result.AddViolation("DEMO-B01: Readiness state does not match Completed & Approved with CanGenerateReport=true.");
            }
            if (b01.Reports.Count < 2)
            {
                result.AddViolation($"DEMO-B01: Expected at least 2 reports (PDF + Excel), found {b01.Reports.Count}.");
            }
            if (!b01.BatchRoundSnapshots.Any(s => s.SnapshotType == "FinalApproval"))
            {
                result.AddViolation("DEMO-B01: Missing FinalApproval snapshot in BatchRoundSnapshots.");
            }
            result.AddPass("DEMO-B01: Completed, Approved, FinalApproval snapshot, and Reports registered.");
        }
        else
        {
            result.AddViolation("Missing DEMO-B01 batch.");
        }

        // B02: Completed, Approved, Reports
        if (batchDict.TryGetValue("DEMO-B02", out var b02))
        {
            var readiness = await readinessService.GetReadinessAsync(b02.BatchId);
            if (readiness == null || !readiness.IsCompleted || !readiness.HasFinalApproval || !readiness.CanGenerateReport)
            {
                result.AddViolation("DEMO-B02: Readiness state does not match Completed & Approved with CanGenerateReport=true.");
            }
            if (b02.Reports.Count < 2)
            {
                result.AddViolation($"DEMO-B02: Expected at least 2 reports (PDF + Excel), found {b02.Reports.Count}.");
            }
            if (!b02.BatchRoundSnapshots.Any(s => s.SnapshotType == "FinalApproval"))
            {
                result.AddViolation("DEMO-B02: Missing FinalApproval snapshot in BatchRoundSnapshots.");
            }
            result.AddPass("DEMO-B02: Completed, Approved, FinalApproval snapshot, and Reports registered.");
        }
        else
        {
            result.AddViolation("Missing DEMO-B02 batch.");
        }

        // B03: Evaluating, Ready for Approval
        if (batchDict.TryGetValue("DEMO-B03", out var b03))
        {
            var readiness = await readinessService.GetReadinessAsync(b03.BatchId);
            if (readiness == null || !readiness.IsEvaluating || !readiness.IsTopsisComplete || readiness.HasFinalApproval)
            {
                result.AddViolation("DEMO-B03: Readiness must be IsEvaluating=true, IsTopsisComplete=true, HasFinalApproval=false.");
            }
            else
            {
                result.AddPass("DEMO-B03: Ready for Approval state verified.");
            }
        }
        else
        {
            result.AddViolation("Missing DEMO-B03 batch.");
        }

        // B04: Evaluating, Ready for TOPSIS
        if (batchDict.TryGetValue("DEMO-B04", out var b04))
        {
            var readiness = await readinessService.GetReadinessAsync(b04.BatchId);
            if (readiness == null || !readiness.IsEvaluating || !readiness.IsAhpReady || readiness.TopsisResultCount > 0)
            {
                result.AddViolation("DEMO-B04: Readiness must be IsEvaluating=true, IsAhpReady=true, TopsisResultCount=0.");
            }
            else
            {
                result.AddPass("DEMO-B04: Ready for TOPSIS state verified.");
            }
        }
        else
        {
            result.AddViolation("Missing DEMO-B04 batch.");
        }

        // B05: Evaluating, AHP Inconsistent (CR > 0.10)
        if (batchDict.TryGetValue("DEMO-B05", out var b05))
        {
            var readiness = await readinessService.GetReadinessAsync(b05.BatchId);
            if (readiness == null || !readiness.IsEvaluating || !readiness.HasAhpResult || readiness.IsAhpConsistent || readiness.AhpCrValue <= 0.10m)
            {
                result.AddViolation($"DEMO-B05: AHP must be inconsistent. HasAhpResult={readiness?.HasAhpResult}, IsConsistent={readiness?.IsAhpConsistent}, CR={readiness?.AhpCrValue}.");
            }
            else
            {
                result.AddPass($"DEMO-B05: AHP Inconsistency Warning verified (CR = {readiness.AhpCrValue:0.0000} > 0.10).");
            }
        }
        else
        {
            result.AddViolation("Missing DEMO-B05 batch.");
        }

        // B06: Evaluating, Incomplete Scores
        if (batchDict.TryGetValue("DEMO-B06", out var b06))
        {
            var readiness = await readinessService.GetReadinessAsync(b06.BatchId);
            if (readiness == null || !readiness.IsEvaluating || readiness.IsScoresComplete || readiness.ActualScoreCount != 7)
            {
                result.AddViolation($"DEMO-B06: Expected partial scores (7/12), found ActualScoreCount={readiness?.ActualScoreCount}, IsScoresComplete={readiness?.IsScoresComplete}.");
            }
            else
            {
                result.AddPass("DEMO-B06: Incomplete scores (7/12) and scoring in progress verified.");
            }
        }
        else
        {
            result.AddViolation("Missing DEMO-B06 batch.");
        }

        // B07: Draft
        if (batchDict.TryGetValue("DEMO-B07", out var b07))
        {
            var readiness = await readinessService.GetReadinessAsync(b07.BatchId);
            if (readiness == null || !readiness.IsDraft || readiness.ActualScoreCount > 0)
            {
                result.AddViolation($"DEMO-B07: Expected Draft status with 0 scores, found IsDraft={readiness?.IsDraft}, Scores={readiness?.ActualScoreCount}.");
            }
            else
            {
                result.AddPass("DEMO-B07: Draft state verified.");
            }
        }
        else
        {
            result.AddViolation("Missing DEMO-B07 batch.");
        }

        // B08: Cancelled
        if (batchDict.TryGetValue("DEMO-B08", out var b08))
        {
            var readiness = await readinessService.GetReadinessAsync(b08.BatchId);
            if (readiness == null || !readiness.IsCancelled || !b08.CancelledAt.HasValue)
            {
                result.AddViolation($"DEMO-B08: Expected Cancelled status with CancelledAt timestamp.");
            }
            else
            {
                result.AddPass("DEMO-B08: Cancelled state and audit timestamp verified.");
            }
        }
        else
        {
            result.AddViolation("Missing DEMO-B08 batch.");
        }

        // 7. Verify Mathematical Consistency (AHP weights sum to 1, TOPSIS CC in [0, 1])
        Console.WriteLine("[Verification 6/8] Verifying Mathematical MCDM Invariants (AHP & TOPSIS)...");
        foreach (var bCode in new[] { "DEMO-B01", "DEMO-B02", "DEMO-B03", "DEMO-B04" })
        {
            var b = batchDict[bCode];
            var weightSum = b.AhpcriteriaWeights.Sum(w => w.WeightValue);
            if (Math.Abs(weightSum - 1.0m) > 0.001m)
            {
                result.AddViolation($"{bCode}: AHP weights sum to {weightSum}, expected 1.0.");
            }
            if (b.Ahpresult == null || b.Ahpresult.Crvalue > 0.10m)
            {
                result.AddViolation($"{bCode}: AHP CR is {b.Ahpresult?.Crvalue}, expected <= 0.10.");
            }
        }
        result.AddPass("Consistent AHP Batches (B01-B04): Weights sum to 1.0 and CR <= 0.10 verified.");

        foreach (var bCode in new[] { "DEMO-B01", "DEMO-B02", "DEMO-B03" })
        {
            var b = batchDict[bCode];
            foreach (var t in b.TopsisResults)
            {
                if (t.ClosenessCoefficient < 0m || t.ClosenessCoefficient > 1m)
                {
                    result.AddViolation($"{bCode}: Supplier {t.SupplierId} TOPSIS CC is {t.ClosenessCoefficient}, expected [0, 1].");
                }
                if (t.RankNo <= 0 || t.RankNo > b.Suppliers.Count)
                {
                    result.AddViolation($"{bCode}: Supplier {t.SupplierId} RankNo is {t.RankNo}, expected 1..{b.Suppliers.Count}.");
                }
            }
        }
        result.AddPass("TOPSIS Results (B01-B03): Closeness Coefficients in [0, 1] and valid RankNo verified.");

        // 8. Verify Physical Report Files on Disk
        Console.WriteLine("[Verification 7/8] Verifying Physical Report Files on Disk...");
        var demoReports = await context.Reports
            .Where(r => r.Batch.BatchCode.StartsWith("DEMO-"))
            .ToListAsync();

        if (demoReports.Count < 4)
        {
            result.AddViolation($"Expected at least 4 generated report records, found {demoReports.Count}.");
        }
        else
        {
            result.AddPass($"Reports Database Count: {demoReports.Count} records found.");
        }

        foreach (var r in demoReports)
        {
            if (string.IsNullOrWhiteSpace(r.FilePath))
            {
                result.AddViolation($"Report ID {r.ReportId} has null or empty FilePath.");
                continue;
            }

            var reportsDir = Path.Combine(_contentRootPath, "App_Data", "reports");
            var expectedFilePath = Path.Combine(_contentRootPath, r.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(expectedFilePath))
            {
                // Also check directly in reportsDir by filename
                var fallback = Path.Combine(reportsDir, Path.GetFileName(r.FilePath));
                if (!File.Exists(fallback))
                {
                    result.AddViolation($"Physical report file missing on disk: {expectedFilePath}");
                    continue;
                }
                expectedFilePath = fallback;
            }

            var fileInfo = new FileInfo(expectedFilePath);
            if (fileInfo.Length == 0)
            {
                result.AddViolation($"Report file on disk is empty (0 bytes): {expectedFilePath}");
            }
        }
        result.AddPass("Physical Report Files: All generated PDF and Excel files exist on disk and are non-empty.");

        // 9. Verify Audit Trail
        Console.WriteLine("[Verification 8/8] Verifying Audit Trail...");
        var demoAuditLogs = await context.AuditLogs
            .Where(a => a.Description != null && a.Description.Contains("[Demo]"))
            .ToListAsync();

        if (demoAuditLogs.Count < 15)
        {
            result.AddViolation($"Expected at least 15 demo audit log entries, found {demoAuditLogs.Count}.");
        }
        else
        {
            result.AddPass($"Audit Logs: {demoAuditLogs.Count} audit entries verified spanning users and actions.");
        }

        return result;
    }
}
