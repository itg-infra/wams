namespace WAMS.Infrastructure.Data.Configurations.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Common;

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("provinces");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseSerialColumn();
        builder.Property(p => p.Code).HasColumnName("code").IsRequired().HasMaxLength(20);
        builder.Property(p => p.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(p => p.Display).HasColumnName("display").IsRequired().HasMaxLength(200);
        builder.Property(p => p.IsActive).HasColumnName("is_active");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => p.Code).IsUnique().HasDatabaseName("ix_provinces_code");
        builder.HasIndex(p => p.Name).IsUnique().HasDatabaseName("ix_provinces_name");
    }
}
