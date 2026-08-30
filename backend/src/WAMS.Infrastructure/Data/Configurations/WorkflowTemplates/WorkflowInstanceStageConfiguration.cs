namespace WAMS.Infrastructure.Data.Configurations.WorkflowTemplates;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WAMS.Domain.Entities.WorkflowTemplates;

public class WorkflowInstanceStageConfiguration : IEntityTypeConfiguration<WorkflowInstanceStage>
{
    private static readonly JsonSerializerOptions JsonOpts = new();
    private static readonly ValueComparer<string[]> ApproverRolesComparer = new(
        (a, b) => a != null && b != null && a.SequenceEqual(b),
        a => a.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
        a => a.ToArray());

    public void Configure(EntityTypeBuilder<WorkflowInstanceStage> builder)
    {
        builder.ToTable("workflow_instance_stages");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseSerialColumn();

        builder.Property(s => s.WorkflowInstanceId).HasColumnName("workflow_instance_id");
        builder.Property(s => s.StageOrder).HasColumnName("stage_order");
        builder.Property(s => s.StageName).HasColumnName("stage_name").IsRequired().HasMaxLength(200);
        builder.Property(s => s.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
        builder.Property(s => s.ApprovedByUserId).HasColumnName("approved_by_user_id");
        builder.Property(s => s.ApprovedAt).HasColumnName("approved_at");
        builder.Property(s => s.RejectedByUserId).HasColumnName("rejected_by_user_id");
        builder.Property(s => s.RejectedAt).HasColumnName("rejected_at");
        builder.Property(s => s.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        builder.Property(s => s.ApproverRoles)
            .HasColumnName("approver_roles")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOpts),
                v => JsonSerializer.Deserialize<string[]>(v, JsonOpts) ?? Array.Empty<string>())
            .Metadata.SetValueComparer(ApproverRolesComparer);

        builder.Property(s => s.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(s => new { s.WorkflowInstanceId, s.StageOrder })
            .HasDatabaseName("ix_workflow_instance_stages_instance_order");

        builder.HasIndex(s => new { s.WorkflowInstanceId, s.Status, s.StageOrder })
            .HasDatabaseName("ix_workflow_instance_stages_instance_status_order");

        builder.HasIndex(s => new { s.WorkflowInstanceId, s.StageOrder, s.Status })
            .HasDatabaseName("ix_workflow_instance_stages_instance_order_status");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("ix_workflow_instance_stages_status");

        builder.HasOne(s => s.ApprovedBy)
            .WithMany()
            .HasForeignKey(s => s.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.RejectedBy)
            .WithMany()
            .HasForeignKey(s => s.RejectedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
