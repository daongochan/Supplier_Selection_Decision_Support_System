using System;

namespace SupplierSelectionDSS.Models;

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
