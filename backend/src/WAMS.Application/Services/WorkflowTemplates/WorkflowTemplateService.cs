namespace WAMS.Application.Services.WorkflowTemplates;

using WAMS.Application.DTOs.WorkflowTemplates;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.WorkflowTemplates;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.WorkflowTemplates;
using WAMS.Domain.Exceptions;

public class WorkflowTemplateService(
    IWorkflowRepository repo,
    IUnitOfWork uow
) : IWorkflowTemplateService
{
    public List<WorkflowDocTypeInfo> GetDocTypes() => [.. WorkflowDocTypes.All.Select(x => new WorkflowDocTypeInfo(x.Value, x.Label))];

    public async Task<(List<WorkflowTemplateSummaryResponse> Items, int Total)> GetAllAsync(
        WorkflowTemplateQuery query,
        long companyId,
        CancellationToken ct = default
    )
    {
        var skip = (query.Page - 1) * query.Limit;
        var sortBy = query.SortBy ?? "createdAt";

        var items = await repo.GetAllTemplatesAsync(
            companyId,
            query.DocType,
            query.Search,
            sortBy,
            query.SortOrder,
            skip,
            query.Limit,
            ct
        );
        var total = await repo.CountTemplatesAsync(companyId, query.DocType, query.Search, ct);

        var summaries = items
            .Select(t => new WorkflowTemplateSummaryResponse(
                t.Id,
                t.DocType,
                t.Name,
                t.IsActive,
                t.StageCount,
                t.CreatedAt,
                t.UpdatedAt))
            .ToList();

        return (summaries, total);
    }

    public async Task<WorkflowTemplateResponse> GetByIdAsync(
        long id,
        long companyId,
        CancellationToken ct = default
    )
    {
        var template = await repo.GetTemplateByIdAsync(id, companyId, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkflowTemplate.NotFound(id));

        return ToResponse(template);
    }

    public async Task<WorkflowTemplateResponse> CreateAsync(
        long companyId,
        CreateWorkflowTemplateRequest request,
        CancellationToken ct = default
    )
    {
        if (request.IsActive)
            await repo.BulkDeactivateAsync(companyId, request.DocType, ct);

        var template = new WorkflowTemplate
        {
            DocType = request.DocType,
            Name = request.Name,
            CompanyId = companyId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            Stages = [.. request.Stages
                .OrderBy(s => s.StageOrder)
                .Select(s => new WorkflowStage
                {
                    StageOrder = s.StageOrder,
                    StageName = s.StageName,
                    ApproverRoles = [.. s.ApproverRoles],
                    CreatedAt = DateTime.UtcNow,
                })],
        };

        await repo.CreateTemplateAsync(template, ct);
        await uow.CommitAsync(ct);

        return ToResponse(template);
    }

    public async Task<WorkflowTemplateResponse> UpdateAsync(
        long id,
        long companyId,
        UpdateWorkflowTemplateRequest request,
        CancellationToken ct = default
    )
    {
        var template = await repo.GetTemplateByIdAsync(id, companyId, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkflowTemplate.NotFound(id));

        if (request.Name is not null)
            template.Name = request.Name;

        // Activate/deactivate via the update body
        if (request.IsActive.HasValue && request.IsActive.Value && !template.IsActive)
        {
            await repo.BulkDeactivateAsync(companyId, template.DocType, ct);
            template.IsActive = true;
        }
        else if (request.IsActive.HasValue && !request.IsActive.Value)
        {
            template.IsActive = false;
        }

        // Full stage replacement (delete old, insert new)
        if (request.Stages is not null)
        {
            template.Stages.Clear();
            foreach (var s in request.Stages.OrderBy(s => s.StageOrder))
                template.Stages.Add(new WorkflowStage
                {
                    StageOrder = s.StageOrder,
                    StageName = s.StageName,
                    ApproverRoles = [.. s.ApproverRoles],
                    CreatedAt = DateTime.UtcNow,
                });
        }

        template.UpdatedAt = DateTime.UtcNow;
        await uow.CommitAsync(ct);

        return ToResponse(template);
    }

    public async Task ActivateAsync(
        long id,
        long companyId,
        CancellationToken ct = default
    )
    {
        var template = await repo.GetTemplateByIdAsync(id, companyId, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkflowTemplate.NotFound(id));

        if (template.IsActive) return;

        // Deactivate all others for same docType, then activate this one
        await repo.BulkDeactivateAsync(companyId, template.DocType, ct);
        template.IsActive = true;
        template.UpdatedAt = DateTime.UtcNow;

        await uow.CommitAsync(ct);
    }

    public async Task DeactivateAsync(
        long id,
        long companyId,
        CancellationToken ct = default
    )
    {
        var template = await repo.GetTemplateByIdAsync(id, companyId, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkflowTemplate.NotFound(id));

        if (!template.IsActive) return;

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;

        await uow.CommitAsync(ct);
    }

    public async Task DeleteAsync(
        long id,
        long companyId,
        CancellationToken ct = default
    )
    {
        var template = await repo.GetTemplateByIdAsync(id, companyId, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkflowTemplate.NotFound(id));

        if (await repo.HasInstancesAsync(id, ct))
            throw new ConflictException(ErrorMessages.WorkflowTemplate.HasActiveInstances);

        repo.DeleteTemplate(template);

        await uow.CommitAsync(ct);
    }

    private static WorkflowTemplateResponse ToResponse(WorkflowTemplate t) =>
        new(
            t.Id,
            t.DocType,
            t.Name,
            t.CompanyId,
            t.IsActive,
            [.. t.Stages
                .OrderBy(s => s.StageOrder)
                .Select(s => new WorkflowStageResponse(s.Id, s.StageOrder, s.StageName, s.ApproverRoles))],
            t.CreatedAt,
            t.UpdatedAt
        );
}
