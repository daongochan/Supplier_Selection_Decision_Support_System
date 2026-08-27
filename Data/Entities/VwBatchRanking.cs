using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Keyless]
public partial class VwBatchRanking
{
    [Column("BatchID")]
    public int BatchId { get; set; }

    [StringLength(20)]
    public string BatchCode { get; set; } = null!;

    [StringLength(200)]
    public string BatchName { get; set; } = null!;

    [StringLength(20)]
    public string BatchStatus { get; set; } = null!;

    [Column("SupplierID")]
    public int SupplierId { get; set; }

    [StringLength(20)]
    public string SupplierCode { get; set; } = null!;

    [StringLength(200)]
    public string SupplierName { get; set; } = null!;

    [Column("DistanceToPIS", TypeName = "decimal(18, 6)")]
    public decimal DistanceToPis { get; set; }

    [Column("DistanceToNIS", TypeName = "decimal(18, 6)")]
    public decimal DistanceToNis { get; set; }

    [Column(TypeName = "decimal(10, 6)")]
    public decimal ClosenessCoefficient { get; set; }

    public int RankNo { get; set; }

    public DateTime CalculatedAt { get; set; }
}
