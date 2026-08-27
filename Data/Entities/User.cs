using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Index("Role", "Status", Name = "IX_Users_Role_Status")]
[Index("Username", Name = "UQ_Users_Username", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(30)]
    public string Role { get; set; } = null!;

    [StringLength(15)]
    public string Status { get; set; } = null!;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("EvaluatedByNavigation")]
    public virtual ICollection<AhppairwiseComparison> AhppairwiseComparisons { get; set; } = new List<AhppairwiseComparison>();

    [InverseProperty("ApprovedByNavigation")]
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    [InverseProperty("User")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<EvaluationBatch> EvaluationBatches { get; set; } = new List<EvaluationBatch>();

    [InverseProperty("EvaluatedByNavigation")]
    public virtual ICollection<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();

    [InverseProperty("GeneratedByNavigation")]
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
