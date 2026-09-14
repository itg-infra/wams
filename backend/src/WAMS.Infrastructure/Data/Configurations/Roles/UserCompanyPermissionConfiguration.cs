namespace WAMS.Infrastructure.Data.Configurations.Roles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Roles;

public class UserCompanyPermissionConfiguration : IEntityTypeConfiguration<UserCompanyPermission>
{
    public void Configure(EntityTypeBuilder<UserCompanyPermission> builder)
    {
        builder.ToTable("user_company_permissions");
        builder.HasKey(ucp => new { ucp.UserCompanyId, ucp.PermissionId });
        builder.Property(ucp => ucp.UserCompanyId).HasColumnName("user_company_id");
        builder.Property(ucp => ucp.PermissionId).HasColumnName("permission_id");
        builder.Property(ucp => ucp.IsGranted).HasColumnName("is_granted");
        builder.Property(ucp => ucp.GrantedBy).HasColumnName("granted_by");
        builder.Property(ucp => ucp.GrantedAt).HasColumnName("granted_at");
        builder.Property(ucp => ucp.ExpiresAt).HasColumnName("expires_at");
        builder.Property(ucp => ucp.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(ucp => ucp.Constraints).HasColumnName("constraints").HasColumnType("jsonb");

        builder.HasOne(ucp => ucp.UserCompany)
            .WithMany(uc => uc.Permissions)
            .HasForeignKey(ucp => ucp.UserCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ucp => ucp.Permission)
            .WithMany(p => p.UserCompanyPermissions)
            .HasForeignKey(ucp => ucp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ucp => ucp.GrantedByUser)
            .WithMany()
            .HasForeignKey(ucp => ucp.GrantedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ucp => new { ucp.UserCompanyId, ucp.IsGranted })
            .HasDatabaseName("idx_user_company_permissions_membership_granted");
        builder.HasIndex(ucp => ucp.ExpiresAt)
            .HasDatabaseName("idx_user_company_permissions_expires_at");
    }
}
