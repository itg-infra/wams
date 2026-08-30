namespace WAMS.Domain.Entities.WorkflowTemplates;

using WAMS.Domain.Common;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Users;

public class WorkflowInstanceStage : BaseEntity
{
    public long WorkflowInstanceId { get; set; }
    public int StageOrder { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string[] ApproverRoles { get; set; } = [];
    public string Status { get; set; } = WorkflowStageStatus.Pending;

    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public long? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Maps to PostgreSQL xmin system column - changes on every write, used for optimistic concurrency.
    public uint Version { get; set; }

    public WorkflowInstance Instance { get; set; } = null!;
    public User? ApprovedBy { get; set; }
    public User? RejectedBy { get; set; }
}
