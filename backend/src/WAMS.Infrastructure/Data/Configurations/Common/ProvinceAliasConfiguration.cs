namespace WAMS.Infrastructure.Data.Configurations.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Common;

public class ProvinceAliasConfiguration : IEntityTypeConfiguration<ProvinceAlias>
{
    public void Configure(EntityTypeBuilder<ProvinceAlias> builder)
    {
        builder.ToTable("province_aliases");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseSerialColumn();
        builder.Property(a => a.ProvinceId).HasColumnName("province_id");
        builder.Property(a => a.Alias).HasColumnName("alias").IsRequired().HasMaxLength(200);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(a => a.Alias).IsUnique().HasDatabaseName("ix_province_aliases_alias");

        builder.HasOne(a => a.Province)
            .WithMany(p => p.Aliases)
            .HasForeignKey(a => a.ProvinceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
