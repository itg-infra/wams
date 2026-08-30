namespace WAMS.Application.Interfaces.Spk;

using WAMS.Application.DTOs.Spk;
using WAMS.Domain.Entities.Spk;

public interface ISpkService
{
    Task<(List<SpkShadow> Items, int TotalCount)> GetAllAsync(SpkQuery query, long userId, CancellationToken ct = default);
    IAsyncEnumerable<SpkShadowResponse> StreamAllAsync(SpkQuery query, long userId, int limit, CancellationToken ct = default);
    Task<SpkShadow> GetByIdAsync(long id, long userId, CancellationToken ct = default);
}
