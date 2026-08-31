namespace WAMS.Infrastructure.Data.Configurations.PurchaseOrders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.PurchaseOrders;
using WAMS.Domain.Enums;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseSerialColumn();

        builder.Property(p => p.Code).HasColumnName("code").IsRequired().HasMaxLength(20);
        builder.Property(p => p.CompanyId).HasColumnName("company_id");
        builder.Property(p => p.VendorShadowId).HasColumnName("vendor_shadow_id");
        builder.Property(p => p.Remark).HasColumnName("remark").HasMaxLength(500);
        builder.Property(p => p.DocDate).HasColumnName("doc_date");
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, s => PurchaseOrderStatus.FromValue(s));
        builder.Property(p => p.SapPoNumber).HasColumnName("sap_po_number").HasMaxLength(100);
        builder.Property(p => p.SapDocEntry).HasColumnName("sap_doc_entry");
        builder.Property(p => p.SapApdpDocEntry).HasColumnName("sap_apdp_doc_entry");
        builder.Property(p => p.SapApdpGeneratedAt).HasColumnName("sap_apdp_generated_at");
        builder.Property(p => p.SapApdpError).HasColumnName("sap_apdp_error").HasMaxLength(1000);
        builder.Property(p => p.ApdpGenerationClaimedAt).HasColumnName("apdp_generation_claimed_at");
        builder.Property(p => p.ApdpGenerationClaimToken).HasColumnName("apdp_generation_claim_token").HasMaxLength(64);
        builder.Property(p => p.GenerationClaimedAt).HasColumnName("generation_claimed_at");
        builder.Property(p => p.GenerationClaimToken).HasColumnName("generation_claim_token").HasMaxLength(64);
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(p => p.GeneratedByUserId).HasColumnName("generated_by_user_id");
        builder.Property(p => p.GeneratedAt).HasColumnName("generated_at");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => p.Code)
            .IsUnique()
            .HasDatabaseName("ix_purchase_orders_code");

        builder.HasIndex(p => new { p.CompanyId, p.Status })
            .HasDatabaseName("ix_purchase_orders_company_status");

        builder.HasOne(p => p.Company)
            .WithMany()
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Vendor)
            .WithMany()
            .HasForeignKey(p => p.VendorShadowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.GeneratedBy)
            .WithMany()
            .HasForeignKey(p => p.GeneratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Items)
            .WithOne(i => i.PurchaseOrder)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
