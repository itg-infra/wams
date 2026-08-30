namespace WAMS.Application.Services.ActivityTypes;

using WAMS.Application.DTOs.ActivityTypes;
using WAMS.Application.Interfaces.ActivityTypes;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Exceptions;

public class ActivityTypeService(
    IActivityTypeRepository activityTypeRepo,
    IUnitOfWork uow
) : IActivityTypeService
{
    public async Task<List<ActivityTypeResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await activityTypeRepo.GetAllAsync(ct);

        return [.. items.Select(Map)];
    }

    public async Task<ActivityTypeResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var at = await activityTypeRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.ActivityType.NotFound(id));

        return Map(at);
    }

    public async Task<ActivityTypeResponse> CreateAsync(
        CreateActivityTypeRequest request,
        CancellationToken ct = default
    )
    {
        var existing = await activityTypeRepo.GetByCodeAsync(request.Code, ct);
        if (existing is not null)
            throw new ValidationException(ErrorMessages.ActivityType.CodeConflict(request.Code));

        var at = new ActivityType
        {
            Code = request.Code,
            Name = request.Name,
        };

        await activityTypeRepo.CreateAsync(at, ct);
        await uow.CommitAsync(ct);

        return Map(at);
    }

    public async Task<ActivityTypeResponse> UpdateAsync(
        long id,
        UpdateActivityTypeRequest request,
        CancellationToken ct = default
    )
    {
        var at = await activityTypeRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.ActivityType.NotFound(id));

        if (request.Code is not null && request.Code != at.Code)
        {
            var existing = await activityTypeRepo.GetByCodeAsync(request.Code, ct);
            if (existing is not null)
                throw new ValidationException(ErrorMessages.ActivityType.CodeConflict(request.Code));
            at.Code = request.Code;
        }

        if (request.Name is not null)
            at.Name = request.Name;

        if (request.IsActive.HasValue)
            at.IsActive = request.IsActive.Value;

        at.UpdatedAt = DateTime.UtcNow;

        await activityTypeRepo.UpdateAsync(at, ct);
        await uow.CommitAsync(ct);

        return Map(at);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var at = await activityTypeRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.ActivityType.NotFound(id));

        await activityTypeRepo.SoftDeleteAsync(id, ct);
        await uow.CommitAsync(ct);
    }

    private static ActivityTypeResponse Map(ActivityType at) =>
        new(at.Id, at.Code, at.Name, at.IsActive);
}
