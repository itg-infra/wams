namespace WAMS.Infrastructure.Data.Configurations.RateCards;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.RateCards;
using WAMS.Domain.Enums;

public class RateCardConfiguration : IEntityTypeConfiguration<RateCard>
{
    public void Configure(EntityTypeBuilder<RateCard> builder)
    {
        builder.ToTable("rate_cards");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseSerialColumn();

        builder.Property(r => r.CompanyId).HasColumnName("company_id");
        builder.Property(r => r.VendorShadowId).HasColumnName("vendor_shadow_id");
        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, s => RateCardStatus.FromValue(s));
        builder.Property(r => r.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(r => r.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("idx_rate_cards_status");

        builder.HasIndex(r => r.CompanyId)
            .HasDatabaseName("idx_rate_cards_company_id");

        builder.HasOne(r => r.Vendor)
            .WithMany()
            .HasForeignKey(r => r.VendorShadowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CreatedBy)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Items)
            .WithOne(i => i.RateCard)
            .HasForeignKey(i => i.RateCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
