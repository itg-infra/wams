namespace WAMS.Application.Interfaces.WorkflowTemplates;

using WAMS.Domain.Entities.WorkflowTemplates;

public record WorkflowTemplateSummary(
    long Id, string DocType, string Name, bool IsActive,
    int StageCount, DateTime CreatedAt, DateTime? UpdatedAt);

public interface IWorkflowRepository
{
    Task<WorkflowTemplate?> GetActiveTemplateAsync(long companyId, string docType, CancellationToken ct = default);
    Task<WorkflowInstance?> GetInstanceWithStagesAsync(long instanceId, CancellationToken ct = default);
    Task CreateInstanceAsync(WorkflowInstance instance, CancellationToken ct = default);
    Task DeleteInstanceAsync(long instanceId, CancellationToken ct = default);

    Task<List<WorkflowTemplateSummary>> GetAllTemplatesAsync(long companyId, string? docType, string? search, string sortBy, string sortOrder, int skip, int take, CancellationToken ct = default);
    Task<int> CountTemplatesAsync(long companyId, string? docType, string? search, CancellationToken ct = default);
    Task<WorkflowTemplate?> GetTemplateByIdAsync(long id, long companyId, CancellationToken ct = default);
    Task<bool> TemplateExistsAsync(long companyId, string docType, CancellationToken ct = default);
    Task CreateTemplateAsync(WorkflowTemplate template, CancellationToken ct = default);
    Task BulkDeactivateAsync(long companyId, string docType, CancellationToken ct = default);
    Task<bool> HasInstancesAsync(long templateId, CancellationToken ct = default);
    void DeleteTemplate(WorkflowTemplate template);
}
