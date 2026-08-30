namespace WAMS.Application.Interfaces.WorkflowTemplates;

using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.WorkflowTemplates;

public interface IWorkflowTemplateService
{
    List<WorkflowDocTypeInfo> GetDocTypes();
    Task<(List<WorkflowTemplateSummaryResponse> Items, int Total)> GetAllAsync(WorkflowTemplateQuery query, long companyId, CancellationToken ct = default);
    Task<WorkflowTemplateResponse> GetByIdAsync(long id, long companyId, CancellationToken ct = default);
    Task<WorkflowTemplateResponse> CreateAsync(long companyId, CreateWorkflowTemplateRequest request, CancellationToken ct = default);
    Task<WorkflowTemplateResponse> UpdateAsync(long id, long companyId, UpdateWorkflowTemplateRequest request, CancellationToken ct = default);
    Task ActivateAsync(long id, long companyId, CancellationToken ct = default);
    Task DeactivateAsync(long id, long companyId, CancellationToken ct = default);
    Task DeleteAsync(long id, long companyId, CancellationToken ct = default);
}
