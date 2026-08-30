namespace WAMS.Infrastructure.Caching.Warehouses;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Infrastructure.Caching.Common;

/// <summary>
/// Caches WarehouseShadow reads (per-user, since visibility is access-controlled) under both
/// a global "warehouse-shadows" tag and a per-user "warehouse-shadows:user:{userId}" tag.
/// Global tag: cleared wholesale by ERP sync (MasterDataSyncBackgroundService), which affects
/// all users at once. Per-user tag: cleared by a single user's scope/pin edit (UserService).
/// No write methods here - this service only reads ERP-synced data.
/// </summary>
public sealed class CachedWarehouseShadowService(
    [FromKeyedServices(ServiceKeys.Real)] IWarehouseShadowService inner,
    HybridCache cache,
    IOptions<WamsCacheOptions> options) : IWarehouseShadowService
{
    private readonly HybridCacheEntryOptions _opts = options.Value.WarehouseShadow.ToHybridOptions();

    public async Task<PaginatedResponse<WarehouseResponse>> GetAllAsync(
        long userId,
        WarehouseQuery query,
        CancellationToken ct = default
    )
        => await cache.GetOrCreateAsync(
            CacheKeys.WarehouseShadowAll(
                userId,
                query.Search,
                query.ProvinceId,
                query.SortBy,
                query.SortOrder,
                query.Page,
                query.Limit
            ),
            async cancel => await inner.GetAllAsync(userId, query, cancel),
            _opts,
            [CacheTags.WarehouseShadows, CacheTags.WarehouseShadowsForUser(userId)],
            ct
        );

    public async Task<WarehouseResponse> GetByIdAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
        => await cache.GetOrCreateAsync(
            CacheKeys.WarehouseShadowById(id, userId),
            async cancel => await inner.GetByIdAsync(id, userId, cancel),
            _opts,
            [CacheTags.WarehouseShadows, CacheTags.WarehouseShadowsForUser(userId)],
            ct
        );

    public async Task<List<ProvinceOption>> GetDistinctLocationsAsync(long userId, CancellationToken ct = default)
        => await cache.GetOrCreateAsync(
            CacheKeys.WarehouseShadowLocations(userId),
            async cancel => await inner.GetDistinctLocationsAsync(userId, cancel),
            _opts,
            [CacheTags.WarehouseShadows, CacheTags.WarehouseShadowsForUser(userId)],
            ct
        );

    // Not cached - streaming export bypasses cache to avoid materializing large result sets in memory
    public IAsyncEnumerable<WarehouseResponse> StreamAllAsync(
        long userId,
        WarehouseQuery query,
        int limit,
        CancellationToken ct = default
    )
        => inner.StreamAllAsync(userId, query, limit, ct);

    // Not cached - admin-only endpoint; results change after sync runs
    public Task<List<WarehouseResponse>> GetUnmappedAsync(long userId, CancellationToken ct = default)
        => inner.GetUnmappedAsync(userId, ct);
}
