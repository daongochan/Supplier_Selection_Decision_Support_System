using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Table("AHPPairwiseComparisons")]
[Index("BatchId", Name = "IX_AHPPairwise_Batch")]
[Index("BatchId", "CriteriaColId", Name = "IX_AHPPairwise_Batch_Col")]
[Index("BatchId", "CriteriaRowId", Name = "IX_AHPPairwise_Batch_Row")]
[Index("BatchId", "CriteriaRowId", "CriteriaColId", Name = "UQ_AHPComparison", IsUnique = true)]
public partial class AhppairwiseComparison
{
    [Key]
    [Column("ComparisonID")]
    public int ComparisonId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("CriteriaRowID")]
    public int CriteriaRowId { get; set; }

    [Column("CriteriaColID")]
    public int CriteriaColId { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal ComparisonValue { get; set; }

    public int EvaluatedBy { get; set; }

    public DateTime EvaluatedAt { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("AhppairwiseComparisons")]
    public virtual EvaluationBatch Batch { get; set; } = null!;

    [ForeignKey("CriteriaColId")]
    [InverseProperty("AhppairwiseComparisonCriteriaCols")]
    public virtual Criterion CriteriaCol { get; set; } = null!;

    [ForeignKey("CriteriaRowId")]
    [InverseProperty("AhppairwiseComparisonCriteriaRows")]
    public virtual Criterion CriteriaRow { get; set; } = null!;

    [ForeignKey("EvaluatedBy")]
    [InverseProperty("AhppairwiseComparisons")]
    public virtual User EvaluatedByNavigation { get; set; } = null!;
}
