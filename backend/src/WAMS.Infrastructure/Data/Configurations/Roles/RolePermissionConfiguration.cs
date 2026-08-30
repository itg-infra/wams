namespace WAMS.Infrastructure.Data.Configurations.Roles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Roles;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Property(rp => rp.RoleId).HasColumnName("role_id");
        builder.Property(rp => rp.PermissionId).HasColumnName("permission_id");
        builder.Property(rp => rp.GrantedAt).HasColumnName("granted_at");
        builder.Property(rp => rp.GrantedBy).HasColumnName("granted_by");
        builder.Property(rp => rp.Constraints).HasColumnName("constraints").HasColumnType("jsonb");

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
