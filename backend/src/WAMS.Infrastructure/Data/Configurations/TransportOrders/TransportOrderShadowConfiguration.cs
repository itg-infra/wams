namespace WAMS.Infrastructure.Data.Configurations.TransportOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.TransportOrders;

public class TransportOrderShadowConfiguration : IEntityTypeConfiguration<TransportOrderShadow>
{
    public void Configure(EntityTypeBuilder<TransportOrderShadow> builder)
    {
        builder.ToTable("transport_order_shadows");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").UseIdentityColumn();

        builder.Property(t => t.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(t => t.DocNo).HasColumnName("doc_no").HasMaxLength(50).IsRequired();
        builder.Property(t => t.Type).HasColumnName("type").HasMaxLength(20).IsRequired();
        builder.Property(t => t.CardCode).HasColumnName("card_code").HasMaxLength(50).IsRequired();
        builder.Property(t => t.CardName).HasColumnName("card_name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.VehicleNo).HasColumnName("vehicle_no").HasMaxLength(50).IsRequired();
        builder.Property(t => t.VehicleType).HasColumnName("vehicle_type").HasMaxLength(50).IsRequired();
        builder.Property(t => t.BlNo).HasColumnName("bl_no").HasMaxLength(100).IsRequired();
        builder.Property(t => t.ItemCode).HasColumnName("item_code").HasMaxLength(50).IsRequired();
        builder.Property(t => t.ItemName).HasColumnName("item_name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        builder.Property(t => t.UoM).HasColumnName("uom").HasMaxLength(20).IsRequired();
        builder.Property(t => t.WhsCode).HasColumnName("whs_code").HasMaxLength(50).IsRequired();
        builder.Property(t => t.WhsName).HasColumnName("whs_name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.DocStatus).HasColumnName("doc_status").HasMaxLength(5).IsRequired();
        builder.Property(t => t.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(t => t.FirstSeenAt).HasColumnName("first_seen_at").IsRequired();
        builder.Property(t => t.SyncedAt).HasColumnName("synced_at").IsRequired();

        builder.HasOne(t => t.Company)
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // VehicleNo included: one shipment can split across trucks, so (DocNo, BlNo) alone can repeat
        builder.HasIndex(t => new { t.CompanyId, t.DocNo, t.BlNo, t.VehicleNo })
            .IsUnique()
            .HasDatabaseName("ux_transport_order_shadows_company_docno_blno_vehicleno");

        builder.HasIndex(t => new { t.CompanyId, t.Type, t.DocStatus, t.WhsCode })
            .HasDatabaseName("ix_transport_order_shadows_filter");
    }
}
