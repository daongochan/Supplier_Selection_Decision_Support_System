using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Table("AHPResults")]
[Index("BatchId", Name = "UQ_AHPResults_Batch", IsUnique = true)]
public partial class Ahpresult
{
    [Key]
    [Column("AHPResultID")]
    public int AhpresultId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column(TypeName = "decimal(10, 6)")]
    public decimal LambdaMax { get; set; }

    [Column("CIValue", TypeName = "decimal(10, 6)")]
    public decimal Civalue { get; set; }

    [Column("RIValue", TypeName = "decimal(10, 6)")]
    public decimal Rivalue { get; set; }

    [Column("CRValue", TypeName = "decimal(10, 6)")]
    public decimal Crvalue { get; set; }

    public bool IsConsistent { get; set; }

    [Column("n")]
    public int? N { get; set; }

    public DateTime CalculatedAt { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("Ahpresult")]
    public virtual EvaluationBatch Batch { get; set; } = null!;
}
