namespace WAMS.Application.Interfaces.Rca;

using WAMS.Application.DTOs.Rca;

public interface IRcaRepository
{
    Task<RcaRepoData> GetDataAsync(
        string warehouseCode,
        DateTime dateFrom,
        DateTime dateTo,
        IReadOnlyList<long>? warehouseIds,
        long? companyId,
        CancellationToken ct = default);
}
