using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Index("BatchId", Name = "IX_TopsisIdeal_Batch")]
[Index("CriteriaId", Name = "IX_TopsisIdeal_Criteria")]
[Index("BatchId", "CriteriaId", Name = "UQ_TopsisIdeal", IsUnique = true)]
public partial class TopsisIdealSolution
{
    [Key]
    [Column("IdealID")]
    public int IdealId { get; set; }

    [Column("BatchID")]
    public int BatchId { get; set; }

    [Column("CriteriaID")]
    public int CriteriaId { get; set; }

    [Column("PISValue", TypeName = "decimal(18, 6)")]
    public decimal Pisvalue { get; set; }

    [Column("NISValue", TypeName = "decimal(18, 6)")]
    public decimal Nisvalue { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("TopsisIdealSolutions")]
    public virtual EvaluationBatch Batch { get; set; } = null!;

    [ForeignKey("CriteriaId")]
    [InverseProperty("TopsisIdealSolutions")]
    public virtual Criterion Criteria { get; set; } = null!;
}
