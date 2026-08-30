namespace WAMS.Infrastructure.Data.Configurations.WorkOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkOrders;

public class WorkOrderLoadingItemConfiguration : IEntityTypeConfiguration<WorkOrderLoadingItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderLoadingItem> builder)
    {
        builder.ToTable("work_order_loading_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseSerialColumn();

        builder.Property(i => i.WorkOrderId).HasColumnName("work_order_id");
        builder.Property(i => i.SpkShadowId).HasColumnName("spk_shadow_id");
        builder.Property(i => i.BlNumber).HasColumnName("bl_number").IsRequired().HasMaxLength(100);
        builder.Property(i => i.ProductName).HasColumnName("product_name").IsRequired().HasMaxLength(200);
        builder.Property(i => i.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        builder.Property(i => i.UomCode).HasColumnName("uom_code").IsRequired().HasMaxLength(50);
        builder.Property(i => i.NoVehicle).HasColumnName("no_vehicle").HasMaxLength(100);
        builder.Property(i => i.NoContainer).HasColumnName("no_container").HasMaxLength(100);
        builder.Property(i => i.NoSeal).HasColumnName("no_seal").HasMaxLength(100);
        builder.Property(i => i.GrossWeight).HasColumnName("gross_weight").HasPrecision(18, 4);
        builder.Property(i => i.FinalWeight).HasColumnName("final_weight").HasPrecision(18, 4);
        builder.Property(i => i.NettWeight).HasColumnName("nett_weight").HasPrecision(18, 4);
        builder.Property(i => i.TotalBag).HasColumnName("total_bag");
        builder.Property(i => i.UnitWeight).HasColumnName("unit_weight").HasPrecision(18, 4);
        builder.Property(i => i.IsChecked).HasColumnName("is_checked");
        builder.Property(i => i.SortOrder).HasColumnName("sort_order");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(i => i.SpkShadow)
            .WithMany()
            .HasForeignKey(i => i.SpkShadowId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
