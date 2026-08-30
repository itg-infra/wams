namespace WAMS.Application.Interfaces.RateCards;

using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.RateCards;
using WAMS.Domain.Enums;

public interface IRateCardService
{
    Task<PaginatedResponse<RateCardSummaryResponse>> GetAllAsync(
        RateCardStatus? status, long? vendorId, DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<RateCardSummaryResponse> StreamAllAsync(
        RateCardStatus? status, long? vendorId, DataTableQuery query, int limit, CancellationToken ct = default);
    Task<RateCardResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<RateCardResponse> CreateAsync(long userId, CreateRateCardRequest request, CancellationToken ct = default);
    Task<RateCardResponse> CreateAndSubmitAsync(long userId, CreateRateCardRequest request, CancellationToken ct = default);
    Task<RateCardResponse> UpdateAsync(long id, UpdateRateCardRequest request, CancellationToken ct = default);
    Task<RateCardResponse> SubmitAsync(long id, long userId, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
