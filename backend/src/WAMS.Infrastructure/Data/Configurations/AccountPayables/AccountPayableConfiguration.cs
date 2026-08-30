namespace WAMS.Infrastructure.Data.Configurations.AccountPayables;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.AccountPayables;
using WAMS.Domain.Enums;

public class AccountPayableConfiguration : IEntityTypeConfiguration<AccountPayable>
{
    public void Configure(EntityTypeBuilder<AccountPayable> builder)
    {
        builder.ToTable("account_payables");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseSerialColumn();

        builder.Property(a => a.Code).HasColumnName("code").IsRequired().HasMaxLength(20);
        builder.Property(a => a.CompanyId).HasColumnName("company_id");
        builder.Property(a => a.VendorShadowId).HasColumnName("vendor_shadow_id");
        builder.Property(a => a.Remark).HasColumnName("remark").HasMaxLength(500);
        builder.Property(a => a.DocDate).HasColumnName("doc_date");
        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, s => AccountPayableStatus.FromValue(s));
        builder.Property(a => a.SapApNumber).HasColumnName("sap_ap_number").HasMaxLength(100);
        builder.Property(a => a.SapApdpDocEntry).HasColumnName("sap_apdp_doc_entry");
        builder.Property(a => a.SapDocEntry).HasColumnName("sap_doc_entry");
        builder.Property(a => a.GenerationClaimedAt).HasColumnName("generation_claimed_at");
        builder.Property(a => a.GenerationClaimToken).HasColumnName("generation_claim_token").HasMaxLength(64);
        builder.Property(a => a.DiscountAmount).HasColumnName("discount_amount").HasDefaultValue(0m);
        builder.Property(a => a.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(a => a.GeneratedByUserId).HasColumnName("generated_by_user_id");
        builder.Property(a => a.GeneratedAt).HasColumnName("generated_at");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(a => a.Code)
            .IsUnique()
            .HasDatabaseName("ix_account_payables_code");

        builder.HasIndex(a => new { a.CompanyId, a.Status })
            .HasDatabaseName("ix_account_payables_company_status");

        builder.HasIndex(a => a.VendorShadowId)
            .HasDatabaseName("ix_account_payables_vendor_shadow_id");

        builder.HasIndex(a => a.DocDate)
            .HasDatabaseName("ix_account_payables_doc_date");

        builder.HasOne(a => a.Company)
            .WithMany()
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Vendor)
            .WithMany()
            .HasForeignKey(a => a.VendorShadowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CreatedBy)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.GeneratedBy)
            .WithMany()
            .HasForeignKey(a => a.GeneratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Items)
            .WithOne(i => i.AccountPayable)
            .HasForeignKey(i => i.AccountPayableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
