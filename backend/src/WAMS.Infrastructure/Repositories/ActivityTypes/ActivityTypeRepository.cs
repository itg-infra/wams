namespace WAMS.Infrastructure.Repositories.ActivityTypes;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.ActivityTypes;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Infrastructure.Data;

public class ActivityTypeRepository(AppDbContext db) : IActivityTypeRepository
{
    public async Task<List<ActivityType>> GetAllAsync(CancellationToken ct = default)
        => await db.ActivityTypes
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

    public async Task<ActivityType?> GetByIdAsync(long id, CancellationToken ct = default)
        => await db.ActivityTypes.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<List<ActivityType>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
        => await db.ActivityTypes.Where(a => ids.Contains(a.Id)).ToListAsync(ct);

    public async Task<ActivityType?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await db.ActivityTypes.FirstOrDefaultAsync(a => a.Code == code, ct);

    public Task CreateAsync(ActivityType activityType, CancellationToken ct = default)
    {
        db.ActivityTypes.Add(activityType);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ActivityType activityType, CancellationToken ct = default)
    {
        db.ActivityTypes.Update(activityType);
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(long id, CancellationToken ct = default)
        => await db.ActivityTypes
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, DateTime.UtcNow), ct);
}
