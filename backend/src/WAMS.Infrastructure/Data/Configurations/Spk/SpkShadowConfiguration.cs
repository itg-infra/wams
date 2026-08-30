namespace WAMS.Infrastructure.Data.Configurations.Spk;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Spk;

public class SpkShadowConfiguration : IEntityTypeConfiguration<SpkShadow>
{
    public void Configure(EntityTypeBuilder<SpkShadow> builder)
    {
        builder.ToTable("spk_shadows");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseSerialColumn();

        builder.Property(s => s.CompanyId).HasColumnName("company_id");
        builder.Property(s => s.Type).HasColumnName("type").HasMaxLength(10).IsRequired();
        builder.Property(s => s.DocNo).HasColumnName("doc_no").HasMaxLength(50).IsRequired();
        builder.Property(s => s.BaseDoc).HasColumnName("base_doc").HasMaxLength(20).IsRequired();
        builder.Property(s => s.BaseDocNo).HasColumnName("base_doc_no").HasMaxLength(50).IsRequired();
        builder.Property(s => s.CardCode).HasColumnName("card_code").HasMaxLength(50).IsRequired();
        builder.Property(s => s.CardName).HasColumnName("card_name").HasMaxLength(200).IsRequired();
        builder.Property(s => s.ItemCode).HasColumnName("item_code").HasMaxLength(100).IsRequired();
        builder.Property(s => s.ItemName).HasColumnName("item_name").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        builder.Property(s => s.DeliveryQty).HasColumnName("delivery_qty").HasPrecision(18, 4);
        builder.Property(s => s.UoM).HasColumnName("uom").HasMaxLength(20).IsRequired();
        builder.Property(s => s.PackType).HasColumnName("pack_type").HasMaxLength(50).IsRequired();
        builder.Property(s => s.WhsCode).HasColumnName("whs_code").HasMaxLength(50).IsRequired();
        builder.Property(s => s.WhsName).HasColumnName("whs_name").HasMaxLength(200).IsRequired();
        builder.Property(s => s.DocStatus).HasColumnName("doc_status").HasMaxLength(5).IsRequired();
        builder.Property(s => s.BlNo).HasColumnName("bl_no").HasMaxLength(100);
        builder.Property(s => s.IsActive).HasColumnName("is_active");
        builder.Property(s => s.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(s => s.SyncedAt).HasColumnName("synced_at");

        builder.HasIndex(s => new { s.CompanyId, s.DocNo })
            .HasDatabaseName("ix_spk_shadows_company_doc_no");

        builder.HasIndex(s => new { s.CompanyId, s.WhsCode })
            .HasDatabaseName("ix_spk_shadows_company_whs");

        builder.HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
