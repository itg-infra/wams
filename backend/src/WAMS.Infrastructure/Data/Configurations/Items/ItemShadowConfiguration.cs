namespace WAMS.Infrastructure.Data.Configurations.Items;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Items;

public class ItemShadowConfiguration : IEntityTypeConfiguration<ItemShadow>
{
    public void Configure(EntityTypeBuilder<ItemShadow> builder)
    {
        builder.ToTable("item_shadows");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseSerialColumn();

        builder.Property(i => i.CompanyId).HasColumnName("company_id");
        builder.Property(i => i.ItemCode).HasColumnName("item_code").IsRequired().HasMaxLength(50);
        builder.Property(i => i.ItemName).HasColumnName("item_name").IsRequired().HasMaxLength(200);
        builder.Property(i => i.AcctCode).HasColumnName("acct_code").IsRequired().HasMaxLength(50);
        builder.Property(i => i.AcctName).HasColumnName("acct_name").IsRequired().HasMaxLength(200);
        builder.Property(i => i.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(i => i.SyncedAt).HasColumnName("synced_at");
        builder.Property(i => i.IsActive).HasColumnName("is_active");

        builder.HasIndex(i => new { i.CompanyId, i.ItemCode })
            .IsUnique()
            .HasDatabaseName("ix_item_shadows_company_id_item_code");

        builder.HasIndex(i => i.CompanyId)
            .HasDatabaseName("idx_item_shadows_company_id");
    }
}
