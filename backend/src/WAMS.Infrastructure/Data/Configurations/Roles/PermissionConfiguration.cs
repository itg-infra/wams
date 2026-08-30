namespace WAMS.Infrastructure.Data.Configurations.Roles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Roles;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseSerialColumn();

        builder.Property(p => p.Module).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Resource).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Action).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(500);

        builder.HasIndex(p => new { p.Module, p.Resource, p.Action })
            .IsUnique()
            .HasDatabaseName("idx_permissions_module_resource_action");

        builder.Ignore(p => p.FullKey);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
    }
}
