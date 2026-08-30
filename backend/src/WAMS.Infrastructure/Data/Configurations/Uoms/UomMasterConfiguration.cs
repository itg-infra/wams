namespace WAMS.Infrastructure.Data.Configurations.Uoms;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Uoms;

public class UomMasterConfiguration : IEntityTypeConfiguration<UomMaster>
{
    public void Configure(EntityTypeBuilder<UomMaster> builder)
    {
        builder.ToTable("uom_masters");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).UseSerialColumn();

        builder.Property(u => u.Code).HasColumnName("code").IsRequired().HasMaxLength(20);
        builder.Property(u => u.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(u => u.IsActive).HasColumnName("is_active");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");

        // Global unique - no CompanyId
        builder.HasIndex(u => u.Code)
            .IsUnique()
            .HasDatabaseName("ix_uom_masters_code");
    }
}
