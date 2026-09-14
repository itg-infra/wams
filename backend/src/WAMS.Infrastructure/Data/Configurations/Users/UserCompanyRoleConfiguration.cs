namespace WAMS.Infrastructure.Data.Configurations.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;

public class UserCompanyRoleConfiguration : IEntityTypeConfiguration<UserCompanyRole>
{
    public void Configure(EntityTypeBuilder<UserCompanyRole> builder)
    {
        builder.ToTable("user_company_roles");
        builder.HasKey(ucr => new { ucr.UserCompanyId, ucr.RoleId });
        builder.Property(ucr => ucr.UserCompanyId).HasColumnName("user_company_id");
        builder.Property(ucr => ucr.RoleId).HasColumnName("role_id");
        builder.Property(ucr => ucr.ExpiresAt).HasColumnName("expires_at");
        builder.Property(ucr => ucr.AssignedAt).HasColumnName("assigned_at");

        builder.HasOne(ucr => ucr.UserCompany)
            .WithMany(uc => uc.Roles)
            .HasForeignKey(ucr => ucr.UserCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ucr => ucr.Role)
            .WithMany(r => r.UserCompanyRoles)
            .HasForeignKey(ucr => ucr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
