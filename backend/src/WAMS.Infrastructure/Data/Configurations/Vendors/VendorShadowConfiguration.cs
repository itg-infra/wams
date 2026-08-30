namespace WAMS.Infrastructure.Data.Configurations.Vendors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Vendors;

public class VendorShadowConfiguration : IEntityTypeConfiguration<VendorShadow>
{
    public void Configure(EntityTypeBuilder<VendorShadow> builder)
    {
        builder.ToTable("vendor_shadows");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).UseSerialColumn();

        builder.Property(v => v.CompanyId).HasColumnName("company_id");
        builder.Property(v => v.CardCode).HasColumnName("card_code").IsRequired().HasMaxLength(50);
        builder.Property(v => v.CardName).HasColumnName("card_name").IsRequired().HasMaxLength(200);
        builder.Property(v => v.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(v => v.SyncedAt).HasColumnName("synced_at");
        builder.Property(v => v.IsActive).HasColumnName("is_active");

        builder.HasIndex(v => new { v.CompanyId, v.CardCode })
            .IsUnique()
            .HasDatabaseName("ix_vendor_shadows_company_id_card_code");

        builder.HasIndex(v => v.CompanyId)
            .HasDatabaseName("idx_vendor_shadows_company_id");
    }
}
