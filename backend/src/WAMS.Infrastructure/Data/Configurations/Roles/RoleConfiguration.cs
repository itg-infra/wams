namespace WAMS.Infrastructure.Data.Configurations.Roles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Roles;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseSerialColumn();

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.DisplayName).HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("idx_roles_name");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.IsSystem).HasColumnName("is_system");
        builder.Property(r => r.GlobalAccess).HasColumnName("global_access");
        builder.Property(r => r.DisplayName).HasColumnName("display_name");

        // CompanyId is nullable - system roles have null CompanyId (global roles)
        builder.HasOne(r => r.Company)
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
