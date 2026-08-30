namespace WAMS.Domain.Entities.WorkflowTemplates;

using WAMS.Domain.Common;

public class WorkflowInstance : BaseEntity
{
    public long WorkflowTemplateId { get; set; }
    public string DocType { get; set; } = string.Empty;
    public long DocId { get; set; }
    public int CurrentStageOrder { get; set; }

    public WorkflowTemplate Template { get; set; } = null!;
    public ICollection<WorkflowInstanceStage> Stages { get; set; } = [];
}
