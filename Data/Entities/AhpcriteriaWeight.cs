using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Table("AHPCriteriaWeights")]
[Index("BatchId", Name = "IX_AHPCriteriaWeights_Batch")]
[Index("BatchId", "CriteriaId", Name = "UQ_AHPWeight", IsUnique = true)]
public partial class AhpcriteriaWeight
{
    [Key]
    [Column("WeightID")]
    public int WeightId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("CriteriaID")]
    public int CriteriaId { get; set; }

    [Column(TypeName = "decimal(10, 6)")]
    public decimal WeightValue { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("AhpcriteriaWeights")]
    public virtual EvaluationBatch Batch { get; set; } = null!;

    [ForeignKey("CriteriaId")]
    [InverseProperty("AhpcriteriaWeights")]
    public virtual Criterion Criteria { get; set; } = null!;
}
