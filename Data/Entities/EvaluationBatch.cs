using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Index("GroupId", Name = "IX_EvaluationBatches_Group")]
[Index("Status", Name = "IX_EvaluationBatches_Status")]
[Index("BatchCode", Name = "UQ_Batch_Code", IsUnique = true)]
public partial class EvaluationBatch
{
    [Key]
    [Column("BatchID")]
    public int BatchId { get; set; }

    [StringLength(20)]
    public string BatchCode { get; set; } = null!;

    [StringLength(200)]
    public string BatchName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Column("GroupID")]
    public int GroupId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [InverseProperty("Batch")]
    public virtual ICollection<AhpcriteriaWeight> AhpcriteriaWeights { get; set; } = new List<AhpcriteriaWeight>();

    [InverseProperty("Batch")]
    public virtual ICollection<AhppairwiseComparison> AhppairwiseComparisons { get; set; } = new List<AhppairwiseComparison>();

    [InverseProperty("Batch")]
    public virtual Ahpresult? Ahpresult { get; set; }

    [InverseProperty("Batch")]
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("EvaluationBatches")]
    public virtual User CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Batch")]
    public virtual ICollection<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();

    [ForeignKey("GroupId")]
    [InverseProperty("EvaluationBatches")]
    public virtual ProductGroup Group { get; set; } = null!;

    [InverseProperty("Batch")]
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    [InverseProperty("Batch")]
    public virtual ICollection<TopsisIdealSolution> TopsisIdealSolutions { get; set; } = new List<TopsisIdealSolution>();

    [InverseProperty("Batch")]
    public virtual ICollection<TopsisResult> TopsisResults { get; set; } = new List<TopsisResult>();

    [ForeignKey("BatchId")]
    [InverseProperty("Batches")]
    public virtual ICollection<Criterion> Criteria { get; set; } = new List<Criterion>();

    [ForeignKey("BatchId")]
    [InverseProperty("Batches")]
    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
