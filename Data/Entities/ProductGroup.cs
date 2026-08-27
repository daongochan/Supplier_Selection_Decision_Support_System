using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Index("GroupCode", Name = "UQ_ProductGroups_Code", IsUnique = true)]
public partial class ProductGroup
{
    [Key]
    [Column("GroupID")]
    public int GroupId { get; set; }

    [StringLength(20)]
    public string GroupCode { get; set; } = null!;

    [StringLength(150)]
    public string GroupName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(15)]
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Group")]
    public virtual ICollection<EvaluationBatch> EvaluationBatches { get; set; } = new List<EvaluationBatch>();

    [ForeignKey("GroupId")]
    [InverseProperty("Groups")]
    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
