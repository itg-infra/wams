namespace WAMS.Application.Interfaces.ActivityTypes;

using WAMS.Application.DTOs.ActivityTypes;

public interface IActivityTypeService
{
    Task<List<ActivityTypeResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ActivityTypeResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ActivityTypeResponse> CreateAsync(CreateActivityTypeRequest request, CancellationToken ct = default);
    Task<ActivityTypeResponse> UpdateAsync(long id, UpdateActivityTypeRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
