namespace WAMS.Infrastructure.Repositories.Uoms;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.Uoms;
using WAMS.Domain.Entities.Uoms;
using WAMS.Infrastructure.Data;

public class UomMasterRepository(AppDbContext db) : IUomMasterRepository
{
    public async Task<List<UomMaster>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = db.UomMasters.AsQueryable();
        if (activeOnly)
            query = query.Where(u => u.IsActive);
        return await query.OrderBy(u => u.Code).ToListAsync(ct);
    }

    public async Task<UomMaster?> GetByIdAsync(long id, CancellationToken ct = default)
        => await db.UomMasters.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<List<UomMaster>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
        => await db.UomMasters.Where(u => ids.Contains(u.Id)).ToListAsync(ct);

    public async Task<UomMaster?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await db.UomMasters.FirstOrDefaultAsync(u => u.Code == code, ct);

    public async Task<bool> IsReferencedByRateCardItemAsync(long id, CancellationToken ct = default)
        => await db.RateCardItems.AnyAsync(i => i.UomMasterId == id, ct);

    public Task<UomMaster> CreateAsync(UomMaster uom, CancellationToken ct = default)
    {
        db.UomMasters.Add(uom);
        return Task.FromResult(uom);
    }

    public Task UpdateAsync(UomMaster uom, CancellationToken ct = default)
    {
        db.UomMasters.Update(uom);
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(long id, CancellationToken ct = default)
        => await db.UomMasters
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.DeletedAt, DateTime.UtcNow)
                .SetProperty(u => u.IsActive, false), ct);
}
