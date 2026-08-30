namespace WAMS.Infrastructure.Data.Configurations.RecapWorkOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.RecapWorkOrders;
using WAMS.Domain.Enums;

public class RecapWorkOrderConfiguration : IEntityTypeConfiguration<RecapWorkOrder>
{
    public void Configure(EntityTypeBuilder<RecapWorkOrder> builder)
    {
        builder.ToTable("recap_work_orders");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseSerialColumn();

        builder.Property(r => r.BudgetPlanId).HasColumnName("budget_plan_id");
        builder.Property(r => r.CompanyId).HasColumnName("company_id");
        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, s => RecapWorkOrderStatus.FromValue(s));
        builder.Property(r => r.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(r => r.RejectionReason).HasColumnName("rejection_reason");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => r.BudgetPlanId)
            .IsUnique()
            .HasDatabaseName("ix_recap_work_orders_budget_plan_id");

        builder.HasIndex(r => new { r.CompanyId, r.Status })
            .HasDatabaseName("idx_recap_work_orders_company_status");

        builder.HasOne(r => r.BudgetPlan)
            .WithMany()
            .HasForeignKey(r => r.BudgetPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReviewedBy)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
