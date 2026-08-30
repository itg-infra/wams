namespace WAMS.Infrastructure.Data.Configurations.BudgetTemplates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.BudgetTemplates;

public class BudgetTemplateItemConfiguration : IEntityTypeConfiguration<BudgetTemplateItem>
{
    public void Configure(EntityTypeBuilder<BudgetTemplateItem> builder)
    {
        builder.ToTable("budget_template_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseSerialColumn();

        builder.Property(i => i.BudgetTemplateId).HasColumnName("budget_template_id");
        builder.Property(i => i.ItemShadowId).HasColumnName("item_shadow_id");
        builder.Property(i => i.ActivityTypeId).HasColumnName("activity_type_id");
        builder.Property(i => i.SortOrder).HasColumnName("sort_order");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(i => i.Item)
            .WithMany()
            .HasForeignKey(i => i.ItemShadowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ActivityType)
            .WithMany()
            .HasForeignKey(i => i.ActivityTypeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.BudgetTemplateId, i.ItemShadowId })
            .IsUnique()
            .HasDatabaseName("ix_budget_template_items_template_item_unique");
    }
}
