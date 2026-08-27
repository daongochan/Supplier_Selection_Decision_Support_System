using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AhpcriteriaWeight> AhpcriteriaWeights { get; set; }

    public virtual DbSet<AhppairwiseComparison> AhppairwiseComparisons { get; set; }

    public virtual DbSet<Ahpresult> Ahpresults { get; set; }

    public virtual DbSet<Approval> Approvals { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Criterion> Criteria { get; set; }

    public virtual DbSet<EvaluationBatch> EvaluationBatches { get; set; }

    public virtual DbSet<EvaluationScore> EvaluationScores { get; set; }

    public virtual DbSet<ProductGroup> ProductGroups { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<TopsisIdealSolution> TopsisIdealSolutions { get; set; }

    public virtual DbSet<TopsisResult> TopsisResults { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwBatchCriteriaWeight> VwBatchCriteriaWeights { get; set; }

    public virtual DbSet<VwBatchRanking> VwBatchRankings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<AhpcriteriaWeight>(entity =>
        {
            entity.ToTable("AHPCriteriaWeights", tb => tb.HasTrigger("TR_AHPCriteriaWeights_ValidateCriteria"));

            entity.HasOne(d => d.Batch).WithMany(p => p.AhpcriteriaWeights)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AHPWeight_Batch");

            entity.HasOne(d => d.Criteria).WithMany(p => p.AhpcriteriaWeights)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AHPWeight_Criteria");
        });

        modelBuilder.Entity<AhppairwiseComparison>(entity =>
        {
            entity.ToTable("AHPPairwiseComparisons", tb => tb.HasTrigger("TR_AHPPairwiseComparisons_ValidateCriteria"));

            entity.Property(e => e.EvaluatedAt).HasDefaultValueSql("(sysdatetime())", "DF_AHPComp_EvaluatedAt");

            entity.HasOne(d => d.Batch).WithMany(p => p.AhppairwiseComparisons)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AHPComp_Batch");

            entity.HasOne(d => d.CriteriaCol).WithMany(p => p.AhppairwiseComparisonCriteriaCols)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AHPComp_CritCol");

            entity.HasOne(d => d.CriteriaRow).WithMany(p => p.AhppairwiseComparisonCriteriaRows)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AHPComp_CritRow");

            entity.HasOne(d => d.EvaluatedByNavigation).WithMany(p => p.AhppairwiseComparisons)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AHPComp_User");
        });

        modelBuilder.Entity<Ahpresult>(entity =>
        {
            entity.Property(e => e.CalculatedAt).HasDefaultValueSql("(sysdatetime())", "DF_AHPResults_CalculatedAt");

            entity.HasOne(d => d.Batch).WithOne(p => p.Ahpresult)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AHPResults_Batch");
        });

        modelBuilder.Entity<Approval>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TR_Approvals_ValidateSupplier"));

            entity.Property(e => e.ApprovalDate).HasDefaultValueSql("(sysdatetime())", "DF_Approval_Date");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Approval_User");

            entity.HasOne(d => d.Batch).WithMany(p => p.Approvals)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Approval_Batch");

            entity.HasOne(d => d.SelectedSupplier).WithMany(p => p.Approvals).HasConstraintName("FK_Approval_Supplier");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.ActionTime).HasDefaultValueSql("(sysdatetime())", "DF_AuditLog_ActionTime");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs).HasConstraintName("FK_AuditLog_User");
        });

        modelBuilder.Entity<Criterion>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())", "DF_Criteria_CreatedAt");
            entity.Property(e => e.Status).HasDefaultValue("Active", "DF_Criteria_Status");
        });

        modelBuilder.Entity<EvaluationBatch>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())", "DF_Batch_CreatedAt");
            entity.Property(e => e.Status).HasDefaultValue("Draft", "DF_Batch_Status");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EvaluationBatches)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Batch_User");

            entity.HasOne(d => d.Group).WithMany(p => p.EvaluationBatches)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Batch_Group");

            entity.HasMany(d => d.Criteria).WithMany(p => p.Batches)
                .UsingEntity<Dictionary<string, object>>(
                    "BatchCriterion",
                    r => r.HasOne<Criterion>().WithMany()
                        .HasForeignKey("CriteriaId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .HasConstraintName("FK_BatchCriteria_Criteria"),
                    l => l.HasOne<EvaluationBatch>().WithMany()
                        .HasForeignKey("BatchId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .HasConstraintName("FK_BatchCriteria_Batch"),
                    j =>
                    {
                        j.HasKey("BatchId", "CriteriaId");
                        j.ToTable("BatchCriteria");
                        j.IndexerProperty<int>("BatchId").HasColumnName("BatchID");
                        j.IndexerProperty<int>("CriteriaId").HasColumnName("CriteriaID");
                    });

            entity.HasMany(d => d.Suppliers).WithMany(p => p.Batches)
                .UsingEntity<Dictionary<string, object>>(
                    "BatchSupplier",
                    r => r.HasOne<Supplier>().WithMany()
                        .HasForeignKey("SupplierId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .HasConstraintName("FK_BatchSuppliers_Supplier"),
                    l => l.HasOne<EvaluationBatch>().WithMany()
                        .HasForeignKey("BatchId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .HasConstraintName("FK_BatchSuppliers_Batch"),
                    j =>
                    {
                        j.HasKey("BatchId", "SupplierId");
                        j.ToTable("BatchSuppliers");
                        j.IndexerProperty<int>("BatchId").HasColumnName("BatchID");
                        j.IndexerProperty<int>("SupplierId").HasColumnName("SupplierID");
                    });
        });

        modelBuilder.Entity<EvaluationScore>(entity =>
        {
            entity.ToTable(tb =>
            {
                tb.HasTrigger("TR_EvaluationScores_ValidateCriteria");
                tb.HasTrigger("TR_EvaluationScores_ValidateSupplier");
            });

            entity.Property(e => e.EvaluatedAt).HasDefaultValueSql("(sysdatetime())", "DF_Score_EvaluatedAt");

            entity.HasOne(d => d.Batch).WithMany(p => p.EvaluationScores)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Score_Batch");

            entity.HasOne(d => d.Criteria).WithMany(p => p.EvaluationScores)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Score_Criteria");

            entity.HasOne(d => d.EvaluatedByNavigation).WithMany(p => p.EvaluationScores)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Score_User");

            entity.HasOne(d => d.Supplier).WithMany(p => p.EvaluationScores)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Score_Supplier");
        });

        modelBuilder.Entity<ProductGroup>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())", "DF_ProductGroups_CreatedAt");
            entity.Property(e => e.Status).HasDefaultValue("Active", "DF_ProductGroups_Status");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.Property(e => e.GeneratedAt).HasDefaultValueSql("(sysdatetime())", "DF_Report_GeneratedAt");

            entity.HasOne(d => d.Batch).WithMany(p => p.Reports)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Report_Batch");

            entity.HasOne(d => d.GeneratedByNavigation).WithMany(p => p.Reports)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Report_User");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())", "DF_Suppliers_CreatedAt");
            entity.Property(e => e.Status).HasDefaultValue("Active", "DF_Suppliers_Status");

            entity.HasMany(d => d.Groups).WithMany(p => p.Suppliers)
                .UsingEntity<Dictionary<string, object>>(
                    "SupplierGroup",
                    r => r.HasOne<ProductGroup>().WithMany()
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .HasConstraintName("FK_SupplierGroups_Group"),
                    l => l.HasOne<Supplier>().WithMany()
                        .HasForeignKey("SupplierId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .HasConstraintName("FK_SupplierGroups_Supplier"),
                    j =>
                    {
                        j.HasKey("SupplierId", "GroupId");
                        j.ToTable("SupplierGroups");
                        j.IndexerProperty<int>("SupplierId").HasColumnName("SupplierID");
                        j.IndexerProperty<int>("GroupId").HasColumnName("GroupID");
                    });
        });

        modelBuilder.Entity<TopsisIdealSolution>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TR_TopsisIdealSolutions_ValidateCriteria"));

            entity.HasOne(d => d.Batch).WithMany(p => p.TopsisIdealSolutions)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Ideal_Batch");

            entity.HasOne(d => d.Criteria).WithMany(p => p.TopsisIdealSolutions)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Ideal_Criteria");
        });

        modelBuilder.Entity<TopsisResult>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("TR_TopsisResults_ValidateSupplier"));

            entity.Property(e => e.CalculatedAt).HasDefaultValueSql("(sysdatetime())", "DF_Topsis_CalculatedAt");

            entity.HasOne(d => d.Batch).WithMany(p => p.TopsisResults)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Topsis_Batch");

            entity.HasOne(d => d.Supplier).WithMany(p => p.TopsisResults)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Topsis_Supplier");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())", "DF_Users_CreatedAt");
            entity.Property(e => e.Status).HasDefaultValue("Active", "DF_Users_Status");
        });

        modelBuilder.Entity<VwBatchCriteriaWeight>(entity =>
        {
            entity.ToView("vw_BatchCriteriaWeights");
        });

        modelBuilder.Entity<VwBatchRanking>(entity =>
        {
            entity.ToView("vw_BatchRanking");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}