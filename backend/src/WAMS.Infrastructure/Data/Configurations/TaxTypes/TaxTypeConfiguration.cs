namespace WAMS.Infrastructure.Data.Configurations.TaxTypes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Enums;

public class TaxTypeConfiguration : IEntityTypeConfiguration<TaxType>
{
    public void Configure(EntityTypeBuilder<TaxType> builder)
    {
        builder.ToTable("tax_types");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseSerialColumn();

        builder.Property(t => t.CompanyId).HasColumnName("company_id");

        builder.Property(t => t.Category)
            .HasColumnName("category")
            .HasMaxLength(10)
            .HasConversion(v => v.Value, s => TaxCategory.FromValue(s));

        builder.Property(t => t.Code).HasColumnName("code").IsRequired().HasMaxLength(20);
        builder.Property(t => t.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(t => t.Rate).HasColumnName("rate").HasPrecision(5, 2).IsRequired();
        builder.Property(t => t.IsActive).HasColumnName("is_active");
        builder.Property(t => t.SyncedAt).HasColumnName("synced_at");
        builder.Property(t => t.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");

        // Unique per company+category: SAP codes are per-company, and PPn/PPh are synced independently
        builder.HasIndex(t => new { t.CompanyId, t.Category, t.Code })
            .IsUnique()
            .HasDatabaseName("ix_tax_types_company_id_category_code");

        // Placeholder seed rows (not real SAP codes), deactivated not deleted - historical
        // snapshots reference them by code. CompanyId backfilled to real company via SQL migration.
        var seedTimestamp = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new { Id = 1L, CompanyId = 1L, Category = TaxCategory.Ppn, Code = "PPN0", Name = "No PPN", Rate = 0.00m, IsActive = false, SyncedAt = seedTimestamp, FirstSeenAt = seedTimestamp, CreatedAt = seedTimestamp, UpdatedAt = (DateTime?)null },
            new { Id = 2L, CompanyId = 1L, Category = TaxCategory.Ppn, Code = "PPN11", Name = "PPN 11%", Rate = 11.00m, IsActive = false, SyncedAt = seedTimestamp, FirstSeenAt = seedTimestamp, CreatedAt = seedTimestamp, UpdatedAt = (DateTime?)null },
            new { Id = 3L, CompanyId = 1L, Category = TaxCategory.Pph, Code = "PPH22", Name = "PPh 22 (Barang)", Rate = 1.50m, IsActive = false, SyncedAt = seedTimestamp, FirstSeenAt = seedTimestamp, CreatedAt = seedTimestamp, UpdatedAt = (DateTime?)null },
            new { Id = 4L, CompanyId = 1L, Category = TaxCategory.Pph, Code = "PPH23", Name = "PPh 23 (Jasa)", Rate = 2.00m, IsActive = false, SyncedAt = seedTimestamp, FirstSeenAt = seedTimestamp, CreatedAt = seedTimestamp, UpdatedAt = (DateTime?)null }
        );
    }
}
