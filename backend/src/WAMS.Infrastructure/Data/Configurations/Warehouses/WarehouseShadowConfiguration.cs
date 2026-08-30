namespace WAMS.Infrastructure.Data.Configurations.Warehouses;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Warehouses;

public class WarehouseShadowConfiguration : IEntityTypeConfiguration<WarehouseShadow>
{
    public void Configure(EntityTypeBuilder<WarehouseShadow> builder)
    {
        builder.ToTable("warehouse_shadows");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).UseSerialColumn();

        builder.Property(w => w.Code).HasColumnName("code").IsRequired().HasMaxLength(50);
        builder.Property(w => w.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(w => w.Location).HasColumnName("location").HasMaxLength(200);
        builder.Property(w => w.ProvinceId).HasColumnName("province_id");
        builder.HasOne(w => w.Province)
            .WithMany(p => p.Warehouses)
            .HasForeignKey(w => w.ProvinceId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(w => w.ProvinceId).HasDatabaseName("idx_warehouse_shadows_province_id");
        builder.Property(w => w.IsActive).HasColumnName("is_active");
        builder.Property(w => w.SyncedAt).HasColumnName("synced_at");
        builder.Property(w => w.FirstSeenAt).HasColumnName("first_seen_at");

        builder.HasIndex(w => new { w.CompanyId, w.Code })
            .IsUnique()
            .HasDatabaseName("ix_warehouse_shadows_company_id_code");

        builder.HasIndex(w => w.CompanyId)
            .HasDatabaseName("idx_warehouse_shadows_company_id");

    }
}
