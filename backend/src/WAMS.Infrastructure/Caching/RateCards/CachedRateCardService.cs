namespace WAMS.Infrastructure.Caching.RateCards;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.RateCards;
using WAMS.Application.Interfaces.RateCards;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Caching.Common;

/// <summary>
/// Caches only GetByIdAsync (hot read in WO creation flow) under "rate-cards".
/// GetAllAsync stays uncached - paginated + multi-filter would generate too many keys.
/// Writes invalidate the tag globally since companyId isn't available at the mutation boundary.
/// </summary>
public sealed class CachedRateCardService(
    [FromKeyedServices(ServiceKeys.Real)] IRateCardService inner,
    HybridCache cache,
    IOptions<WamsCacheOptions> options) : IRateCardService
{
    private readonly HybridCacheEntryOptions _opts = options.Value.RateCard.ToHybridOptions();

    public async Task<PaginatedResponse<RateCardSummaryResponse>> GetAllAsync(
        RateCardStatus? status,
        long? vendorId,
        DataTableQuery query,
        CancellationToken ct = default
    )
        => await inner.GetAllAsync(status, vendorId, query, ct);

    public IAsyncEnumerable<RateCardSummaryResponse> StreamAllAsync(
        RateCardStatus? status,
        long? vendorId,
        DataTableQuery query,
        int limit,
        CancellationToken ct = default
    )
        => inner.StreamAllAsync(status, vendorId, query, limit, ct);

    public async Task<RateCardResponse> GetByIdAsync(long id, CancellationToken ct = default)
        => await cache.GetOrCreateAsync(
            CacheKeys.RateCardById(id),
            async cancel => await inner.GetByIdAsync(id, cancel),
            _opts,
            [CacheTags.RateCards],
            ct
        );

    public async Task<RateCardResponse> CreateAsync(
        long userId,
        CreateRateCardRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.CreateAsync(userId, request, ct);
        await cache.RemoveByTagAsync(CacheTags.RateCards, ct);
        return result;
    }

    public async Task<RateCardResponse> CreateAndSubmitAsync(
        long userId,
        CreateRateCardRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.CreateAndSubmitAsync(userId, request, ct);
        await cache.RemoveByTagAsync(CacheTags.RateCards, ct);
        return result;
    }

    public async Task<RateCardResponse> UpdateAsync(
        long id,
        UpdateRateCardRequest request,
        CancellationToken ct = default
    )
    {
        var result = await inner.UpdateAsync(id, request, ct);
        await cache.RemoveByTagAsync(CacheTags.RateCards, ct);
        return result;
    }

    public async Task<RateCardResponse> SubmitAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var result = await inner.SubmitAsync(id, userId, ct);
        // Status change: Draft → Submitted - invalidate cached GetById
        await cache.RemoveByTagAsync(CacheTags.RateCards, ct);
        return result;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await cache.RemoveByTagAsync(CacheTags.RateCards, ct);
    }
}
