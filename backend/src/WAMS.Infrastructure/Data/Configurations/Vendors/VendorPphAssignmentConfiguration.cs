namespace WAMS.Infrastructure.Data.Configurations.Vendors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.Vendors;

public class VendorPphAssignmentConfiguration : IEntityTypeConfiguration<VendorPphAssignment>
{
    public void Configure(EntityTypeBuilder<VendorPphAssignment> builder)
    {
        builder.ToTable("vendor_pph_assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseSerialColumn();

        builder.Property(a => a.VendorShadowId).HasColumnName("vendor_shadow_id");
        builder.Property(a => a.TaxTypeId).HasColumnName("tax_type_id");
        builder.Property(a => a.IsActive).HasColumnName("is_active");
        builder.Property(a => a.SyncedAt).HasColumnName("synced_at");
        builder.Property(a => a.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(a => a.Vendor)
            .WithMany()
            .HasForeignKey(a => a.VendorShadowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.TaxType)
            .WithMany()
            .HasForeignKey(a => a.TaxTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.VendorShadowId, a.TaxTypeId })
            .IsUnique()
            .HasDatabaseName("ix_vendor_pph_assignments_vendor_tax_type");
    }
}
