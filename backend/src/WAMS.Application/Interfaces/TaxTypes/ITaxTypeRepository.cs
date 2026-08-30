namespace WAMS.Application.Interfaces.TaxTypes;

using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Enums;

public interface ITaxTypeRepository
{
    Task<List<TaxType>> GetAllAsync(TaxCategory? category, bool activeOnly, CancellationToken ct = default);
    Task<TaxType?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<TaxType>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task<TaxType?> GetByCodeAsync(TaxCategory category, string code, CancellationToken ct = default);
}
