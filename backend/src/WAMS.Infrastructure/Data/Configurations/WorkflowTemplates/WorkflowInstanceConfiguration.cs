namespace WAMS.Infrastructure.Data.Configurations.WorkflowTemplates;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkflowTemplates;

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("workflow_instances");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).UseSerialColumn();

        builder.Property(i => i.WorkflowTemplateId).HasColumnName("workflow_template_id");
        builder.Property(i => i.DocType).HasColumnName("doc_type").IsRequired().HasMaxLength(100);
        builder.Property(i => i.DocId).HasColumnName("doc_id");
        builder.Property(i => i.CurrentStageOrder).HasColumnName("current_stage_order");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(i => new { i.DocType, i.DocId })
            .HasDatabaseName("ix_workflow_instances_doctype_docid");

        builder.HasOne(i => i.Template)
            .WithMany()
            .HasForeignKey(i => i.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Stages)
            .WithOne(s => s.Instance)
            .HasForeignKey(s => s.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
