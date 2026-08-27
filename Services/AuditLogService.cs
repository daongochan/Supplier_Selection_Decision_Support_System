using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class AuditLogService
{
    private readonly ApplicationDbContext _context;


    // =========================================================
    // STANDARD ACTION TYPES
    // =========================================================

    public static class Actions
    {
        // User
        public const string CreateUser = AuditActions.CreateUser;
        public const string UpdateUser = AuditActions.UpdateUser;

        // Batch
        public const string CreateBatch = AuditActions.CreateBatch;
        public const string StartBatch = AuditActions.StartBatch;

        // Evaluation
        public const string SaveEvaluation = AuditActions.SaveEvaluation;

        // AHP
        public const string SaveAHP = AuditActions.SaveAHP;
        public const string CalculateAHP = AuditActions.CalculateAHP;

        // TOPSIS
        public const string CalculateTOPSIS = AuditActions.CalculateTOPSIS;

        // Decision
        public const string Approve = AuditActions.Approve;
        public const string Reject = AuditActions.Reject;
        public const string Reevaluate = AuditActions.Reevaluate;
    }


    private static readonly string[] SupportedActions =
    {
        Actions.CreateUser,
        Actions.UpdateUser,
        Actions.CreateBatch,
        Actions.StartBatch,
        Actions.SaveEvaluation,
        Actions.SaveAHP,
        Actions.CalculateAHP,
        Actions.CalculateTOPSIS,
        Actions.Approve,
        Actions.Reject,
        Actions.Reevaluate
    };


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
                    NormalizeNullable(
                        oldValue),

                NewValue =
                    NormalizeNullable(
                        newValue),

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
            int? userId)
    {
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


        var logs =
            await query
                .OrderByDescending(
                    x =>
                        x.ActionTime)
                .ThenByDescending(
                    x =>
                        x.LogId)
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
                                x.OldValue,

                            NewValue =
                                x.NewValue,

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
                users
        };
    }


    // =========================================================
    // SUPPORT
    // =========================================================

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