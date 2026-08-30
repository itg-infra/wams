namespace WAMS.Infrastructure.Data.Configurations.BudgetPlans;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.BudgetPlans;

public class BudgetPlanSpkItemConfiguration : IEntityTypeConfiguration<BudgetPlanSpkItem>
{
    public void Configure(EntityTypeBuilder<BudgetPlanSpkItem> builder)
    {
        builder.ToTable("budget_plan_spk_items");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseSerialColumn();

        builder.Property(s => s.BudgetPlanId).HasColumnName("budget_plan_id");
        builder.Property(s => s.SpkShadowId).HasColumnName("spk_shadow_id");
        builder.Property(s => s.SortOrder).HasColumnName("sort_order");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(s => s.BudgetPlanId)
            .HasDatabaseName("idx_budget_plan_spk_items_budget_plan_id");

        builder.HasIndex(s => new { s.BudgetPlanId, s.SortOrder })
            .HasDatabaseName("idx_budget_plan_spk_items_plan_sort");

        builder.HasOne(s => s.Spk)
            .WithMany()
            .HasForeignKey(s => s.SpkShadowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
