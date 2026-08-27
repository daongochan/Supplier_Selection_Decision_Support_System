using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

public partial class Approval
{
    [Key]
    [Column("ApprovalID")]
    public int ApprovalId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("SelectedSupplierID")]
    public int? SelectedSupplierId { get; set; }

    public int ApprovedBy { get; set; }

    public DateTime ApprovalDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(25)]
    public string Status { get; set; } = null!;

    public bool IsFinal { get; set; }

    [ForeignKey("ApprovedBy")]
    [InverseProperty("Approvals")]
    public virtual User ApprovedByNavigation { get; set; } = null!;

    [ForeignKey("BatchId")]
    [InverseProperty("Approvals")]
    public virtual EvaluationBatch Batch { get; set; } = null!;

    [ForeignKey("SelectedSupplierId")]
    [InverseProperty("Approvals")]
    public virtual Supplier? SelectedSupplier { get; set; }
}
