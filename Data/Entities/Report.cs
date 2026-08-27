using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

public partial class Report
{
    [Key]
    [Column("ReportID")]
    public int ReportId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [StringLength(10)]
    public string ReportType { get; set; } = null!;

    public int GeneratedBy { get; set; }

    public DateTime GeneratedAt { get; set; }

    [StringLength(255)]
    public string? FilePath { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("Reports")]
    public virtual EvaluationBatch Batch { get; set; } = null!;

    [ForeignKey("GeneratedBy")]
    [InverseProperty("Reports")]
    public virtual User GeneratedByNavigation { get; set; } = null!;
}
