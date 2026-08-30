namespace WAMS.Infrastructure.Data.Configurations.BudgetPlans;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Enums;

public class BudgetPlanItemConfiguration : IEntityTypeConfiguration<BudgetPlanItem>
{
    public void Configure(EntityTypeBuilder<BudgetPlanItem> builder)
    {
        builder.ToTable("budget_plan_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseSerialColumn();

        builder.Property(i => i.BudgetPlanId).HasColumnName("budget_plan_id");
        builder.Property(i => i.ItemShadowId).HasColumnName("item_shadow_id");
        builder.Property(i => i.ActivityTypeId).HasColumnName("activity_type_id");
        builder.Property(i => i.VendorShadowId).HasColumnName("vendor_shadow_id");
        builder.Property(i => i.UomMasterId).HasColumnName("uom_master_id");
        builder.Property(i => i.Type)
            .HasColumnName("type")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, s => BudgetPlanType.FromValue(s));
        builder.Property(i => i.IsRfba).HasColumnName("is_rfba");
        builder.Property(i => i.CostValue).HasColumnName("cost_value").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Quantity).HasColumnName("quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(i => i.TotalValue).HasColumnName("total_value").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.PpnTaxTypeCode).HasColumnName("ppn_tax_type_code").HasMaxLength(20);
        builder.Property(i => i.PpnRate).HasColumnName("ppn_rate").HasPrecision(5, 2);
        builder.Property(i => i.PphTaxTypeCode).HasColumnName("pph_tax_type_code").HasMaxLength(20);
        builder.Property(i => i.PphRate).HasColumnName("pph_rate").HasPrecision(5, 2);
        builder.Property(i => i.CostTreatment).HasColumnName("cost_treatment").HasMaxLength(20);
        builder.Property(i => i.PpnAmount).HasColumnName("ppn_amount").HasPrecision(18, 2);
        builder.Property(i => i.PphAmount).HasColumnName("pph_amount").HasPrecision(18, 2);
        builder.Property(i => i.GrandTotal).HasColumnName("grand_total").HasPrecision(18, 2);
        builder.Property(i => i.SortOrder).HasColumnName("sort_order");
        builder.Property(i => i.DocExternal).HasColumnName("doc_external").HasMaxLength(100);
        builder.Property(i => i.BillOfLading).HasColumnName("bill_of_lading").HasMaxLength(100);
        builder.Property(i => i.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(i => i.SpkShadowId).HasColumnName("spk_shadow_id");
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

        builder.HasOne(i => i.Vendor)
            .WithMany()
            .HasForeignKey(i => i.VendorShadowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Uom)
            .WithMany()
            .HasForeignKey(i => i.UomMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Spk)
            .WithMany()
            .HasForeignKey(i => i.SpkShadowId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(i => new { i.BudgetPlanId, i.SortOrder })
            .HasDatabaseName("ix_budget_plan_items_budget_plan_sort_order");

        builder.HasIndex(i => new { i.BudgetPlanId, i.VendorShadowId })
            .HasDatabaseName("ix_budget_plan_items_budget_plan_vendor");

        // Partial index: most cost items have no SPK link; only index populated rows.
        builder.HasIndex(i => i.SpkShadowId)
            .HasDatabaseName("ix_budget_plan_items_spk_shadow_id")
            .HasFilter("spk_shadow_id IS NOT NULL");
    }
}
