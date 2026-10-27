using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Services;

namespace SupplierSelectionDSS.Tools.DemoSeeder;

public class DemoDataSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(IServiceProvider serviceProvider, ILogger<DemoDataSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync(DateTime asOfDate, string rawPassword, bool dryRun = false)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ahpService = scope.ServiceProvider.GetRequiredService<AHPService>();
        var topsisService = scope.ServiceProvider.GetRequiredService<TOPSISService>();
        var decisionService = scope.ServiceProvider.GetRequiredService<DecisionResultService>();
        var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();

        // Ensure database safety before doing anything
        var connectionString = context.Database.GetConnectionString();
        DatabaseSafetyGuard.AssertSafeDemoConnection(connectionString!);

        Console.WriteLine($"==> Target Database verified safe: {DatabaseSafetyGuard.AllowedDatabaseName}");
        Console.WriteLine($"==> Seeding reference date (AsOfDate): {asOfDate:yyyy-MM-dd}");
        Console.WriteLine($"==> Dry-run mode: {dryRun}");

        if (dryRun)
        {
            Console.WriteLine("[Dry-Run] Safety check and initialization simulated successfully.");
            return;
        }

        // 1. CLEAN UP PREVIOUS DEMO DATA IDEMPOTENTLY
        await CleanPreviousDemoDataAsync(context);

        // 2. SEED USERS (5 users: 4 active roles + 1 inactive)
        Console.WriteLine("[1/7] Seeding Demo User Accounts...");
        var users = await SeedUsersAsync(context, rawPassword, asOfDate);

        // 3. SEED PRODUCT GROUPS (4 groups: 3 active + 1 inactive)
        Console.WriteLine("[2/7] Seeding Demo Product Groups...");
        var productGroups = await SeedProductGroupsAsync(context, asOfDate);

        // 4. SEED SUPPLIERS (12 suppliers: 10 active + 2 inactive) & SUPPLIER GROUPS
        Console.WriteLine("[3/7] Seeding Demo Suppliers and Category Mappings...");
        var suppliers = await SeedSuppliersAsync(context, productGroups, asOfDate);

        // 5. SEED CRITERIA (7 criteria: 6 active + 1 inactive; Benefit & Cost, Point & Unit)
        Console.WriteLine("[4/7] Seeding Demo Evaluation Criteria...");
        var criteria = await SeedCriteriaAsync(context, asOfDate);

        // 6. SEED EVALUATION BATCHES & WORKFLOW CYCLES (8 batches spanning all lifecycle states)
        Console.WriteLine("[5/7] Seeding Evaluation Batches and Executing MCDM Workflows...");
        var batches = await SeedEvaluationBatchesAndWorkflowsAsync(
            context,
            ahpService,
            topsisService,
            decisionService,
            reportService,
            users,
            productGroups,
            suppliers,
            criteria,
            asOfDate);

        // 7. SEED AUDIT LOGS (20+ realistic entries)
        Console.WriteLine("[6/7] Seeding Demo System Audit Trail...");
        await SeedAuditLogsAsync(context, users, batches, asOfDate);

        Console.WriteLine("[7/7] Demo Dataset Seeding Completed Successfully!");
    }

    private static async Task CleanPreviousDemoDataAsync(ApplicationDbContext context)
    {
        Console.WriteLine("--> Cleaning existing demo entities for fresh idempotent run...");

        // 1. Delete dependent batch child tables for DEMO batches
        var demoBatchIds = await context.EvaluationBatches
            .Where(b => b.BatchCode.StartsWith("DEMO-"))
            .Select(b => b.BatchId)
            .ToListAsync();

        if (demoBatchIds.Count > 0)
        {
            var reports = await context.Reports.Where(r => demoBatchIds.Contains(r.BatchId)).ToListAsync();
            context.Reports.RemoveRange(reports);

            var snapshots = await context.BatchRoundSnapshots.Where(s => demoBatchIds.Contains(s.BatchId)).ToListAsync();
            context.BatchRoundSnapshots.RemoveRange(snapshots);

            var approvals = await context.Approvals.Where(a => demoBatchIds.Contains(a.BatchId)).ToListAsync();
            context.Approvals.RemoveRange(approvals);

            var topsisResults = await context.TopsisResults.Where(t => demoBatchIds.Contains(t.BatchId)).ToListAsync();
            context.TopsisResults.RemoveRange(topsisResults);

            var topsisIdeals = await context.TopsisIdealSolutions.Where(t => demoBatchIds.Contains(t.BatchId)).ToListAsync();
            context.TopsisIdealSolutions.RemoveRange(topsisIdeals);

            var ahpWeights = await context.AhpcriteriaWeights.Where(w => demoBatchIds.Contains(w.BatchId)).ToListAsync();
            context.AhpcriteriaWeights.RemoveRange(ahpWeights);

            var ahpResults = await context.Ahpresults.Where(r => demoBatchIds.Contains(r.BatchId)).ToListAsync();
            context.Ahpresults.RemoveRange(ahpResults);

            var ahpComparisons = await context.AhppairwiseComparisons.Where(c => demoBatchIds.Contains(c.BatchId)).ToListAsync();
            context.AhppairwiseComparisons.RemoveRange(ahpComparisons);

            var scores = await context.EvaluationScores.Where(s => demoBatchIds.Contains(s.BatchId)).ToListAsync();
            context.EvaluationScores.RemoveRange(scores);

            await context.SaveChangesAsync();

            // Clear Many-to-many relationships
            var batchesWithChildren = await context.EvaluationBatches
                .Include(b => b.Suppliers)
                .Include(b => b.Criteria)
                .Where(b => demoBatchIds.Contains(b.BatchId))
                .ToListAsync();

            foreach (var b in batchesWithChildren)
            {
                b.Suppliers.Clear();
                b.Criteria.Clear();
            }
            await context.SaveChangesAsync();

            context.EvaluationBatches.RemoveRange(batchesWithChildren);
            await context.SaveChangesAsync();
        }

        // 2. Clear SupplierGroups for demo suppliers / groups
        var demoSupplierIds = await context.Suppliers
            .Where(s => s.SupplierCode.StartsWith("DEMO-"))
            .Select(s => s.SupplierId)
            .ToListAsync();

        var demoGroupIds = await context.ProductGroups
            .Where(g => g.GroupCode.StartsWith("DEMO-"))
            .Select(g => g.GroupId)
            .ToListAsync();

        if (demoSupplierIds.Count > 0 || demoGroupIds.Count > 0)
        {
            var demoSuppliers = await context.Suppliers
                .Include(s => s.Groups)
                .Where(s => demoSupplierIds.Contains(s.SupplierId))
                .ToListAsync();

            foreach (var s in demoSuppliers)
            {
                s.Groups.Clear();
            }
            await context.SaveChangesAsync();

            context.Suppliers.RemoveRange(demoSuppliers);
            await context.SaveChangesAsync();
        }

        // 3. Clear demo criteria
        var demoCriteria = await context.Criteria
            .Where(c => c.CriteriaCode.StartsWith("DEMO-"))
            .ToListAsync();
        if (demoCriteria.Count > 0)
        {
            context.Criteria.RemoveRange(demoCriteria);
            await context.SaveChangesAsync();
        }

        // 4. Clear demo product groups
        var remainingGroups = await context.ProductGroups
            .Where(g => g.GroupCode.StartsWith("DEMO-"))
            .ToListAsync();
        if (remainingGroups.Count > 0)
        {
            context.ProductGroups.RemoveRange(remainingGroups);
            await context.SaveChangesAsync();
        }

        // 5. Clear demo audit logs first (to avoid FK_AuditLog_User)
        var demoUserIds = await context.Users
            .Where(u => u.Username.StartsWith("demo."))
            .Select(u => u.UserId)
            .ToListAsync();

        var demoAuditLogs = await context.AuditLogs
            .Where(a => (a.UserId != null && demoUserIds.Contains(a.UserId.Value)) ||
                        (a.Description != null && a.Description.Contains("[Demo]")))
            .ToListAsync();
        if (demoAuditLogs.Count > 0)
        {
            context.AuditLogs.RemoveRange(demoAuditLogs);
            await context.SaveChangesAsync();
        }

        // 6. Clear demo users
        var demoUsers = await context.Users
            .Where(u => u.Username.StartsWith("demo."))
            .ToListAsync();
        if (demoUsers.Count > 0)
        {
            context.Users.RemoveRange(demoUsers);
            await context.SaveChangesAsync();
        }

        Console.WriteLine("--> Previous demo entities cleaned successfully.");
    }

    private static async Task<Dictionary<string, User>> SeedUsersAsync(ApplicationDbContext context, string rawPassword, DateTime asOfDate)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword, 11);
        var baseDate = asOfDate.AddMonths(-6);

        var userList = new List<User>
        {
            new()
            {
                Username = "demo.admin",
                PasswordHash = passwordHash,
                FullName = "Demo Administrator",
                Email = "demo.admin@dss-demo.local",
                Phone = "0901000001",
                Role = "Administrator",
                Status = "Active",
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                LastLoginAt = asOfDate.AddHours(-1)
            },
            new()
            {
                Username = "demo.staff",
                PasswordHash = passwordHash,
                FullName = "Demo Purchasing Staff",
                Email = "demo.staff@dss-demo.local",
                Phone = "0901000002",
                Role = "PurchasingStaff",
                Status = "Active",
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                LastLoginAt = asOfDate.AddHours(-3)
            },
            new()
            {
                Username = "demo.expert",
                PasswordHash = passwordHash,
                FullName = "Demo Evaluation Expert",
                Email = "demo.expert@dss-demo.local",
                Phone = "0901000003",
                Role = "Expert",
                Status = "Active",
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                LastLoginAt = asOfDate.AddHours(-5)
            },
            new()
            {
                Username = "demo.manager",
                PasswordHash = passwordHash,
                FullName = "Demo Procurement Manager",
                Email = "demo.manager@dss-demo.local",
                Phone = "0901000004",
                Role = "Manager",
                Status = "Active",
                CreatedAt = baseDate,
                UpdatedAt = baseDate,
                LastLoginAt = asOfDate.AddHours(-2)
            },
            new()
            {
                Username = "demo.inactive",
                PasswordHash = passwordHash,
                FullName = "Demo Inactive Expert",
                Email = "demo.inactive@dss-demo.local",
                Phone = "0901000005",
                Role = "Expert",
                Status = "Inactive",
                CreatedAt = baseDate,
                UpdatedAt = baseDate.AddMonths(1),
                LastLoginAt = baseDate.AddMonths(1)
            }
        };

        context.Users.AddRange(userList);
        await context.SaveChangesAsync();

        return userList.ToDictionary(u => u.Username, u => u);
    }

    private static async Task<Dictionary<string, ProductGroup>> SeedProductGroupsAsync(ApplicationDbContext context, DateTime asOfDate)
    {
        var baseDate = asOfDate.AddMonths(-6);

        var groups = new List<ProductGroup>
        {
            new()
            {
                GroupCode = "DEMO-PG01",
                GroupName = "Office Supplies & Stationery",
                Description = "Nhóm hàng văn phòng phẩm, giấy in và vật tư tiêu hao văn phòng",
                Status = "Active",
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new()
            {
                GroupCode = "DEMO-PG02",
                GroupName = "IT Equipment & Enterprise Hardware",
                Description = "Máy tính, máy chủ trung tâm dữ liệu, thiết bị mạng và phần cứng CNTT",
                Status = "Active",
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new()
            {
                GroupCode = "DEMO-PG03",
                GroupName = "Logistics & Warehouse Services",
                Description = "Dịch vụ giao nhận vận tải hàng hoá, kho vận và hoàn tất đơn hàng",
                Status = "Active",
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new()
            {
                GroupCode = "DEMO-PG04",
                GroupName = "Archived / Deprecated Product Group",
                Description = "Nhóm danh mục cũ đã lưu trữ, phục vụ kiểm thử phân quyền và lọc trạng thái",
                Status = "Inactive",
                CreatedAt = baseDate,
                UpdatedAt = baseDate.AddMonths(2)
            }
        };

        context.ProductGroups.AddRange(groups);
        await context.SaveChangesAsync();

        return groups.ToDictionary(g => g.GroupCode, g => g);
    }

    private static async Task<Dictionary<string, Supplier>> SeedSuppliersAsync(
        ApplicationDbContext context,
        Dictionary<string, ProductGroup> groups,
        DateTime asOfDate)
    {
        var baseDate = asOfDate.AddMonths(-6);

        var supplierList = new List<Supplier>
        {
            new()
            {
                SupplierCode = "DEMO-S01",
                SupplierName = "Công ty CP Văn phòng phẩm Hồng Hà",
                Address = "25 Lý Thường Kiệt, Hoàn Kiếm, Hà Nội",
                ContactPerson = "Nguyễn Minh Tuấn",
                Email = "contact@hongha-demo.vn",
                Phone = "02438252200",
                TaxCode = "0100100101",
                BusinessField = "Văn phòng phẩm, giấy in",
                PaymentTerms = "Net 30 days",
                Website = "https://hongha-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S02",
                SupplierName = "Công ty CP Tập đoàn Thiên Long",
                Address = "Tầng 10, Sofic Tower, 10 Mai Chí Thọ, TP. Thủ Đức, TP. HCM",
                ContactPerson = "Trần Hoàng Nam",
                Email = "sales@thienlong-demo.vn",
                Phone = "02837505555",
                TaxCode = "0301464830",
                BusinessField = "Bút viết, dụng cụ văn phòng",
                PaymentTerms = "Net 30 days",
                Website = "https://thienlong-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S03",
                SupplierName = "Công ty TNHH Thương mại Dịch vụ Tân Phú Vinh",
                Address = "142 Võ Văn Tần, Quận 3, TP. HCM",
                ContactPerson = "Lê Quốc Bảo",
                Email = "info@tanphuvinh-demo.vn",
                Phone = "02839301234",
                TaxCode = "0304567890",
                BusinessField = "Văn phòng phẩm và Logistics nội địa",
                PaymentTerms = "Net 45 days",
                Website = "https://tanphuvinh-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S04",
                SupplierName = "Công ty TNHH Thiết bị Văn phòng Phúc Thịnh",
                Address = "518/1 Lê Văn Thọ, Gò Vấp, TP. HCM",
                ContactPerson = "Phạm Hải Đăng",
                Email = "support@phucthinh-demo.vn",
                Phone = "02862955566",
                TaxCode = "0309876543",
                BusinessField = "Thiết bị văn phòng, mực in",
                PaymentTerms = "Net 30 days",
                Website = "https://phucthinh-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S05",
                SupplierName = "Công ty CP Công nghệ FPT IS",
                Address = "Tòa nhà FPT, Phố Duy Tân, Cầu Giấy, Hà Nội",
                ContactPerson = "Vũ Đình Trọng",
                Email = "solutions@fptis-demo.vn",
                Phone = "02473007373",
                TaxCode = "0101234567",
                BusinessField = "Máy chủ, máy trạm, thiết bị mạng",
                PaymentTerms = "Net 45 days",
                Website = "https://fptis-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S06",
                SupplierName = "Công ty CP Đầu tư Thương mại CMC",
                Address = "Tòa nhà CMC, Duy Tân, Dịch Vọng Hậu, Cầu Giấy, Hà Nội",
                ContactPerson = "Đỗ Thanh Sơn",
                Email = "enterprise@cmc-demo.vn",
                Phone = "02437958686",
                TaxCode = "0102345678",
                BusinessField = "Hạ tầng CNTT và thiết bị viễn thông",
                PaymentTerms = "Net 45 days",
                Website = "https://cmc-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S07",
                SupplierName = "Công ty TNHH Phân phối Synnex FPT",
                Address = "Tòa nhà FPT Tân Thuận, KCX Tân Thuận, Quận 7, TP. HCM",
                ContactPerson = "Hoàng Thị Mai",
                Email = "dist@synnex-demo.vn",
                Phone = "02873006666",
                TaxCode = "0303456789",
                BusinessField = "Phân phối máy tính xách tay và linh kiện",
                PaymentTerms = "Net 30 days",
                Website = "https://synnex-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S08",
                SupplierName = "Công ty CP Thế Giới Số (Digiworld)",
                Address = "195 Nguyễn Thái Bình, Quận 1, TP. HCM",
                ContactPerson = "Bùi Quang Huy",
                Email = "b2b@digiworld-demo.vn",
                Phone = "02839290059",
                TaxCode = "0302888999",
                BusinessField = "Thiết bị số và điện tử chuyên dụng",
                PaymentTerms = "Net 30 days",
                Website = "https://digiworld-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S09",
                SupplierName = "Tổng Công ty CP Bưu chính Viettel (Viettel Post)",
                Address = "Toà nhà Viettel Post, Ngõ 15 Duy Tân, Cầu Giấy, Hà Nội",
                ContactPerson = "Ngô Đức Anh",
                Email = "logistics@viettelpost-demo.vn",
                Phone = "19008095",
                TaxCode = "0104050607",
                BusinessField = "Chuyển phát nhanh, kho bãi, logistics",
                PaymentTerms = "Net 30 days",
                Website = "https://viettelpost-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S10",
                SupplierName = "Công ty CP Giao Hàng Tiết Kiệm",
                Address = "Tòa nhà VTV, 8 Phạm Hùng, Cầu Giấy, Hà Nội",
                ContactPerson = "Lương Thế Vinh",
                Email = "enterprise@ghtk-demo.vn",
                Phone = "02473022222",
                TaxCode = "0106070809",
                BusinessField = "Dịch vụ giao nhận và lưu kho thương mại điện tử",
                PaymentTerms = "Net 30 days",
                Website = "https://ghtk-demo.vn",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S11",
                SupplierName = "Công ty TNHH Vận tải & Giao nhận Hải Yến (Ngừng HĐ)",
                Address = "12 Đặng Dung, Ba Đình, Hà Nội",
                ContactPerson = "Nguyễn Hải Yến",
                Email = "haiyen-inactive@demo.vn",
                Phone = "02437160000",
                TaxCode = "0108889999",
                BusinessField = "Vận tải đường bộ nội địa",
                PaymentTerms = "COD",
                Website = "https://haiyen-demo.vn",
                Status = "Inactive",
                CreatedAt = baseDate
            },
            new()
            {
                SupplierCode = "DEMO-S12",
                SupplierName = "Công ty TNHH Văn phòng phẩm Sao Mai (Ngừng HĐ)",
                Address = "88 Minh Khai, Hai Bà Trưng, Hà Nội",
                ContactPerson = "Trần Sao Mai",
                Email = "saomai-inactive@demo.vn",
                Phone = "02438620000",
                TaxCode = "0107778888",
                BusinessField = "Văn phòng phẩm tổng hợp",
                PaymentTerms = "COD",
                Website = "https://saomai-demo.vn",
                Status = "Inactive",
                CreatedAt = baseDate
            }
        };

        context.Suppliers.AddRange(supplierList);
        await context.SaveChangesAsync();

        var supplierMap = supplierList.ToDictionary(s => s.SupplierCode, s => s);

        // Map Suppliers to Groups (SupplierGroups)
        // DEMO-PG01: S01, S02, S03, S04 (active) + S12 (inactive)
        var pg01 = groups["DEMO-PG01"];
        pg01.Suppliers.Add(supplierMap["DEMO-S01"]);
        pg01.Suppliers.Add(supplierMap["DEMO-S02"]);
        pg01.Suppliers.Add(supplierMap["DEMO-S03"]);
        pg01.Suppliers.Add(supplierMap["DEMO-S04"]);
        pg01.Suppliers.Add(supplierMap["DEMO-S12"]);

        // DEMO-PG02: S05, S06, S07, S08
        var pg02 = groups["DEMO-PG02"];
        pg02.Suppliers.Add(supplierMap["DEMO-S05"]);
        pg02.Suppliers.Add(supplierMap["DEMO-S06"]);
        pg02.Suppliers.Add(supplierMap["DEMO-S07"]);
        pg02.Suppliers.Add(supplierMap["DEMO-S08"]);

        // DEMO-PG03: S09, S10, S03 (multi-category) + S11 (inactive)
        var pg03 = groups["DEMO-PG03"];
        pg03.Suppliers.Add(supplierMap["DEMO-S09"]);
        pg03.Suppliers.Add(supplierMap["DEMO-S10"]);
        pg03.Suppliers.Add(supplierMap["DEMO-S03"]);
        pg03.Suppliers.Add(supplierMap["DEMO-S11"]);

        // DEMO-PG04: S12 (inactive)
        var pg04 = groups["DEMO-PG04"];
        pg04.Suppliers.Add(supplierMap["DEMO-S12"]);

        await context.SaveChangesAsync();

        return supplierMap;
    }

    private static async Task<Dictionary<string, Criterion>> SeedCriteriaAsync(ApplicationDbContext context, DateTime asOfDate)
    {
        var baseDate = asOfDate.AddMonths(-6);

        var criteriaList = new List<Criterion>
        {
            new()
            {
                CriteriaCode = "DEMO-C01",
                CriteriaName = "Đơn giá chào hàng (Unit Price)",
                CriteriaType = "Cost",
                Unit = "VND",
                Description = "Chi phí mua hàng trên một đơn vị sản phẩm (càng thấp càng tốt)",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                CriteriaCode = "DEMO-C02",
                CriteriaName = "Chất lượng kỹ thuật (Product Quality)",
                CriteriaType = "Benefit",
                Unit = "Điểm",
                Description = "Chất lượng đáp ứng tiêu chuẩn kỹ thuật, tỷ lệ lỗi và độ tin cậy (thang điểm 0 - 10)",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                CriteriaCode = "DEMO-C03",
                CriteriaName = "Thời gian giao hàng (Lead Time)",
                CriteriaType = "Cost",
                Unit = "Ngày",
                Description = "Thời gian giao hàng tính từ lúc xác nhận đơn (càng ngắn càng tốt)",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                CriteriaCode = "DEMO-C04",
                CriteriaName = "Năng lực cung ứng (Supply Capacity)",
                CriteriaType = "Benefit",
                Unit = "Điểm",
                Description = "Khả năng đáp ứng sản lượng lớn và xử lý biến động nhu cầu (thang điểm 0 - 10)",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                CriteriaCode = "DEMO-C05",
                CriteriaName = "Dịch vụ bảo hành & hậu mãi (After-Sales)",
                CriteriaType = "Benefit",
                Unit = "Điểm",
                Description = "Chính sách bảo hành, hỗ trợ kỹ thuật tận nơi và tốc độ phản hồi (thang điểm 0 - 10)",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                CriteriaCode = "DEMO-C06",
                CriteriaName = "Thời hạn thanh toán linh hoạt (Payment Terms)",
                CriteriaType = "Benefit",
                Unit = "Ngày",
                Description = "Thời hạn thanh toán công nợ cho phép (càng dài càng có lợi)",
                Status = "Active",
                CreatedAt = baseDate
            },
            new()
            {
                CriteriaCode = "DEMO-C07",
                CriteriaName = "Chỉ số kiểm định cũ (Legacy Metric)",
                CriteriaType = "Cost",
                Unit = "VND",
                Description = "Tiêu chí kiểm định theo quy chế cũ, đã ngừng áp dụng",
                Status = "Inactive",
                CreatedAt = baseDate
            }
        };

        context.Criteria.AddRange(criteriaList);
        await context.SaveChangesAsync();

        return criteriaList.ToDictionary(c => c.CriteriaCode, c => c);
    }

    private static async Task<Dictionary<string, EvaluationBatch>> SeedEvaluationBatchesAndWorkflowsAsync(
        ApplicationDbContext context,
        AHPService ahpService,
        TOPSISService topsisService,
        DecisionResultService decisionService,
        ReportService reportService,
        Dictionary<string, User> users,
        Dictionary<string, ProductGroup> groups,
        Dictionary<string, Supplier> suppliers,
        Dictionary<string, Criterion> criteria,
        DateTime asOfDate)
    {
        var staffUser = users["demo.staff"];
        var expertUser = users["demo.expert"];
        var managerUser = users["demo.manager"];
        var adminUser = users["demo.admin"];

        var batchMap = new Dictionary<string, EvaluationBatch>();

        // =========================================================================
        // BATCH 1: DEMO-B01 (Completed & Approved, Reports Generated)
        // Office Supplies (PG01) - 4 suppliers x 4 criteria
        // =========================================================================
        Console.WriteLine("    -> Seeding DEMO-B01 (Completed & Approved with Reports)...");
        var b01Date = asOfDate.AddMonths(-5);
        var b01Completed = b01Date.AddDays(15);
        var b01 = new EvaluationBatch
        {
            BatchCode = "DEMO-B01",
            BatchName = "Đợt mua sắm Văn phòng phẩm Định kỳ Quý 2/2026",
            Description = "Mua sắm giấy in, mực in, tài liệu lưu trữ phục vụ các phòng ban và chi nhánh Q2/2026",
            GroupId = groups["DEMO-PG01"].GroupId,
            StartDate = DateOnly.FromDateTime(b01Date),
            EndDate = DateOnly.FromDateTime(b01Completed),
            Status = BatchStatus.Evaluating, // Set to Evaluating first so services and triggers succeed
            CreatedBy = staffUser.UserId,
            CreatedAt = b01Date
        };
        b01.Suppliers.Add(suppliers["DEMO-S01"]);
        b01.Suppliers.Add(suppliers["DEMO-S02"]);
        b01.Suppliers.Add(suppliers["DEMO-S03"]);
        b01.Suppliers.Add(suppliers["DEMO-S04"]);

        b01.Criteria.Add(criteria["DEMO-C01"]); // Unit Price (Cost)
        b01.Criteria.Add(criteria["DEMO-C02"]); // Quality (Benefit)
        b01.Criteria.Add(criteria["DEMO-C03"]); // Lead Time (Cost)
        b01.Criteria.Add(criteria["DEMO-C05"]); // After-Sales (Benefit)

        context.EvaluationBatches.Add(b01);
        await context.SaveChangesAsync();

        // 16 Scores for B01
        var b01Scores = new List<EvaluationScore>
        {
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S01"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 145000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S01"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 9.2m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S01"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 3m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S01"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 8.8m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b01Date.AddDays(2) },

            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S02"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 152000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S02"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 9.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S02"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 4m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S02"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 9.2m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b01Date.AddDays(2) },

            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 138000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 5m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 8.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b01Date.AddDays(2) },

            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S04"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 160000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S04"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S04"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 6m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
            new() { BatchId = b01.BatchId, SupplierId = suppliers["DEMO-S04"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 7.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b01Date.AddDays(2) },
        };
        context.EvaluationScores.AddRange(b01Scores);
        await context.SaveChangesAsync();

        // AHP Comparisons for B01 (C01, C02, C03, C05)
        var b01CriteriaIds = new[] { criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C03"].CriteriaId, criteria["DEMO-C05"].CriteriaId };
        await InsertPairwiseComparisonsAsync(context, b01.BatchId, b01CriteriaIds, expertUser.UserId, b01Date.AddDays(3), new Dictionary<(int, int), decimal>
        {
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId), 0.5m },
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C03"].CriteriaId), 3.0m },
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 4.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C03"].CriteriaId), 4.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 5.0m },
            { (criteria["DEMO-C03"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 2.0m },
        });

        // Calculate AHP & TOPSIS
        await ahpService.CalculateAsync(b01.BatchId);
        await topsisService.CalculateAsync(b01.BatchId);

        // Find Rank 1 supplier
        var b01Top = await context.TopsisResults.Where(t => t.BatchId == b01.BatchId && t.RankNo == 1).FirstAsync();
        await decisionService.ApproveAsync(
            b01.BatchId,
            b01Top.SupplierId,
            managerUser.UserId,
            "Phê duyệt lựa chọn nhà cung cấp có điểm số TOPSIS cao nhất cho gói văn phòng phẩm Quý 2/2026.");

        // Fix completion timestamps
        b01.CompletedAt = b01Completed;
        await context.SaveChangesAsync();

        // Generate PDF and Excel reports
        await reportService.GeneratePdfAsync(b01.BatchId, adminUser.UserId);
        await reportService.GenerateExcelAsync(b01.BatchId, adminUser.UserId);

        batchMap["DEMO-B01"] = b01;

        // =========================================================================
        // BATCH 2: DEMO-B02 (Completed & Approved, Reports Generated)
        // IT Equipment (PG02) - 3 suppliers x 4 criteria
        // =========================================================================
        Console.WriteLine("    -> Seeding DEMO-B02 (Completed & Approved with Reports)...");
        var b02Date = asOfDate.AddMonths(-3);
        var b02Completed = b02Date.AddDays(12);
        var b02 = new EvaluationBatch
        {
            BatchCode = "DEMO-B02",
            BatchName = "Đợt trang bị Máy chủ & Thiết bị mạng Trung tâm Dữ liệu",
            Description = "Mua sắm máy chủ phiến (blade server), thiết bị định tuyến lõi và chuyển mạch trung tâm dữ liệu",
            GroupId = groups["DEMO-PG02"].GroupId,
            StartDate = DateOnly.FromDateTime(b02Date),
            EndDate = DateOnly.FromDateTime(b02Completed),
            Status = BatchStatus.Evaluating,
            CreatedBy = staffUser.UserId,
            CreatedAt = b02Date
        };
        b02.Suppliers.Add(suppliers["DEMO-S05"]);
        b02.Suppliers.Add(suppliers["DEMO-S06"]);
        b02.Suppliers.Add(suppliers["DEMO-S07"]);

        b02.Criteria.Add(criteria["DEMO-C01"]); // Unit Price (Cost)
        b02.Criteria.Add(criteria["DEMO-C02"]); // Quality (Benefit)
        b02.Criteria.Add(criteria["DEMO-C04"]); // Capacity (Benefit)
        b02.Criteria.Add(criteria["DEMO-C05"]); // After-Sales (Benefit)

        context.EvaluationBatches.Add(b02);
        await context.SaveChangesAsync();

        var b02Scores = new List<EvaluationScore>
        {
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S05"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 850000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S05"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 9.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S05"].SupplierId, CriteriaId = criteria["DEMO-C04"].CriteriaId, ScoreValue = 9.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S05"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 9.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b02Date.AddDays(2) },

            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S06"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 820000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S06"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 9.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S06"].SupplierId, CriteriaId = criteria["DEMO-C04"].CriteriaId, ScoreValue = 8.8m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S06"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 8.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b02Date.AddDays(2) },

            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S07"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 790000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S07"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.2m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S07"].SupplierId, CriteriaId = criteria["DEMO-C04"].CriteriaId, ScoreValue = 8.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
            new() { BatchId = b02.BatchId, SupplierId = suppliers["DEMO-S07"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 8.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b02Date.AddDays(2) },
        };
        context.EvaluationScores.AddRange(b02Scores);
        await context.SaveChangesAsync();

        var b02CriteriaIds = new[] { criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C04"].CriteriaId, criteria["DEMO-C05"].CriteriaId };
        await InsertPairwiseComparisonsAsync(context, b02.BatchId, b02CriteriaIds, expertUser.UserId, b02Date.AddDays(3), new Dictionary<(int, int), decimal>
        {
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId), 0.3333m },
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C04"].CriteriaId), 1.0m },
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 2.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C04"].CriteriaId), 3.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 4.0m },
            { (criteria["DEMO-C04"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 2.0m },
        });

        await ahpService.CalculateAsync(b02.BatchId);
        await topsisService.CalculateAsync(b02.BatchId);

        var b02Top = await context.TopsisResults.Where(t => t.BatchId == b02.BatchId && t.RankNo == 1).FirstAsync();
        await decisionService.ApproveAsync(
            b02.BatchId,
            b02Top.SupplierId,
            managerUser.UserId,
            "Phê duyệt giải pháp máy chủ và mạng dựa trên điểm kỹ thuật và dịch vụ hậu mãi vượt trội.");

        b02.CompletedAt = b02Completed;
        await context.SaveChangesAsync();

        await reportService.GeneratePdfAsync(b02.BatchId, adminUser.UserId);
        await reportService.GenerateExcelAsync(b02.BatchId, adminUser.UserId);

        batchMap["DEMO-B02"] = b02;

        // =========================================================================
        // BATCH 3: DEMO-B03 (Evaluating, Ready for Approval)
        // Logistics (PG03) - 3 suppliers x 4 criteria, AHP consistent, TOPSIS calculated, NO approval
        // =========================================================================
        Console.WriteLine("    -> Seeding DEMO-B03 (Evaluating, Ready for Approval)...");
        var b03Date = asOfDate.AddDays(-20);
        var b03 = new EvaluationBatch
        {
            BatchCode = "DEMO-B03",
            BatchName = "Đợt chọn Nhà cung cấp Dịch vụ Vận tải & Kho bãi Miền Bắc",
            Description = "Thuê dịch vụ kho bãi logistics vệ tinh và vận tải tuyến Hà Nội - Hải Phòng - Quảng Ninh",
            GroupId = groups["DEMO-PG03"].GroupId,
            StartDate = DateOnly.FromDateTime(b03Date),
            EndDate = DateOnly.FromDateTime(asOfDate.AddDays(10)),
            Status = BatchStatus.Evaluating,
            CreatedBy = staffUser.UserId,
            CreatedAt = b03Date
        };
        b03.Suppliers.Add(suppliers["DEMO-S09"]);
        b03.Suppliers.Add(suppliers["DEMO-S10"]);
        b03.Suppliers.Add(suppliers["DEMO-S03"]);

        b03.Criteria.Add(criteria["DEMO-C01"]); // Unit Price
        b03.Criteria.Add(criteria["DEMO-C02"]); // Quality
        b03.Criteria.Add(criteria["DEMO-C03"]); // Lead Time
        b03.Criteria.Add(criteria["DEMO-C06"]); // Payment Terms

        context.EvaluationBatches.Add(b03);
        await context.SaveChangesAsync();

        var b03Scores = new List<EvaluationScore>
        {
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S09"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 45000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S09"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 9.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S09"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 2m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S09"].SupplierId, CriteriaId = criteria["DEMO-C06"].CriteriaId, ScoreValue = 45m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b03Date.AddDays(2) },

            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S10"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 42000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S10"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S10"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 3m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S10"].SupplierId, CriteriaId = criteria["DEMO-C06"].CriteriaId, ScoreValue = 30m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b03Date.AddDays(2) },

            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 48000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.8m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 2m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
            new() { BatchId = b03.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C06"].CriteriaId, ScoreValue = 60m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b03Date.AddDays(2) },
        };
        context.EvaluationScores.AddRange(b03Scores);
        await context.SaveChangesAsync();

        var b03CriteriaIds = new[] { criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C03"].CriteriaId, criteria["DEMO-C06"].CriteriaId };
        await InsertPairwiseComparisonsAsync(context, b03.BatchId, b03CriteriaIds, expertUser.UserId, b03Date.AddDays(3), new Dictionary<(int, int), decimal>
        {
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId), 0.5m },
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C03"].CriteriaId), 2.0m },
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C06"].CriteriaId), 3.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C03"].CriteriaId), 3.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C06"].CriteriaId), 4.0m },
            { (criteria["DEMO-C03"].CriteriaId, criteria["DEMO-C06"].CriteriaId), 2.0m },
        });

        await ahpService.CalculateAsync(b03.BatchId);
        await topsisService.CalculateAsync(b03.BatchId);

        batchMap["DEMO-B03"] = b03;

        // =========================================================================
        // BATCH 4: DEMO-B04 (Evaluating, Ready for TOPSIS)
        // Office Supplies (PG01) - 3 suppliers x 4 criteria, AHP calculated, NO TOPSIS
        // =========================================================================
        Console.WriteLine("    -> Seeding DEMO-B04 (Evaluating, Ready for TOPSIS)...");
        var b04Date = asOfDate.AddDays(-14);
        var b04 = new EvaluationBatch
        {
            BatchCode = "DEMO-B04",
            BatchName = "Đợt mua sắm Giấy in và Vật tư văn phòng Đột xuất Q4",
            Description = "Bổ sung vật tư đóng gói và giấy in cao cấp phục vụ mùa báo cáo tài chính cuối năm",
            GroupId = groups["DEMO-PG01"].GroupId,
            StartDate = DateOnly.FromDateTime(b04Date),
            EndDate = DateOnly.FromDateTime(asOfDate.AddDays(15)),
            Status = BatchStatus.Evaluating,
            CreatedBy = staffUser.UserId,
            CreatedAt = b04Date
        };
        b04.Suppliers.Add(suppliers["DEMO-S01"]);
        b04.Suppliers.Add(suppliers["DEMO-S02"]);
        b04.Suppliers.Add(suppliers["DEMO-S03"]);

        b04.Criteria.Add(criteria["DEMO-C01"]);
        b04.Criteria.Add(criteria["DEMO-C02"]);
        b04.Criteria.Add(criteria["DEMO-C03"]);
        b04.Criteria.Add(criteria["DEMO-C05"]);

        context.EvaluationBatches.Add(b04);
        await context.SaveChangesAsync();

        var b04Scores = new List<EvaluationScore>
        {
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S01"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 146000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S01"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 9.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S01"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 3m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S01"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 8.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b04Date.AddDays(2) },

            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S02"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 150000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S02"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 9.2m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S02"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 4m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S02"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 9.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b04Date.AddDays(2) },

            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 140000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.2m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 5m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
            new() { BatchId = b04.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 8.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b04Date.AddDays(2) },
        };
        context.EvaluationScores.AddRange(b04Scores);
        await context.SaveChangesAsync();

        var b04CriteriaIds = new[] { criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C03"].CriteriaId, criteria["DEMO-C05"].CriteriaId };
        await InsertPairwiseComparisonsAsync(context, b04.BatchId, b04CriteriaIds, expertUser.UserId, b04Date.AddDays(3), new Dictionary<(int, int), decimal>
        {
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId), 0.5m },
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C03"].CriteriaId), 3.0m },
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 4.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C03"].CriteriaId), 4.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 5.0m },
            { (criteria["DEMO-C03"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 2.0m },
        });

        await ahpService.CalculateAsync(b04.BatchId);

        batchMap["DEMO-B04"] = b04;

        // =========================================================================
        // BATCH 5: DEMO-B05 (Evaluating, AHP Inconsistency CR > 0.10)
        // IT Equipment (PG02) - 3 suppliers x 4 criteria, scores complete, AHP CR > 0.10, NO TOPSIS
        // =========================================================================
        Console.WriteLine("    -> Seeding DEMO-B05 (Evaluating, AHP Inconsistency Warning CR > 0.10)...");
        var b05Date = asOfDate.AddDays(-10);
        var b05 = new EvaluationBatch
        {
            BatchCode = "DEMO-B05",
            BatchName = "Đợt nâng cấp Laptop cho Khối Kỹ thuật & R&D",
            Description = "Mua sắm máy tính xách tay cấu hình cao trang bị cho kỹ sư phần mềm và nghiên cứu phát triển",
            GroupId = groups["DEMO-PG02"].GroupId,
            StartDate = DateOnly.FromDateTime(b05Date),
            EndDate = DateOnly.FromDateTime(asOfDate.AddDays(20)),
            Status = BatchStatus.Evaluating,
            CreatedBy = staffUser.UserId,
            CreatedAt = b05Date
        };
        b05.Suppliers.Add(suppliers["DEMO-S05"]);
        b05.Suppliers.Add(suppliers["DEMO-S06"]);
        b05.Suppliers.Add(suppliers["DEMO-S08"]);

        b05.Criteria.Add(criteria["DEMO-C01"]); // Unit Price
        b05.Criteria.Add(criteria["DEMO-C02"]); // Quality
        b05.Criteria.Add(criteria["DEMO-C04"]); // Capacity
        b05.Criteria.Add(criteria["DEMO-C05"]); // After-Sales

        context.EvaluationBatches.Add(b05);
        await context.SaveChangesAsync();

        var b05Scores = new List<EvaluationScore>
        {
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S05"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 28000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S05"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 9.2m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S05"].SupplierId, CriteriaId = criteria["DEMO-C04"].CriteriaId, ScoreValue = 9.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S05"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 9.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b05Date.AddDays(2) },

            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S06"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 26500000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S06"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.8m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S06"].SupplierId, CriteriaId = criteria["DEMO-C04"].CriteriaId, ScoreValue = 8.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S06"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 8.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b05Date.AddDays(2) },

            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S08"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 25000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S08"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S08"].SupplierId, CriteriaId = criteria["DEMO-C04"].CriteriaId, ScoreValue = 8.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
            new() { BatchId = b05.BatchId, SupplierId = suppliers["DEMO-S08"].SupplierId, CriteriaId = criteria["DEMO-C05"].CriteriaId, ScoreValue = 8.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b05Date.AddDays(2) },
        };
        context.EvaluationScores.AddRange(b05Scores);
        await context.SaveChangesAsync();

        // Intentionally inconsistent AHP comparisons: C01 >> C02, C02 >> C04, C04 >> C01 (cyclic conflict)
        var b05CriteriaIds = new[] { criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C04"].CriteriaId, criteria["DEMO-C05"].CriteriaId };
        await InsertPairwiseComparisonsAsync(context, b05.BatchId, b05CriteriaIds, expertUser.UserId, b05Date.AddDays(3), new Dictionary<(int, int), decimal>
        {
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C02"].CriteriaId), 7.0m },
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C04"].CriteriaId), 0.1111m }, // C04 vs C01 is 9.0!
            { (criteria["DEMO-C01"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 5.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C04"].CriteriaId), 8.0m },
            { (criteria["DEMO-C02"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 0.2m },
            { (criteria["DEMO-C04"].CriteriaId, criteria["DEMO-C05"].CriteriaId), 6.0m },
        });

        // AHP calculation will succeed, save CR > 0.10 and IsConsistent = false
        await ahpService.CalculateAsync(b05.BatchId);

        batchMap["DEMO-B05"] = b05;

        // =========================================================================
        // BATCH 6: DEMO-B06 (Evaluating, Incomplete Scores)
        // Logistics (PG03) - 3 suppliers x 4 criteria, 7 filled, 5 missing
        // =========================================================================
        Console.WriteLine("    -> Seeding DEMO-B06 (Evaluating, Scoring in Progress)...");
        var b06Date = asOfDate.AddDays(-5);
        var b06 = new EvaluationBatch
        {
            BatchCode = "DEMO-B06",
            BatchName = "Đợt thẩm định Dịch vụ Chuyển phát Thương mại điện tử",
            Description = "Lựa chọn đối tác cung ứng dịch vụ chuyển phát nhanh nội thành cho mảng thương mại điện tử",
            GroupId = groups["DEMO-PG03"].GroupId,
            StartDate = DateOnly.FromDateTime(b06Date),
            EndDate = DateOnly.FromDateTime(asOfDate.AddDays(25)),
            Status = BatchStatus.Evaluating,
            CreatedBy = staffUser.UserId,
            CreatedAt = b06Date
        };
        b06.Suppliers.Add(suppliers["DEMO-S09"]);
        b06.Suppliers.Add(suppliers["DEMO-S10"]);
        b06.Suppliers.Add(suppliers["DEMO-S03"]);

        b06.Criteria.Add(criteria["DEMO-C01"]);
        b06.Criteria.Add(criteria["DEMO-C02"]);
        b06.Criteria.Add(criteria["DEMO-C03"]);
        b06.Criteria.Add(criteria["DEMO-C06"]);

        context.EvaluationBatches.Add(b06);
        await context.SaveChangesAsync();

        // 7 filled out of 12 scores
        var b06Scores = new List<EvaluationScore>
        {
            new() { BatchId = b06.BatchId, SupplierId = suppliers["DEMO-S09"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 50000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b06Date.AddDays(1) },
            new() { BatchId = b06.BatchId, SupplierId = suppliers["DEMO-S09"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.5m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b06Date.AddDays(1) },
            new() { BatchId = b06.BatchId, SupplierId = suppliers["DEMO-S09"].SupplierId, CriteriaId = criteria["DEMO-C03"].CriteriaId, ScoreValue = 2m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b06Date.AddDays(1) },

            new() { BatchId = b06.BatchId, SupplierId = suppliers["DEMO-S10"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 48000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b06Date.AddDays(1) },
            new() { BatchId = b06.BatchId, SupplierId = suppliers["DEMO-S10"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.0m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b06Date.AddDays(1) },

            new() { BatchId = b06.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C01"].CriteriaId, ScoreValue = 52000000m, EvaluatedBy = staffUser.UserId, EvaluatedAt = b06Date.AddDays(1) },
            new() { BatchId = b06.BatchId, SupplierId = suppliers["DEMO-S03"].SupplierId, CriteriaId = criteria["DEMO-C02"].CriteriaId, ScoreValue = 8.8m, EvaluatedBy = expertUser.UserId, EvaluatedAt = b06Date.AddDays(1) },
        };
        context.EvaluationScores.AddRange(b06Scores);
        await context.SaveChangesAsync();

        batchMap["DEMO-B06"] = b06;

        // =========================================================================
        // BATCH 7: DEMO-B07 (Draft)
        // Office Supplies (PG01) - 2 suppliers x 2 criteria, no scores
        // =========================================================================
        Console.WriteLine("    -> Seeding DEMO-B07 (Draft Batch)...");
        var b07Date = asOfDate.AddDays(-2);
        var b07 = new EvaluationBatch
        {
            BatchCode = "DEMO-B07",
            BatchName = "Kế hoạch mua sắm Thiết bị Văn phòng Chi nhánh Đà Nẵng",
            Description = "Dự thảo kế hoạch chọn nhà cung ứng văn phòng phẩm và trang thiết bị cho chi nhánh mới thành lập",
            GroupId = groups["DEMO-PG01"].GroupId,
            StartDate = DateOnly.FromDateTime(asOfDate.AddDays(5)),
            EndDate = DateOnly.FromDateTime(asOfDate.AddDays(30)),
            Status = BatchStatus.Draft,
            CreatedBy = staffUser.UserId,
            CreatedAt = b07Date
        };
        b07.Suppliers.Add(suppliers["DEMO-S01"]);
        b07.Suppliers.Add(suppliers["DEMO-S02"]);

        b07.Criteria.Add(criteria["DEMO-C01"]);
        b07.Criteria.Add(criteria["DEMO-C02"]);

        context.EvaluationBatches.Add(b07);
        await context.SaveChangesAsync();

        batchMap["DEMO-B07"] = b07;

        // =========================================================================
        // BATCH 8: DEMO-B08 (Cancelled)
        // IT Equipment (PG02) - Cancelled with audit trail
        // =========================================================================
        Console.WriteLine("    -> Seeding DEMO-B08 (Cancelled Batch)...");
        var b08Date = asOfDate.AddMonths(-2);
        var b08Cancelled = b08Date.AddDays(3);
        var b08 = new EvaluationBatch
        {
            BatchCode = "DEMO-B08",
            BatchName = "Đợt chào giá Thiết bị Lưu trữ NAS Dự phòng (Đã hủy)",
            Description = "Hủy do thay đổi kế hoạch ngân sách và chuyển dịch trọng tâm sang dịch vụ Cloud Storage",
            GroupId = groups["DEMO-PG02"].GroupId,
            StartDate = DateOnly.FromDateTime(b08Date),
            EndDate = DateOnly.FromDateTime(b08Date.AddDays(20)),
            Status = BatchStatus.Cancelled,
            CancelledAt = b08Cancelled,
            CreatedBy = staffUser.UserId,
            CreatedAt = b08Date
        };
        b08.Suppliers.Add(suppliers["DEMO-S05"]);
        b08.Suppliers.Add(suppliers["DEMO-S06"]);

        b08.Criteria.Add(criteria["DEMO-C01"]);
        b08.Criteria.Add(criteria["DEMO-C04"]);

        context.EvaluationBatches.Add(b08);
        await context.SaveChangesAsync();

        batchMap["DEMO-B08"] = b08;

        return batchMap;
    }

    private static async Task InsertPairwiseComparisonsAsync(
        ApplicationDbContext context,
        int batchId,
        int[] criteriaIds,
        int evaluatedBy,
        DateTime evaluatedAt,
        Dictionary<(int, int), decimal> directComparisons)
    {
        var n = criteriaIds.Length;
        for (var i = 0; i < n; i++)
        {
            var rowId = criteriaIds[i];
            for (var j = 0; j < n; j++)
            {
                var colId = criteriaIds[j];
                decimal value;

                if (rowId == colId)
                {
                    value = 1.0m;
                }
                else if (directComparisons.TryGetValue((rowId, colId), out var directVal))
                {
                    value = directVal;
                }
                else if (directComparisons.TryGetValue((colId, rowId), out var inverseVal))
                {
                    value = SaatyScale.GetReciprocalDecimal(inverseVal);
                }
                else
                {
                    value = 1.0m;
                }

                context.AhppairwiseComparisons.Add(new AhppairwiseComparison
                {
                    BatchId = batchId,
                    CriteriaRowId = rowId,
                    CriteriaColId = colId,
                    ComparisonValue = value,
                    EvaluatedBy = evaluatedBy,
                    EvaluatedAt = evaluatedAt
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAuditLogsAsync(
        ApplicationDbContext context,
        Dictionary<string, User> users,
        Dictionary<string, EvaluationBatch> batches,
        DateTime asOfDate)
    {
        var admin = users["demo.admin"];
        var staff = users["demo.staff"];
        var expert = users["demo.expert"];
        var manager = users["demo.manager"];

        var b01 = batches["DEMO-B01"];
        var b02 = batches["DEMO-B02"];
        var b03 = batches["DEMO-B03"];
        var b04 = batches["DEMO-B04"];
        var b05 = batches["DEMO-B05"];
        var b06 = batches["DEMO-B06"];
        var b07 = batches["DEMO-B07"];
        var b08 = batches["DEMO-B08"];

        var logs = new List<AuditLog>
        {
            // B01 Timeline
            new() { UserId = staff.UserId, ActionType = AuditActions.CreateBatch, TargetTable = "EvaluationBatches", TargetId = b01.BatchId, NewValue = b01.BatchCode, Description = $"[Demo] Khởi tạo đợt đánh giá {b01.BatchCode}", ActionTime = b01.CreatedAt },
            new() { UserId = staff.UserId, ActionType = AuditActions.StartBatch, TargetTable = "EvaluationBatches", TargetId = b01.BatchId, OldValue = "Draft", NewValue = "Evaluating", Description = $"[Demo] Chuyển trạng thái đợt đánh giá {b01.BatchCode} sang Đang đánh giá", ActionTime = b01.CreatedAt.AddHours(2) },
            new() { UserId = expert.UserId, ActionType = AuditActions.SaveEvaluation, TargetTable = "EvaluationScores", TargetId = b01.BatchId, Description = $"[Demo] Nhập bảng điểm đánh giá kỹ thuật và chất lượng cho {b01.BatchCode}", ActionTime = b01.CreatedAt.AddDays(2) },
            new() { UserId = expert.UserId, ActionType = AuditActions.CalculateAHP, TargetTable = "AHPResults", TargetId = b01.BatchId, Description = $"[Demo] Thực hiện tính toán trọng số tiêu chí AHP cho {b01.BatchCode}", ActionTime = b01.CreatedAt.AddDays(3) },
            new() { UserId = staff.UserId, ActionType = AuditActions.CalculateTOPSIS, TargetTable = "TopsisResults", TargetId = b01.BatchId, Description = $"[Demo] Thực hiện xếp hạng nhà cung cấp theo TOPSIS cho {b01.BatchCode}", ActionTime = b01.CreatedAt.AddDays(5) },
            new() { UserId = manager.UserId, ActionType = AuditActions.Approve, TargetTable = "Approvals", TargetId = b01.BatchId, NewValue = "Approved", Description = $"[Demo] Phê duyệt kết quả lựa chọn nhà cung cấp cho {b01.BatchCode}", ActionTime = b01.CompletedAt ?? b01.CreatedAt.AddDays(15) },
            new() { UserId = admin.UserId, ActionType = AuditActions.GeneratePdf, TargetTable = "Reports", TargetId = b01.BatchId, Description = $"[Demo] Xuất báo cáo kết quả đánh giá định dạng PDF cho {b01.BatchCode}", ActionTime = (b01.CompletedAt ?? b01.CreatedAt.AddDays(15)).AddMinutes(5) },
            new() { UserId = admin.UserId, ActionType = AuditActions.GenerateExcel, TargetTable = "Reports", TargetId = b01.BatchId, Description = $"[Demo] Xuất bảng tính dữ liệu chi tiết định dạng Excel cho {b01.BatchCode}", ActionTime = (b01.CompletedAt ?? b01.CreatedAt.AddDays(15)).AddMinutes(6) },

            // B02 Timeline
            new() { UserId = staff.UserId, ActionType = AuditActions.CreateBatch, TargetTable = "EvaluationBatches", TargetId = b02.BatchId, NewValue = b02.BatchCode, Description = $"[Demo] Khởi tạo đợt đánh giá {b02.BatchCode}", ActionTime = b02.CreatedAt },
            new() { UserId = expert.UserId, ActionType = AuditActions.CalculateAHP, TargetTable = "AHPResults", TargetId = b02.BatchId, Description = $"[Demo] Tính toán ma trận so sánh cặp AHP cho {b02.BatchCode}", ActionTime = b02.CreatedAt.AddDays(3) },
            new() { UserId = staff.UserId, ActionType = AuditActions.CalculateTOPSIS, TargetTable = "TopsisResults", TargetId = b02.BatchId, Description = $"[Demo] Tính toán xếp hạng TOPSIS cho {b02.BatchCode}", ActionTime = b02.CreatedAt.AddDays(4) },
            new() { UserId = manager.UserId, ActionType = AuditActions.Approve, TargetTable = "Approvals", TargetId = b02.BatchId, NewValue = "Approved", Description = $"[Demo] Phê duyệt quyết định lựa chọn nhà thầu CNTT cho {b02.BatchCode}", ActionTime = b02.CompletedAt ?? b02.CreatedAt.AddDays(12) },
            new() { UserId = admin.UserId, ActionType = AuditActions.GeneratePdf, TargetTable = "Reports", TargetId = b02.BatchId, Description = $"[Demo] Xuất báo cáo tổng kết PDF cho {b02.BatchCode}", ActionTime = (b02.CompletedAt ?? b02.CreatedAt.AddDays(12)).AddMinutes(10) },

            // B03 Timeline (Ready for Approval)
            new() { UserId = staff.UserId, ActionType = AuditActions.CreateBatch, TargetTable = "EvaluationBatches", TargetId = b03.BatchId, NewValue = b03.BatchCode, Description = $"[Demo] Khởi tạo đợt đánh giá dịch vụ Logistics {b03.BatchCode}", ActionTime = b03.CreatedAt },
            new() { UserId = expert.UserId, ActionType = AuditActions.CalculateAHP, TargetTable = "AHPResults", TargetId = b03.BatchId, Description = $"[Demo] Chốt trọng số tiêu chí AHP cho {b03.BatchCode}", ActionTime = b03.CreatedAt.AddDays(3) },
            new() { UserId = staff.UserId, ActionType = AuditActions.CalculateTOPSIS, TargetTable = "TopsisResults", TargetId = b03.BatchId, Description = $"[Demo] Tính toán kết quả xếp hạng TOPSIS hoàn tất, sẵn sàng phê duyệt cho {b03.BatchCode}", ActionTime = b03.CreatedAt.AddDays(4) },

            // B04 Timeline (Ready for TOPSIS)
            new() { UserId = staff.UserId, ActionType = AuditActions.CreateBatch, TargetTable = "EvaluationBatches", TargetId = b04.BatchId, NewValue = b04.BatchCode, Description = $"[Demo] Khởi tạo đợt mua sắm bổ sung {b04.BatchCode}", ActionTime = b04.CreatedAt },
            new() { UserId = expert.UserId, ActionType = AuditActions.CalculateAHP, TargetTable = "AHPResults", TargetId = b04.BatchId, Description = $"[Demo] Thiết lập và tính toán ma trận AHP nhất quán cho {b04.BatchCode}", ActionTime = b04.CreatedAt.AddDays(3) },

            // B05 Timeline (AHP Inconsistency)
            new() { UserId = staff.UserId, ActionType = AuditActions.CreateBatch, TargetTable = "EvaluationBatches", TargetId = b05.BatchId, NewValue = b05.BatchCode, Description = $"[Demo] Khởi tạo đợt mua sắm laptop {b05.BatchCode}", ActionTime = b05.CreatedAt },
            new() { UserId = expert.UserId, ActionType = AuditActions.CalculateAHP, TargetTable = "AHPResults", TargetId = b05.BatchId, Description = $"[Demo] Cảnh báo ma trận AHP chưa đạt tính nhất quán (CR > 0.10) tại {b05.BatchCode}", ActionTime = b05.CreatedAt.AddDays(3) },

            // B06 Timeline (Scoring In Progress)
            new() { UserId = staff.UserId, ActionType = AuditActions.CreateBatch, TargetTable = "EvaluationBatches", TargetId = b06.BatchId, NewValue = b06.BatchCode, Description = $"[Demo] Khởi tạo đợt thẩm định đối tác chuyển phát {b06.BatchCode}", ActionTime = b06.CreatedAt },
            new() { UserId = staff.UserId, ActionType = AuditActions.SaveEvaluation, TargetTable = "EvaluationScores", TargetId = b06.BatchId, Description = $"[Demo] Lưu điểm đánh giá một phần (7/12 chỉ tiêu) tại {b06.BatchCode}", ActionTime = b06.CreatedAt.AddDays(1) },

            // B07 Timeline (Draft)
            new() { UserId = staff.UserId, ActionType = AuditActions.CreateBatch, TargetTable = "EvaluationBatches", TargetId = b07.BatchId, NewValue = b07.BatchCode, Description = $"[Demo] Lưu bản nháp đợt đánh giá thiết bị chi nhánh {b07.BatchCode}", ActionTime = b07.CreatedAt },

            // B08 Timeline (Cancelled)
            new() { UserId = staff.UserId, ActionType = AuditActions.CreateBatch, TargetTable = "EvaluationBatches", TargetId = b08.BatchId, NewValue = b08.BatchCode, Description = $"[Demo] Khởi tạo đợt chào giá thiết bị lưu trữ NAS {b08.BatchCode}", ActionTime = b08.CreatedAt },
            new() { UserId = manager.UserId, ActionType = AuditActions.CancelBatch, TargetTable = "EvaluationBatches", TargetId = b08.BatchId, OldValue = "Draft", NewValue = "Cancelled", Description = $"[Demo] Hủy bỏ đợt chào giá {b08.BatchCode} theo quyết định điều chỉnh ngân sách", ActionTime = b08.CancelledAt ?? b08.CreatedAt.AddDays(3) },

            // User Logins
            new() { UserId = admin.UserId, ActionType = AuditActions.LoginSuccess, TargetTable = "Users", TargetId = admin.UserId, Description = "[Demo] Đăng nhập hệ thống thành công (demo.admin)", ActionTime = asOfDate.AddHours(-1) },
            new() { UserId = manager.UserId, ActionType = AuditActions.LoginSuccess, TargetTable = "Users", TargetId = manager.UserId, Description = "[Demo] Đăng nhập hệ thống thành công (demo.manager)", ActionTime = asOfDate.AddHours(-2) },
            new() { UserId = staff.UserId, ActionType = AuditActions.LoginSuccess, TargetTable = "Users", TargetId = staff.UserId, Description = "[Demo] Đăng nhập hệ thống thành công (demo.staff)", ActionTime = asOfDate.AddHours(-3) }
        };

        context.AuditLogs.AddRange(logs);
        await context.SaveChangesAsync();
    }
}
