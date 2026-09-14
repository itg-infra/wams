namespace WAMS.Infrastructure.Data.Configurations.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Users;

public class UserCompanyProvinceConfiguration : IEntityTypeConfiguration<UserCompanyProvince>
{
    public void Configure(EntityTypeBuilder<UserCompanyProvince> builder)
    {
        builder.ToTable("user_company_provinces");
        builder.HasKey(ucp => new { ucp.UserCompanyId, ucp.ProvinceId });
        builder.Property(ucp => ucp.UserCompanyId).HasColumnName("user_company_id");
        builder.Property(ucp => ucp.ProvinceId).HasColumnName("province_id");

        builder.HasOne(ucp => ucp.UserCompany)
            .WithMany(uc => uc.Provinces)
            .HasForeignKey(ucp => ucp.UserCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ucp => ucp.Province)
            .WithMany()
            .HasForeignKey(ucp => ucp.ProvinceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
