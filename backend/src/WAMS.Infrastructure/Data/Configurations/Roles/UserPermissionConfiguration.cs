namespace WAMS.Infrastructure.Data.Configurations.Roles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Roles;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");

        builder.HasKey(up => new { up.UserId, up.PermissionId });

        builder.Property(up => up.UserId).HasColumnName("user_id");
        builder.Property(up => up.PermissionId).HasColumnName("permission_id");
        builder.Property(up => up.IsGranted).HasColumnName("is_granted");
        builder.Property(up => up.GrantedBy).HasColumnName("granted_by");
        builder.Property(up => up.GrantedAt).HasColumnName("granted_at");
        builder.Property(up => up.ExpiresAt).HasColumnName("expires_at");
        builder.Property(up => up.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(up => up.Constraints).HasColumnName("constraints").HasColumnType("jsonb");

        builder.HasOne(up => up.User)
            .WithMany(u => u.UserPermissions)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Permission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.GrantedByUser)
            .WithMany()
            .HasForeignKey(up => up.GrantedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(up => new { up.UserId, up.IsGranted })
            .HasDatabaseName("idx_user_permissions_user_granted");

        builder.HasIndex(up => up.ExpiresAt)
            .HasDatabaseName("idx_user_permissions_expires_at");
    }
}
