namespace WAMS.Infrastructure.Caching.Common;

using Microsoft.Extensions.Caching.Hybrid;
using WAMS.Application.Interfaces.Common;

public sealed class CacheInvalidationService(HybridCache cache) : ICacheInvalidationService
{
    public async Task InvalidateWarehouseShadowsAsync(CancellationToken ct = default)
        => await cache.RemoveByTagAsync(CacheTags.WarehouseShadows, ct);

    public async Task InvalidateWarehouseShadowsForUserAsync(long userId, CancellationToken ct = default)
        => await cache.RemoveByTagAsync(CacheTags.WarehouseShadowsForUser(userId), ct);

    public async Task InvalidateRateCardsAsync(CancellationToken ct = default)
        => await cache.RemoveByTagAsync(CacheTags.RateCards, ct);

    public async Task InvalidateTaxTypesAsync(CancellationToken ct = default)
        => await cache.RemoveByTagAsync(CacheTags.TaxTypes, ct);
}
