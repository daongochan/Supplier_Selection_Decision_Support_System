using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Keyless]
public partial class VwBatchCriteriaWeight
{
    [Column("BatchID")]
    public int BatchId { get; set; }

    [StringLength(20)]
    public string BatchCode { get; set; } = null!;

    [Column("CriteriaID")]
    public int CriteriaId { get; set; }

    [StringLength(20)]
    public string CriteriaCode { get; set; } = null!;

    [StringLength(150)]
    public string CriteriaName { get; set; } = null!;

    [StringLength(10)]
    public string CriteriaType { get; set; } = null!;

    [Column(TypeName = "decimal(10, 6)")]
    public decimal WeightValue { get; set; }
}
