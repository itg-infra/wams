namespace WAMS.Application.Services.Uoms;

using FluentValidation;
using WAMS.Application.DTOs.Uoms;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Uoms;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Exceptions;
using DomainValidationException = Domain.Exceptions.ValidationException;

public class UomService(
    IUomMasterRepository uomRepo,
    IUnitOfWork uow,
    IValidator<CreateUomRequest> createValidator
) : IUomService
{
    public async Task<List<UomResponse>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var uoms = await uomRepo.GetAllAsync(activeOnly, ct);

        return [.. uoms.Select(Map)];
    }

    public async Task<UomResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var uom = await uomRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.Uom.NotFound(id));

        return Map(uom);
    }

    public async Task<UomResponse> CreateAsync(CreateUomRequest request, CancellationToken ct = default)
    {
        var validation = await createValidator.ValidateAsync(request, ct);

        if (!validation.IsValid)
            throw new DomainValidationException(validation.Errors.First().ErrorMessage);

        var normalizedCode = request.Code.ToUpperInvariant();
        var existing = await uomRepo.GetByCodeAsync(normalizedCode, ct);

        if (existing is not null)
            throw new ConflictException(ErrorMessages.Uom.CodeConflict(normalizedCode));

        var uom = new UomMaster
        {
            Code = normalizedCode,
            Name = request.Name,
            IsActive = true,
        };

        await uomRepo.CreateAsync(uom, ct);
        await uow.CommitAsync(ct);

        return Map(uom);
    }

    public async Task<UomResponse> UpdateAsync(long id, UpdateUomRequest request, CancellationToken ct = default)
    {
        var uom = await uomRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.Uom.NotFound(id));

        uom.Name = request.Name;
        uom.IsActive = request.IsActive;
        uom.UpdatedAt = DateTime.UtcNow;

        await uomRepo.UpdateAsync(uom, ct);
        await uow.CommitAsync(ct);

        return Map(uom);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var uom = await uomRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.Uom.NotFound(id));

        if (await uomRepo.IsReferencedByRateCardItemAsync(id, ct))
            throw new ConflictException(ErrorMessages.Uom.ReferencedByRateCard);

        await uomRepo.SoftDeleteAsync(id, ct);
        await uow.CommitAsync(ct);
    }

    private static UomResponse Map(UomMaster u) => new(u.Id, u.Code, u.Name, u.IsActive);
}
