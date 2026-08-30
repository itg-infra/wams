namespace WAMS.Infrastructure.Data.Configurations.WorkflowTemplates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkflowTemplates;

public class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        builder.ToTable("workflow_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseSerialColumn();

        builder.Property(t => t.DocType).HasColumnName("doc_type").IsRequired().HasMaxLength(100);
        builder.Property(t => t.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(t => t.CompanyId).HasColumnName("company_id");
        builder.Property(t => t.IsActive).HasColumnName("is_active");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(t => new { t.CompanyId, t.DocType })
            .HasDatabaseName("ix_workflow_templates_company_doctype");

        builder.HasIndex(t => new { t.CompanyId, t.DocType, t.IsActive })
            .HasDatabaseName("ix_workflow_templates_company_doctype_active");

        builder.HasIndex(t => new { t.CompanyId, t.DocType })
            .IsUnique()
            .HasFilter("is_active = true")
            .HasDatabaseName("ux_workflow_templates_active_per_doc");

        builder.HasOne(t => t.Company)
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Stages)
            .WithOne(s => s.Template)
            .HasForeignKey(s => s.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
