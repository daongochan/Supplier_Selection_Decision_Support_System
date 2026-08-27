using System;

namespace SupplierSelectionDSS.Models;

public class AuditLogIndexViewModel
{
    public string? Keyword { get; set; }

    public string ActionType { get; set; } = "All";

    public int? UserId { get; set; }

    public List<AuditLogListItemViewModel> Logs { get; set; } = new();

    public List<AuditLogUserOptionViewModel> Users { get; set; } = new();
}


public class AuditLogListItemViewModel
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string ActionType { get; set; } = string.Empty;

    public string? TargetTable { get; set; }

    public int? TargetId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime ActionTime { get; set; }

    public string? Description { get; set; }
}


public class AuditLogUserOptionViewModel
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}