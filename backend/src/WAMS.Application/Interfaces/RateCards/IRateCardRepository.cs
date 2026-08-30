namespace WAMS.Application.Interfaces.RateCards;

using WAMS.Application.Common;
using WAMS.Application.DTOs.RateCards;
using WAMS.Domain.Entities.RateCards;
using WAMS.Domain.Enums;

public interface IRateCardRepository
{
    Task<(List<RateCardSummaryResponse> Items, int TotalCount)> GetAllAsync(RateCardStatus? status, long? vendorShadowId, DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<RateCardSummaryResponse> StreamAllAsync(RateCardStatus? status, long? vendorShadowId, DataTableQuery query, int limit, CancellationToken ct = default);
    Task<RateCard?> GetByIdWithItemsAsync(long id, CancellationToken ct = default);
    Task<RateCardItem?> FindSubmittedRateAsync(long vendorShadowId, long itemShadowId, CancellationToken ct = default);
    Task<Dictionary<(long VendorShadowId, long ItemShadowId), RateCardItem>> FindSubmittedRatesBatchAsync(IReadOnlyList<(long VendorShadowId, long ItemShadowId)> pairs, CancellationToken ct = default);
    Task<List<RateAvailability>> GetRateAvailabilityDiagnosticsAsync(IReadOnlyList<(long VendorShadowId, long ItemShadowId)> pairs, CancellationToken ct = default);
    Task<List<RateCardItem>> GetSubmittedRatesForItemAsync(long itemShadowId, CancellationToken ct = default);
    Task<RateCard> CreateAsync(RateCard rateCard, CancellationToken ct = default);
    Task UpdateAsync(RateCard rateCard, CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsAsync(long id, CancellationToken ct = default);
}
