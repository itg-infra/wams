namespace WAMS.Infrastructure.Data.Configurations.Auth;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Auth;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).UseSerialColumn();

        builder.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(512);
        builder.Property(rt => rt.DeviceInfo).HasMaxLength(500);
        builder.Property(rt => rt.IpAddress).HasMaxLength(45);

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_refresh_tokens_token_hash");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("idx_refresh_tokens_user_id");

        builder.Ignore(rt => rt.IsExpired);
        builder.Ignore(rt => rt.IsRevoked);
        builder.Ignore(rt => rt.IsActive);

        builder.Property(rt => rt.CreatedAt).HasColumnName("created_at");
        builder.Property(rt => rt.UpdatedAt).HasColumnName("updated_at");
        builder.Property(rt => rt.UserId).HasColumnName("user_id");
        builder.Property(rt => rt.CompanyId).HasColumnName("company_id");
        builder.Property(rt => rt.TokenHash).HasColumnName("token_hash");
        builder.Property(rt => rt.DeviceInfo).HasColumnName("device_info");
        builder.Property(rt => rt.IpAddress).HasColumnName("ip_address");
        builder.Property(rt => rt.ExpiresAt).HasColumnName("expires_at");
        builder.Property(rt => rt.RevokedAt).HasColumnName("revoked_at");
    }
}
