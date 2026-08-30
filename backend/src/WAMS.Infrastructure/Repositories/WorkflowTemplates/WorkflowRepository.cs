namespace WAMS.Infrastructure.Repositories.WorkflowTemplates;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.WorkflowTemplates;
using WAMS.Domain.Entities.WorkflowTemplates;
using WAMS.Infrastructure.Data;

public class WorkflowRepository(AppDbContext db) : IWorkflowRepository
{
    public Task<WorkflowTemplate?> GetActiveTemplateAsync(long companyId, string docType, CancellationToken ct = default)
        => db.WorkflowTemplates
            .Where(t => t.CompanyId == companyId && t.DocType == docType && t.IsActive)
            .Include(t => t.Stages.OrderBy(s => s.StageOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

    public Task<WorkflowInstance?> GetInstanceWithStagesAsync(long instanceId, CancellationToken ct = default)
        => db.WorkflowInstances
            .Where(i => i.Id == instanceId)
            .Include(i => i.Stages.OrderBy(s => s.StageOrder))
                .ThenInclude(s => s.ApprovedBy)
            .Include(i => i.Stages.OrderBy(s => s.StageOrder))
                .ThenInclude(s => s.RejectedBy)
            .FirstOrDefaultAsync(ct);

    public Task CreateInstanceAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        db.WorkflowInstances.Add(instance);
        return Task.CompletedTask;
    }

    public Task DeleteInstanceAsync(long instanceId, CancellationToken ct = default)
        => db.WorkflowInstances
            .Where(i => i.Id == instanceId)
            .ExecuteDeleteAsync(ct);

    public Task<List<WorkflowTemplateSummary>> GetAllTemplatesAsync(
        long companyId,
        string? docType,
        string? search,
        string sortBy,
        string sortOrder,
        int skip,
        int take,
        CancellationToken ct = default
    )
    {
        var q = db.WorkflowTemplates
            .IgnoreQueryFilters()
            .Where(t => t.CompanyId == companyId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(docType))
            q = q.Where(t => t.DocType == docType);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(search.Trim());
            q = q.Where(t => EF.Functions.ILike(t.Name, pattern, "\\") || EF.Functions.ILike(t.DocType, pattern, "\\"));
        }

        // Sort on the entity BEFORE projecting - EF can't translate OrderBy on record constructors
        q = (sortBy?.ToLowerInvariant(), sortOrder?.ToLowerInvariant()) switch
        {
            ("doctype", "desc") => q.OrderByDescending(t => t.DocType),
            ("doctype", _) => q.OrderBy(t => t.DocType),
            ("name", "desc") => q.OrderByDescending(t => t.Name),
            ("name", _) => q.OrderBy(t => t.Name),
            ("isactive", "desc") => q.OrderByDescending(t => t.IsActive),
            ("isactive", _) => q.OrderBy(t => t.IsActive),
            ("updatedat", "desc") => q.OrderByDescending(t => t.UpdatedAt),
            ("updatedat", _) => q.OrderBy(t => t.UpdatedAt),
            (_, "desc") => q.OrderByDescending(t => t.CreatedAt),
            _ => q.OrderByDescending(t => t.CreatedAt),
        };

        // Project after sort - t.Stages.Count translates to a correlated COUNT subquery, no stage rows loaded
        return q.Skip(skip).Take(take)
            .Select(t => new WorkflowTemplateSummary(
                t.Id, t.DocType, t.Name, t.IsActive,
                t.Stages.Count,
                t.CreatedAt, t.UpdatedAt))
            .ToListAsync(ct);
    }

    public Task<int> CountTemplatesAsync(
        long companyId,
        string? docType,
        string? search,
        CancellationToken ct = default
    )
    {
        var q = db.WorkflowTemplates
            .IgnoreQueryFilters()
            .Where(t => t.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(docType))
            q = q.Where(t => t.DocType == docType);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(search.Trim());
            q = q.Where(t => EF.Functions.ILike(t.Name, pattern, "\\") || EF.Functions.ILike(t.DocType, pattern, "\\"));
        }

        return q.CountAsync(ct);
    }

    public Task<WorkflowTemplate?> GetTemplateByIdAsync(long id, long companyId, CancellationToken ct = default)
        => db.WorkflowTemplates
            .IgnoreQueryFilters()
            .Where(t => t.Id == id && t.CompanyId == companyId)
            .Include(t => t.Stages.OrderBy(s => s.StageOrder))
            .FirstOrDefaultAsync(ct);

    public Task<bool> TemplateExistsAsync(long companyId, string docType, CancellationToken ct = default)
        => db.WorkflowTemplates
            .IgnoreQueryFilters()
            .AnyAsync(t => t.CompanyId == companyId && t.DocType == docType, ct);

    public Task CreateTemplateAsync(WorkflowTemplate template, CancellationToken ct = default)
    {
        db.WorkflowTemplates.Add(template);
        return Task.CompletedTask;
    }

    public void DeleteTemplate(WorkflowTemplate template)
        => db.WorkflowTemplates.Remove(template);

    // ExecuteUpdateAsync = single UPDATE statement, no entity tracking overhead
    public Task BulkDeactivateAsync(long companyId, string docType, CancellationToken ct = default)
        => db.WorkflowTemplates
            .IgnoreQueryFilters()
            .Where(t => t.CompanyId == companyId && t.DocType == docType && t.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false), ct);

    public Task<bool> HasInstancesAsync(long templateId, CancellationToken ct = default)
        => db.WorkflowInstances
            .AnyAsync(i => i.WorkflowTemplateId == templateId, ct);
}
