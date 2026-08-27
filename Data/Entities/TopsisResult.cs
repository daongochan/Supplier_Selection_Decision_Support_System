using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Index("BatchId", "RankNo", Name = "IX_TopsisResults_Batch_Rank")]
[Index("BatchId", "ClosenessCoefficient", Name = "IX_TopsisResults_CC", IsDescending = new[] { false, true })]
[Index("BatchId", "SupplierId", Name = "UQ_TopsisResult", IsUnique = true)]
public partial class TopsisResult
{
    [Key]
    [Column("ResultID")]
    public int ResultId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("SupplierID")]
    public int SupplierId { get; set; }

    [Column("DistanceToPIS", TypeName = "decimal(18, 6)")]
    public decimal DistanceToPis { get; set; }

    [Column("DistanceToNIS", TypeName = "decimal(18, 6)")]
    public decimal DistanceToNis { get; set; }

    [Column(TypeName = "decimal(10, 6)")]
    public decimal ClosenessCoefficient { get; set; }

    public int RankNo { get; set; }

    public DateTime CalculatedAt { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("TopsisResults")]
    public virtual EvaluationBatch Batch { get; set; } = null!;

    [ForeignKey("SupplierId")]
    [InverseProperty("TopsisResults")]
    public virtual Supplier Supplier { get; set; } = null!;
}
