namespace WAMS.Infrastructure.Caching.ActivityTypes;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WAMS.Application.DTOs.ActivityTypes;
using WAMS.Application.Interfaces.ActivityTypes;
using WAMS.Infrastructure.Caching.Common;

/// <summary>
/// Caches ActivityType reads under the "activity-types" tag.
/// Any write clears the whole tag.
/// </summary>
public sealed class CachedActivityTypeService(
    [FromKeyedServices(ServiceKeys.Real)] IActivityTypeService inner,
    HybridCache cache,
    IOptions<WamsCacheOptions> options) : IActivityTypeService
{
    private readonly HybridCacheEntryOptions _opts = options.Value.ActivityType.ToHybridOptions();

    public async Task<List<ActivityTypeResponse>> GetAllAsync(CancellationToken ct = default)
        => await cache.GetOrCreateAsync(
            CacheKeys.ActivityTypeAll,
            async cancel => await inner.GetAllAsync(cancel),
            _opts,
            [CacheTags.ActivityTypes],
            ct
        );

    public async Task<ActivityTypeResponse> GetByIdAsync(long id, CancellationToken ct = default)
        => await cache.GetOrCreateAsync(
            CacheKeys.ActivityTypeById(id),
            async cancel => await inner.GetByIdAsync(id, cancel),
            _opts,
            [CacheTags.ActivityTypes],
            ct
        );

    public async Task<ActivityTypeResponse> CreateAsync(
        CreateActivityTypeRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.CreateAsync(request, ct);
        await cache.RemoveByTagAsync(CacheTags.ActivityTypes, ct);
        return result;
    }

    public async Task<ActivityTypeResponse> UpdateAsync(
        long id,
        UpdateActivityTypeRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.UpdateAsync(id, request, ct);
        await cache.RemoveByTagAsync(CacheTags.ActivityTypes, ct);
        return result;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await cache.RemoveByTagAsync(CacheTags.ActivityTypes, ct);
    }
}
