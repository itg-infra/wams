namespace WAMS.Infrastructure.Data.Configurations.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Users;

public class UserCompanyConfiguration : IEntityTypeConfiguration<UserCompany>
{
    public void Configure(EntityTypeBuilder<UserCompany> builder)
    {
        builder.ToTable("user_companies");

        builder.HasKey(uc => uc.Id);
        builder.Property(uc => uc.Id).UseSerialColumn();
        builder.Property(uc => uc.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(uc => uc.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(uc => uc.AuthorizationVersion)
            .HasColumnName("authorization_version")
            .HasDefaultValue(0)
            .IsRequired();
        builder.Property(uc => uc.CreatedBy).HasColumnName("created_by");
        builder.Property(uc => uc.RemovedAt).HasColumnName("removed_at");
        builder.Property(uc => uc.CreatedAt).HasColumnName("created_at");
        builder.Property(uc => uc.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(uc => new { uc.UserId, uc.CompanyId })
            .IsUnique()
            .HasFilter("removed_at IS NULL")
            .HasDatabaseName("idx_user_companies_user_company_live");

        builder.HasOne(uc => uc.User)
            .WithMany(u => u.UserCompanies)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uc => uc.Company)
            .WithMany(c => c.UserCompanies)
            .HasForeignKey(uc => uc.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
