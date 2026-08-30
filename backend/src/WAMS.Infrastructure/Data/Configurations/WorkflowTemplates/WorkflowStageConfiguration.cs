namespace WAMS.Infrastructure.Data.Configurations.WorkflowTemplates;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkflowTemplates;

public class WorkflowStageConfiguration : IEntityTypeConfiguration<WorkflowStage>
{
    private static readonly JsonSerializerOptions JsonOpts = new();
    private static readonly ValueComparer<string[]> ApproverRolesComparer = new(
        (a, b) => a != null && b != null && a.SequenceEqual(b),
        a => a.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
        a => a.ToArray());

    public void Configure(EntityTypeBuilder<WorkflowStage> builder)
    {
        builder.ToTable("workflow_stages");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseSerialColumn();

        builder.Property(s => s.WorkflowTemplateId).HasColumnName("workflow_template_id");
        builder.Property(s => s.StageOrder).HasColumnName("stage_order");
        builder.Property(s => s.StageName).HasColumnName("stage_name").IsRequired().HasMaxLength(200);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.Property(s => s.ApproverRoles)
            .HasColumnName("approver_roles")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOpts),
                v => JsonSerializer.Deserialize<string[]>(v, JsonOpts) ?? Array.Empty<string>())
            .Metadata.SetValueComparer(ApproverRolesComparer);

        builder.HasIndex(s => new { s.WorkflowTemplateId, s.StageOrder })
            .IsUnique()
            .HasDatabaseName("ix_workflow_stages_template_order");
    }
}
