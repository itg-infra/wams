namespace WAMS.Application.Interfaces.Common;

using WAMS.Domain.Entities.Common;

public interface IProvinceRepository
{
    Task<Province?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<(long Id, string Name, string Display)>> GetAllActiveAsync(CancellationToken ct = default);
    Task<List<(long Id, string Name, string Display, bool IsActive)>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
}
