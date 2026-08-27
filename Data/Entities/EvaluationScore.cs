using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Index("BatchId", Name = "IX_EvaluationScores_Batch")]
[Index("BatchId", "SupplierId", Name = "IX_EvaluationScores_Batch_Supplier")]
[Index("SupplierId", Name = "IX_EvaluationScores_Supplier")]
[Index("BatchId", "SupplierId", "CriteriaId", Name = "UQ_EvaluationScores", IsUnique = true)]
public partial class EvaluationScore
{
    [Key]
    [Column("ScoreID")]
    public int ScoreId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("SupplierID")]
    public int SupplierId { get; set; }

    [Column("CriteriaID")]
    public int CriteriaId { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal ScoreValue { get; set; }

    public int EvaluatedBy { get; set; }

    public DateTime EvaluatedAt { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("EvaluationScores")]
    public virtual EvaluationBatch Batch { get; set; } = null!;

    [ForeignKey("CriteriaId")]
    [InverseProperty("EvaluationScores")]
    public virtual Criterion Criteria { get; set; } = null!;

    [ForeignKey("EvaluatedBy")]
    [InverseProperty("EvaluationScores")]
    public virtual User EvaluatedByNavigation { get; set; } = null!;

    [ForeignKey("SupplierId")]
    [InverseProperty("EvaluationScores")]
    public virtual Supplier Supplier { get; set; } = null!;
}
