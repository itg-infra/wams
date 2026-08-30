namespace WAMS.Domain.Entities.WorkflowTemplates;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Companies;

public class WorkflowTemplate : BaseEntity
{
    public string DocType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
    public ICollection<WorkflowStage> Stages { get; set; } = [];
}
