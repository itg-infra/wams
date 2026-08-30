namespace WAMS.Infrastructure.Caching.WorkflowTemplates;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WAMS.Application.DTOs.WorkflowTemplates;
using WAMS.Application.Interfaces.WorkflowTemplates;
using WAMS.Infrastructure.Caching.Common;

/// <summary>
/// Caches WorkflowTemplate reads per company under "workflow-templates:{companyId}".
/// A write for one company only clears that company's tag; others are unaffected.
/// PageResult wraps GetAllAsync's value-tuple return, since STJ can't serialise tuples.
/// </summary>
public sealed class CachedWorkflowTemplateService(
    [FromKeyedServices(ServiceKeys.Real)] IWorkflowTemplateService inner,
    HybridCache cache,
    IOptions<WamsCacheOptions> options) : IWorkflowTemplateService
{
    private readonly HybridCacheEntryOptions _opts = options.Value.WorkflowTemplate.ToHybridOptions();

    // STJ-serialisable wrapper for the value tuple the interface returns
    private sealed record PageResult(List<WorkflowTemplateSummaryResponse> Items, int Total);

    public List<WorkflowDocTypeInfo> GetDocTypes() => inner.GetDocTypes();

    public async Task<(List<WorkflowTemplateSummaryResponse> Items, int Total)> GetAllAsync(
        WorkflowTemplateQuery query,
        long companyId,
        CancellationToken ct = default
    )
    {
        var result = await cache.GetOrCreateAsync(
            CacheKeys.WorkflowTemplateAll(
                companyId,
                query.DocType,
                query.Search,
                query.SortBy,
                query.SortOrder,
                query.Page,
                query.Limit
            ),
            async cancel =>
            {
                var (items, total) = await inner.GetAllAsync(query, companyId, cancel);
                return new PageResult(items, total);
            },
            _opts,
            [CacheTags.WorkflowTemplates(companyId)],
            ct
        );
        return (result.Items, result.Total);
    }

    public async Task<WorkflowTemplateResponse> GetByIdAsync(
        long id,
        long companyId,
        CancellationToken ct = default
    )
        => await cache.GetOrCreateAsync(
            CacheKeys.WorkflowTemplateById(id, companyId),
            async cancel => await inner.GetByIdAsync(id, companyId, cancel),
            _opts,
            [CacheTags.WorkflowTemplates(companyId)],
            ct
        );

    // Mutations - delegate then invalidate
    public async Task<WorkflowTemplateResponse> CreateAsync(
        long companyId,
        CreateWorkflowTemplateRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.CreateAsync(companyId, request, ct);
        await cache.RemoveByTagAsync(CacheTags.WorkflowTemplates(companyId), ct);
        return result;
    }

    public async Task<WorkflowTemplateResponse> UpdateAsync(
        long id,
        long companyId,
        UpdateWorkflowTemplateRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.UpdateAsync(id, companyId, request, ct);
        await cache.RemoveByTagAsync(CacheTags.WorkflowTemplates(companyId), ct);
        return result;
    }

    public async Task ActivateAsync(long id, long companyId, CancellationToken ct = default)
    {
        await inner.ActivateAsync(id, companyId, ct);
        // Activate/deactivate bulk-changes IsActive on other templates - clear all for company
        await cache.RemoveByTagAsync(CacheTags.WorkflowTemplates(companyId), ct);
    }

    public async Task DeactivateAsync(long id, long companyId, CancellationToken ct = default)
    {
        await inner.DeactivateAsync(id, companyId, ct);
        await cache.RemoveByTagAsync(CacheTags.WorkflowTemplates(companyId), ct);
    }

    public async Task DeleteAsync(long id, long companyId, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, companyId, ct);
        await cache.RemoveByTagAsync(CacheTags.WorkflowTemplates(companyId), ct);
    }
}
