namespace WAMS.Infrastructure.Caching.Uoms;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WAMS.Application.DTOs.Uoms;
using WAMS.Application.Interfaces.Uoms;
using WAMS.Infrastructure.Caching.Common;

/// <summary>
/// Caches UoM reads under the "uom" tag.
/// Any write clears the whole tag, evicting all cached UoM entries atomically.
/// </summary>
public sealed class CachedUomService(
    [FromKeyedServices(ServiceKeys.Real)] IUomService inner,
    HybridCache cache,
    IOptions<WamsCacheOptions> options) : IUomService
{
    private readonly HybridCacheEntryOptions _opts = options.Value.Uom.ToHybridOptions();

    public async Task<List<UomResponse>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
        => await cache.GetOrCreateAsync(
            CacheKeys.UomAll(activeOnly),
            async cancel => await inner.GetAllAsync(activeOnly, cancel),
            _opts,
            [CacheTags.Uom],
            ct
        );

    public async Task<UomResponse> GetByIdAsync(long id, CancellationToken ct = default)
        => await cache.GetOrCreateAsync(
            CacheKeys.UomById(id),
            async cancel => await inner.GetByIdAsync(id, cancel),
            _opts,
            [CacheTags.Uom],
            ct
        );

    public async Task<UomResponse> CreateAsync(
        CreateUomRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.CreateAsync(request, ct);
        await cache.RemoveByTagAsync(CacheTags.Uom, ct);
        return result;
    }

    public async Task<UomResponse> UpdateAsync(
        long id,
        UpdateUomRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.UpdateAsync(id, request, ct);
        await cache.RemoveByTagAsync(CacheTags.Uom, ct);
        return result;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await cache.RemoveByTagAsync(CacheTags.Uom, ct);
    }
}
