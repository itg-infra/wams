namespace WAMS.Infrastructure.Data.Configurations.BudgetPlans;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Enums;

public class BudgetPlanConfiguration : IEntityTypeConfiguration<BudgetPlan>
{
    public void Configure(EntityTypeBuilder<BudgetPlan> builder)
    {
        builder.ToTable("budget_plans");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).UseSerialColumn();

        builder.Property(b => b.Code).HasColumnName("code").IsRequired().HasMaxLength(20);
        builder.Property(b => b.CompanyId).HasColumnName("company_id");
        builder.Property(b => b.BudgetTemplateId).HasColumnName("budget_template_id");
        builder.Property(b => b.WarehouseShadowId).HasColumnName("warehouse_shadow_id");
        builder.Property(b => b.Remark).HasColumnName("remark").HasMaxLength(500);
        builder.Property(b => b.DocDate).HasColumnName("doc_date");
        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, s => BudgetPlanStatus.FromValue(s));
        builder.Property(b => b.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(b => b.SubmittedByUserId).HasColumnName("submitted_by_user_id");
        builder.Property(b => b.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(b => b.WorkflowInstanceId).HasColumnName("workflow_instance_id");
        builder.Property(b => b.RejectedByUserId).HasColumnName("rejected_by_user_id");
        builder.Property(b => b.RejectedAt).HasColumnName("rejected_at");
        builder.Property(b => b.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(b => b.Code)
            .IsUnique()
            .HasDatabaseName("ix_budget_plans_code");

        builder.HasIndex(b => new { b.CompanyId, b.Status })
            .HasDatabaseName("ix_budget_plans_company_status");

        builder.HasIndex(b => new { b.CompanyId, b.CreatedAt })
            .HasDatabaseName("ix_budget_plans_company_created_at");

        builder.HasIndex(b => b.WorkflowInstanceId)
            .HasDatabaseName("ix_budget_plans_workflow_instance_id");

        builder.HasIndex(b => b.DocDate)
            .HasDatabaseName("ix_budget_plans_doc_date");

        // Supports GetOverdueForReminderAsync: filters by status + submitted_at
        builder.HasIndex(b => new { b.Status, b.SubmittedAt })
            .HasDatabaseName("ix_budget_plans_status_submitted_at");

        builder.HasIndex(b => new { b.Status, b.DeletedAt, b.WorkflowInstanceId, b.SubmittedAt })
            .HasDatabaseName("ix_budget_plans_status_deleted_workflow_submitted");

        builder.HasIndex(b => b.WarehouseShadowId)
            .HasDatabaseName("ix_budget_plans_warehouse_shadow_id");

        builder.HasOne(b => b.Company)
            .WithMany()
            .HasForeignKey(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.BudgetTemplate)
            .WithMany()
            .HasForeignKey(b => b.BudgetTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Warehouse)
            .WithMany()
            .HasForeignKey(b => b.WarehouseShadowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.CreatedBy)
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.SubmittedBy)
            .WithMany()
            .HasForeignKey(b => b.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.WorkflowInstance)
            .WithMany()
            .HasForeignKey(b => b.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.RejectedBy)
            .WithMany()
            .HasForeignKey(b => b.RejectedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Items)
            .WithOne(i => i.BudgetPlan)
            .HasForeignKey(i => i.BudgetPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.WorkOrders)
            .WithOne(w => w.BudgetPlan)
            .HasForeignKey(w => w.BudgetPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
