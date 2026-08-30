namespace WAMS.Infrastructure.Repositories.TaxTypes;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.TaxTypes;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;

public class TaxTypeRepository(AppDbContext db) : ITaxTypeRepository
{
    public async Task<List<TaxType>> GetAllAsync(
        TaxCategory? category,
        bool activeOnly,
        CancellationToken ct = default
    )
    {
        var query = db.TaxTypes.AsQueryable();
        if (category is not null)
            query = query.Where(t => t.Category == category);
        if (activeOnly)
            query = query.Where(t => t.IsActive);
        return await query.OrderBy(t => t.Code).ToListAsync(ct);
    }

    public async Task<TaxType?> GetByIdAsync(long id, CancellationToken ct = default)
        => await db.TaxTypes.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<List<TaxType>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
        => await db.TaxTypes.Where(t => ids.Contains(t.Id)).ToListAsync(ct);

    public async Task<TaxType?> GetByCodeAsync(TaxCategory category, string code, CancellationToken ct = default)
        => await db.TaxTypes.FirstOrDefaultAsync(t => t.Category == category && t.Code == code, ct);
}
