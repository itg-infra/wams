namespace WAMS.Application.Services.TaxTypes;

using WAMS.Application.DTOs.TaxTypes;
using WAMS.Application.Interfaces.TaxTypes;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;

public class TaxTypeService(ITaxTypeRepository taxTypeRepo) : ITaxTypeService
{
    public async Task<List<TaxTypeResponse>> GetAllAsync(
        TaxCategory? category,
        bool activeOnly = true,
        CancellationToken ct = default
    )
    {
        var taxTypes = await taxTypeRepo.GetAllAsync(category, activeOnly, ct);

        return [.. taxTypes.Select(Map)];
    }

    public async Task<TaxTypeResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var taxType = await taxTypeRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.TaxType.NotFound(id));

        return Map(taxType);
    }

    private static TaxTypeResponse Map(TaxType t) => new(
        t.Id,
        t.Category.Value,
        t.Code,
        t.Name,
        t.Rate,
        t.IsActive
    );
}
