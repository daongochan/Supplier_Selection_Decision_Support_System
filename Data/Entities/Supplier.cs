using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Data.Entities;

[Index("Status", Name = "IX_Suppliers_Status")]
[Index("SupplierCode", Name = "UQ_Suppliers_Code", IsUnique = true)]
public partial class Supplier
{
    [Key]
    [Column("SupplierID")]
    public int SupplierId { get; set; }

    [StringLength(20)]
    public string SupplierCode { get; set; } = null!;

    [StringLength(200)]
    public string SupplierName { get; set; } = null!;

    [StringLength(255)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(20)]
    public string? TaxCode { get; set; }

    [StringLength(255)]
    public string? Website { get; set; }

    [StringLength(150)]
    public string? BusinessField { get; set; }

    [StringLength(255)]
    public string? PaymentTerms { get; set; }

    [StringLength(15)]
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("SelectedSupplier")]
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();

    [InverseProperty("Supplier")]
    public virtual ICollection<EvaluationScore> EvaluationScores { get; set; } = new List<EvaluationScore>();

    [InverseProperty("Supplier")]
    public virtual ICollection<TopsisResult> TopsisResults { get; set; } = new List<TopsisResult>();

    [ForeignKey("SupplierId")]
    [InverseProperty("Suppliers")]
    public virtual ICollection<EvaluationBatch> Batches { get; set; } = new List<EvaluationBatch>();

    [ForeignKey("SupplierId")]
    [InverseProperty("Suppliers")]
    public virtual ICollection<ProductGroup> Groups { get; set; } = new List<ProductGroup>();
}
