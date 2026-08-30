namespace WAMS.Application.Interfaces.ActivityTypes;

using WAMS.Domain.Entities.ActivityTypes;

public interface IActivityTypeRepository
{
    Task<List<ActivityType>> GetAllAsync(CancellationToken ct = default);
    Task<ActivityType?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<ActivityType>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task<ActivityType?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task CreateAsync(ActivityType activityType, CancellationToken ct = default);
    Task UpdateAsync(ActivityType activityType, CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);
}
