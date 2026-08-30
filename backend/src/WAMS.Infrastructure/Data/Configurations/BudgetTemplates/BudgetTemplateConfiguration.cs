namespace WAMS.Infrastructure.Data.Configurations.BudgetTemplates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Enums;

public class BudgetTemplateConfiguration : IEntityTypeConfiguration<BudgetTemplate>
{
    public void Configure(EntityTypeBuilder<BudgetTemplate> builder)
    {
        builder.ToTable("budget_templates");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).UseSerialColumn();

        builder.Property(b => b.Code).HasColumnName("code").IsRequired().HasMaxLength(20);
        builder.Property(b => b.CompanyId).HasColumnName("company_id");
        builder.Property(b => b.ProvinceId).HasColumnName("province_id");
        builder.HasOne(t => t.Province)
            .WithMany()
            .HasForeignKey(t => t.ProvinceId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(t => t.ProvinceId).HasDatabaseName("idx_budget_templates_province_id");
        builder.Property(b => b.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(v => v.Value, s => BudgetTemplateStatus.FromValue(s));
        builder.Property(b => b.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(b => b.SubmittedByUserId).HasColumnName("submitted_by_user_id");
        builder.Property(b => b.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(b => b.Code)
            .IsUnique()
            .HasDatabaseName("ix_budget_templates_code");

        builder.HasIndex(b => new { b.CompanyId, b.Status })
            .HasDatabaseName("ix_budget_templates_company_status");

        builder.HasOne(b => b.Company)
            .WithMany()
            .HasForeignKey(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.CreatedBy)
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.SubmittedBy)
            .WithMany()
            .HasForeignKey(b => b.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Items)
            .WithOne(i => i.BudgetTemplate)
            .HasForeignKey(i => i.BudgetTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
