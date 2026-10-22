using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Table("BatchRoundSnapshots")]
[Index("BatchId", Name = "IX_BatchRoundSnapshots_Batch")]
public partial class BatchRoundSnapshot
{
    [Key]
    [Column("SnapshotID")]
    public int SnapshotId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [StringLength(50)]
    public string SnapshotType { get; set; } = null!;

    public string SnapshotData { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("BatchRoundSnapshots")]
    public virtual EvaluationBatch Batch { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByNavigation { get; set; }
}
