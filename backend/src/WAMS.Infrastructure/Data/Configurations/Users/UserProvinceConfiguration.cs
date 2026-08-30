namespace WAMS.Infrastructure.Data.Configurations.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Users;

public class UserProvinceConfiguration : IEntityTypeConfiguration<UserProvince>
{
    public void Configure(EntityTypeBuilder<UserProvince> builder)
    {
        builder.ToTable("user_provinces");
        builder.HasKey(up => new { up.UserId, up.ProvinceId });
        builder.Property(up => up.UserId).HasColumnName("user_id");
        builder.Property(up => up.ProvinceId).HasColumnName("province_id");

        builder.HasOne(up => up.Province)
            .WithMany()
            .HasForeignKey(up => up.ProvinceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.User)
            .WithMany(u => u.UserProvinces)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
