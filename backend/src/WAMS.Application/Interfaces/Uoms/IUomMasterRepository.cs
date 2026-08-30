namespace WAMS.Application.Interfaces.Uoms;

using WAMS.Domain.Entities.Uoms;

public interface IUomMasterRepository
{
    Task<List<UomMaster>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<UomMaster?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<UomMaster>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task<UomMaster?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> IsReferencedByRateCardItemAsync(long id, CancellationToken ct = default);
    Task<UomMaster> CreateAsync(UomMaster uom, CancellationToken ct = default);
    Task UpdateAsync(UomMaster uom, CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);
}
