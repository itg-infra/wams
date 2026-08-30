namespace WAMS.Application.Interfaces.Spk;

using WAMS.Application.DTOs.Spk;
using WAMS.Domain.Entities.Spk;

public interface ISpkShadowRepository
{
    Task<(List<SpkShadow> Items, int TotalCount)> GetAllAsync(SpkQuery query, IReadOnlyList<string>? whsCodes, CancellationToken ct = default);
    IAsyncEnumerable<SpkShadowResponse> StreamAllAsync(SpkQuery query, IReadOnlyList<string>? whsCodes, int limit, CancellationToken ct = default);
    Task<SpkShadow?> GetByIdAsync(long id, IReadOnlyList<string>? whsCodes, CancellationToken ct = default);
    Task<List<SpkShadow>> GetByIdsAsync(IEnumerable<long> ids, IReadOnlyList<string>? whsCodes, CancellationToken ct = default);
}
