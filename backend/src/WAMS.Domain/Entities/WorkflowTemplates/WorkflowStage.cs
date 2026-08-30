namespace WAMS.Domain.Entities.WorkflowTemplates;

using WAMS.Domain.Common;

public class WorkflowStage : BaseEntity
{
    public long WorkflowTemplateId { get; set; }
    public int StageOrder { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string[] ApproverRoles { get; set; } = [];

    public WorkflowTemplate Template { get; set; } = null!;
}
