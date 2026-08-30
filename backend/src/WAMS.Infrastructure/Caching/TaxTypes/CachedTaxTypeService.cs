namespace WAMS.Infrastructure.Caching.TaxTypes;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WAMS.Application.DTOs.TaxTypes;
using WAMS.Application.Interfaces.TaxTypes;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Caching.Common;

/// <summary>
/// Caches TaxType reads under the "tax-types" tag.
/// Writes only happen via PpnSyncService/PphLookupService, which clear the tag themselves.
/// </summary>
public sealed class CachedTaxTypeService(
    [FromKeyedServices(ServiceKeys.Real)] ITaxTypeService inner,
    HybridCache cache,
    IOptions<WamsCacheOptions> options) : ITaxTypeService
{
    private readonly HybridCacheEntryOptions _opts = options.Value.TaxType.ToHybridOptions();

    public async Task<List<TaxTypeResponse>> GetAllAsync(
        TaxCategory? category,
        bool activeOnly = true,
        CancellationToken ct = default
    )
        => await cache.GetOrCreateAsync(
            CacheKeys.TaxTypeAll(category?.Value, activeOnly),
            async cancel => await inner.GetAllAsync(category, activeOnly, cancel),
            _opts,
            [CacheTags.TaxTypes],
            ct
        );

    public async Task<TaxTypeResponse> GetByIdAsync(long id, CancellationToken ct = default)
        => await cache.GetOrCreateAsync(
            CacheKeys.TaxTypeById(id),
            async cancel => await inner.GetByIdAsync(id, cancel),
            _opts,
            [CacheTags.TaxTypes],
            ct
        );
}
