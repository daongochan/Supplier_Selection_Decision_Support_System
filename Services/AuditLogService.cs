using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class AuditLogService
{
    private readonly ApplicationDbContext _context;


    public static class Actions
    {
        public const string CreateUser = AuditActions.CreateUser;
        public const string UpdateUser = AuditActions.UpdateUser;
        public const string CreateBatch = AuditActions.CreateBatch;
        public const string StartBatch = AuditActions.StartBatch;
        public const string CancelBatch = AuditActions.CancelBatch;
        public const string SaveEvaluation = AuditActions.SaveEvaluation;
        public const string SaveAHP = AuditActions.SaveAHP;
        public const string CalculateAHP = AuditActions.CalculateAHP;
        public const string CalculateTOPSIS = AuditActions.CalculateTOPSIS;
        public const string Approve = AuditActions.Approve;
        public const string Reject = AuditActions.Reject;
        public const string Reevaluate = AuditActions.Reevaluate;
        public const string GeneratePdf = AuditActions.GeneratePdf;
        public const string GenerateExcel = AuditActions.GenerateExcel;
        public const string DownloadPdf = AuditActions.DownloadPdf;
        public const string DownloadExcel = AuditActions.DownloadExcel;
    }

    private static readonly string[] SupportedActions = AuditActions.All;


    public AuditLogService(
        ApplicationDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // WRITE LOG
    // =========================================================

    public async Task LogAsync(
        int? userId,
        string actionType,
        string? targetTable = null,
        int? targetId = null,
        string? oldValue = null,
        string? newValue = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(actionType))
        {
            throw new ArgumentException(
                "ActionType không được để trống.",
                nameof(actionType));
        }


        var normalizedAction =
            actionType.Trim();


        var log =
            new AuditLog
            {
                UserId =
                    userId,

                ActionType =
                    normalizedAction,

                TargetTable =
                    NormalizeNullable(
                        targetTable),

                TargetId =
                    targetId,

                OldValue =
                    SanitizeSensitiveData(
                        NormalizeNullable(
                            oldValue)),

                NewValue =
                    SanitizeSensitiveData(
                        NormalizeNullable(
                            newValue)),

                ActionTime =
                    DateTime.UtcNow,

                Description =
                    NormalizeNullable(
                        description)
            };


        _context.AuditLogs.Add(
            log);


        await _context.SaveChangesAsync();
    }


    // =========================================================
    // SEARCH
    // =========================================================

    public async Task<AuditLogIndexViewModel>
        SearchAsync(
            string? keyword,
            string? actionType,
            int? userId,
            int pageNumber = 1,
            int pageSize = 15)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        keyword =
            keyword?.Trim();


        actionType =
            string.IsNullOrWhiteSpace(
                actionType)
                    ? "All"
                    : actionType.Trim();


        var query =
            _context.AuditLogs
                .AsNoTracking()
                .Include(
                    x =>
                        x.User)
                .AsQueryable();


        // =====================================================
        // KEYWORD
        // =====================================================

        if (!string.IsNullOrWhiteSpace(
                keyword))
        {
            query =
                query.Where(
                    x =>
                        (
                            x.Description != null &&
                            x.Description.Contains(
                                keyword)
                        )
                        ||
                        (
                            x.TargetTable != null &&
                            x.TargetTable.Contains(
                                keyword)
                        )
                        ||
                        (
                            x.ActionType.Contains(
                                keyword)
                        )
                        ||
                        (
                            x.TargetId.HasValue &&
                            x.TargetId.Value
                                .ToString()
                                .Contains(
                                    keyword)
                        )
                        ||
                        (
                            x.User != null &&
                            (
                                x.User.Username.Contains(
                                    keyword)
                                ||
                                x.User.FullName.Contains(
                                    keyword)
                            )
                        ));
        }


        // =====================================================
        // ACTION
        // =====================================================

        if (
            actionType != "All" &&
            SupportedActions.Contains(
                actionType))
        {
            query =
                query.Where(
                    x =>
                        x.ActionType ==
                        actionType);
        }


        // =====================================================
        // USER
        // =====================================================

        if (userId.HasValue &&
            userId.Value > 0)
        {
            query =
                query.Where(
                    x =>
                        x.UserId ==
                        userId.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && pageNumber > totalPages)
        {
            pageNumber = totalPages;
        }
        else if (totalPages == 0)
        {
            pageNumber = 1;
        }

        var skipLong = ((long)pageNumber - 1L) * pageSize;
        var skip = (int)Math.Clamp(skipLong, 0L, int.MaxValue);

        var logs = totalCount == 0
            ? new List<AuditLogListItemViewModel>()
            : await query
                .OrderByDescending(
                    x =>
                        x.ActionTime)
                .ThenByDescending(
                    x =>
                        x.LogId)
                .Skip(skip)
                .Take(pageSize)
                .Select(
                    x =>
                        new AuditLogListItemViewModel
                        {
                            LogId =
                                x.LogId,

                            UserId =
                                x.UserId,

                            Username =
                                x.User != null
                                    ? x.User.Username
                                    : "System",

                            FullName =
                                x.User != null
                                    ? x.User.FullName
                                    : "System",

                            ActionType =
                                x.ActionType,

                            TargetTable =
                                x.TargetTable,

                            TargetId =
                                x.TargetId,

                            OldValue =
                                SanitizeSensitiveData(x.OldValue),

                            NewValue =
                                SanitizeSensitiveData(x.NewValue),

                            ActionTime =
                                x.ActionTime,

                            Description =
                                x.Description
                        })
                .ToListAsync();


        var users =
            await _context.Users
                .AsNoTracking()
                .OrderBy(
                    x =>
                        x.FullName)
                .ThenBy(
                    x =>
                        x.Username)
                .Select(
                    x =>
                        new AuditLogUserOptionViewModel
                        {
                            UserId =
                                x.UserId,

                            Username =
                                x.Username,

                            FullName =
                                x.FullName
                        })
                .ToListAsync();


        return new AuditLogIndexViewModel
        {
            Keyword =
                keyword,

            ActionType =
                actionType,

            UserId =
                userId,

            Logs =
                logs,

            Users =
                users,

            PageNumber =
                pageNumber,

            PageSize =
                pageSize,

            TotalCount =
                totalCount,

            TotalPages =
                totalPages
        };
    }


    // =========================================================
    // KPI SUMMARY
    // =========================================================

    public async Task<(int TotalLogs, int TodayLogs, int ActiveUsers, int CriticalActions)> GetKpiSummaryAsync()
    {
        var todayUtc = DateTime.UtcNow.Date;
        var total = await _context.AuditLogs.CountAsync();
        var today = await _context.AuditLogs.CountAsync(x => x.ActionTime >= todayUtc);
        var activeUsers = await _context.AuditLogs
            .Where(x => x.UserId.HasValue)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync();

        var criticalActions = await _context.AuditLogs.CountAsync(x =>
            x.ActionType == AuditActions.Approve ||
            x.ActionType == AuditActions.Reject ||
            x.ActionType == AuditActions.CancelBatch ||
            x.ActionType == AuditActions.CalculateTOPSIS ||
            x.ActionType == AuditActions.CalculateAHP);

        return (total, today, activeUsers, criticalActions);
    }


    // =========================================================
    // SUPPORT
    // =========================================================

    private static readonly Regex SensitiveJsonRegex = new(
        @"""(?<key>(?i:password|passwordhash|token|secret|jwt|apikey))""\s*:\s*""[^""]*""",
        RegexOptions.Compiled);

    public static string? SanitizeSensitiveData(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return SensitiveJsonRegex.Replace(value, @"""${key}"": ""********""");
    }

    private static string? NormalizeNullable(
        string? value)
    {
        var normalized =
            value?.Trim();


        return string.IsNullOrWhiteSpace(
            normalized)
                ? null
                : normalized;
    }
}
