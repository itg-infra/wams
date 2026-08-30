namespace WAMS.Application.Interfaces.TaxTypes;

using WAMS.Application.DTOs.TaxTypes;
using WAMS.Domain.Enums;

public interface ITaxTypeService
{
    Task<List<TaxTypeResponse>> GetAllAsync(TaxCategory? category, bool activeOnly = true, CancellationToken ct = default);
    Task<TaxTypeResponse> GetByIdAsync(long id, CancellationToken ct = default);
}
