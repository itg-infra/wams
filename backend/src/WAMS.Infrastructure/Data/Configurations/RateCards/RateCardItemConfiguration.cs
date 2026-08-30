namespace WAMS.Infrastructure.Data.Configurations.RateCards;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.RateCards;

public class RateCardItemConfiguration : IEntityTypeConfiguration<RateCardItem>
{
    public void Configure(EntityTypeBuilder<RateCardItem> builder)
    {
        builder.ToTable("rate_card_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseSerialColumn();

        builder.Property(i => i.RateCardId).HasColumnName("rate_card_id");
        builder.Property(i => i.ItemShadowId).HasColumnName("item_shadow_id");
        builder.Property(i => i.UomMasterId).HasColumnName("uom_master_id");
        builder.Property(i => i.PpnTaxTypeId).HasColumnName("ppn_tax_type_id");
        builder.Property(i => i.PphTaxTypeId).HasColumnName("pph_tax_type_id");
        builder.Property(i => i.PpnTaxTypeCode).HasColumnName("ppn_tax_type_code").HasMaxLength(20);
        builder.Property(i => i.PphTaxTypeCode).HasColumnName("pph_tax_type_code").HasMaxLength(20);
        builder.Property(i => i.PpnRate).HasColumnName("ppn_rate").HasPrecision(5, 2);
        builder.Property(i => i.PphRate).HasColumnName("pph_rate").HasPrecision(5, 2);
        builder.Property(i => i.CostTreatment).HasColumnName("cost_treatment").HasMaxLength(20);
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        builder.Property(i => i.CostValue)
            .HasColumnName("cost_value")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(i => i.RateCard)
            .WithMany(r => r.Items)
            .HasForeignKey(i => i.RateCardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Item)
            .WithMany()
            .HasForeignKey(i => i.ItemShadowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Uom)
            .WithMany(u => u.RateCardItems)
            .HasForeignKey(i => i.UomMasterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
