using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Index("CriteriaCode", Name = "UQ_Criteria_Code", IsUnique = true)]
public partial class Criterion
{
    [Key]
    [Column("CriteriaID")]
    public int CriteriaId { get; set; }

    [StringLength(20)]
    public string CriteriaCode { get; set; } = null!;

    [StringLength(150)]
    public string CriteriaName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(10)]
    public string CriteriaType { get; set; } = null!;

    [StringLength(30)]
    public string? Unit { get; set; }

    [StringLength(15)]
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Criteria")]
    public virtual ICollection<AhpcriteriaWeight> AhpcriteriaWeights { get; set; } = new List<AhpcriteriaWeight>();

    [InverseProperty("CriteriaCol")]
    public virtual ICollection<AhppairwiseComparison> AhppairwiseComparisonCriteriaCols { get; set; } = new List<AhppairwiseComparison>();

    [InverseProperty("CriteriaRow")]
    public virtual ICollection<AhppairwiseComparison> AhppairwiseComparisonCriteriaRows { get; set; } = new List<AhppairwiseComparison>();

    [InverseProperty("Criteria")]
    public virtual ICollection<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();

    [InverseProperty("Criteria")]
    public virtual ICollection<TopsisIdealSolution> TopsisIdealSolutions { get; set; } = new List<TopsisIdealSolution>();

    [ForeignKey("CriteriaId")]
    [InverseProperty("Criteria")]
    public virtual ICollection<EvaluationBatch> Batches { get; set; } = new List<EvaluationBatch>();
}
