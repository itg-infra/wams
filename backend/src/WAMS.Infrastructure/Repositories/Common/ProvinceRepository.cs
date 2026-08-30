namespace WAMS.Infrastructure.Repositories.Common;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.Common;
using WAMS.Infrastructure.Data;

public class ProvinceRepository(AppDbContext db) : IProvinceRepository
{
    public async Task<Province?> GetByIdAsync(long id, CancellationToken ct = default)
        => await db.Provinces.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<(long Id, string Name, string Display)>> GetAllActiveAsync(CancellationToken ct = default)
        => (await db.Provinces.Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Display }).ToListAsync(ct))
            .Select(p => (p.Id, p.Name, p.Display)).ToList();

    public async Task<List<(long Id, string Name, string Display, bool IsActive)>> GetByIdsAsync(
        IEnumerable<long> ids,
        CancellationToken ct = default
    )
        => (await db.Provinces.Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Display, p.IsActive }).ToListAsync(ct))
            .Select(p => (p.Id, p.Name, p.Display, p.IsActive)).ToList();
}
