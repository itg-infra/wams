namespace WAMS.Infrastructure.Data.Configurations.PurchaseOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.PurchaseOrders;
using WAMS.Domain.Enums;

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("purchase_order_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseSerialColumn();

        builder.Property(i => i.PurchaseOrderId).HasColumnName("purchase_order_id");
        builder.Property(i => i.BudgetPlanItemId).HasColumnName("budget_plan_item_id");
        builder.Property(i => i.ItemShadowId).HasColumnName("item_shadow_id");
        builder.Property(i => i.ItemCode).HasColumnName("item_code").IsRequired().HasMaxLength(100);
        builder.Property(i => i.ItemName).HasColumnName("item_name").IsRequired().HasMaxLength(200);
        builder.Property(i => i.CoaCode).HasColumnName("coa_code").IsRequired().HasMaxLength(100);
        builder.Property(i => i.CoaName).HasColumnName("coa_name").IsRequired().HasMaxLength(200);
        builder.Property(i => i.VendorShadowId).HasColumnName("vendor_shadow_id");
        builder.Property(i => i.VendorCode).HasColumnName("vendor_code").IsRequired().HasMaxLength(100);
        builder.Property(i => i.VendorName).HasColumnName("vendor_name").IsRequired().HasMaxLength(200);
        builder.Property(i => i.UomMasterId).HasColumnName("uom_master_id");
        builder.Property(i => i.UomCode).HasColumnName("uom_code").IsRequired().HasMaxLength(50);
        builder.Property(i => i.UomName).HasColumnName("uom_name").IsRequired().HasMaxLength(100);
        builder.Property(i => i.IsRfba).HasColumnName("is_rfba");
        builder.Property(i => i.BillOfLading).HasColumnName("bill_of_lading").HasMaxLength(100);
        builder.Property(i => i.CostValue).HasColumnName("cost_value").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Quantity).HasColumnName("quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(i => i.TotalValue).HasColumnName("total_value").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.SortOrder).HasColumnName("sort_order");
        builder.Property(i => i.PpnTaxTypeCode).HasColumnName("ppn_tax_type_code").HasMaxLength(20);
        builder.Property(i => i.PpnRate).HasColumnName("ppn_rate").HasPrecision(5, 2);
        builder.Property(i => i.PphTaxTypeCode).HasColumnName("pph_tax_type_code").HasMaxLength(20);
        builder.Property(i => i.PphRate).HasColumnName("pph_rate").HasPrecision(5, 2);
        builder.Property(i => i.CostTreatment).HasColumnName("cost_treatment").HasMaxLength(20);
        builder.Property(i => i.PpnAmount).HasColumnName("ppn_amount").HasPrecision(18, 2);
        builder.Property(i => i.PphAmount).HasColumnName("pph_amount").HasPrecision(18, 2);
        builder.Property(i => i.GrandTotal).HasColumnName("grand_total").HasPrecision(18, 2);
        builder.Property(i => i.PaymentStatus)
            .HasColumnName("payment_status")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, s => PurchaseOrderItemPaymentStatus.FromValue(s))
            .HasDefaultValue(PurchaseOrderItemPaymentStatus.Unpaid);
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(i => i.BudgetPlanItemId)
            .HasDatabaseName("ix_purchase_order_items_budget_plan_item_id");

        builder.HasIndex(i => i.PurchaseOrderId)
            .HasDatabaseName("ix_purchase_order_items_purchase_order_id");

        builder.HasOne(i => i.BudgetPlanItem)
            .WithMany()
            .HasForeignKey(i => i.BudgetPlanItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
