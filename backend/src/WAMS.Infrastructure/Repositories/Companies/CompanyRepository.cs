namespace WAMS.Infrastructure.Repositories.Companies;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Companies;
using WAMS.Application.Interfaces.Companies;
using WAMS.Domain.Entities.Companies;
using WAMS.Infrastructure.Data;

public sealed class CompanyRepository(AppDbContext db) : ICompanyRepository
{
    private readonly AppDbContext _db = db;

    // Every method uses IgnoreQueryFilters() because company management
    // is a cross-tenant operation. SUPER_ADMIN needs to see ALL companies.

    public async Task<Company?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _db.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<CompanyResponse?> GetByIdWithCountsAsync(long id, CancellationToken ct = default)
    {
        return await _db.Companies
            .IgnoreQueryFilters()
            .Where(c => c.Id == id)
            .Select(c => new CompanyResponse(
                c.Id,
                c.Code,
                c.Name,
                c.Address,
                c.Phone,
                c.Email,
                c.IsActive,
                c.CreatedAt,
                c.Users.Count(),
                c.Warehouses.Count(),
                c.LogoStorageKey != null))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Company?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _db.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Code == code, ct);
    }

    public async Task<(List<CompanyResponse> Items, int TotalCount)> GetAllAsync(DataTableQuery q, CancellationToken ct = default)
    {
        var query = _db.Companies.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(c =>
                EF.Functions.ILike(c.Code, pattern, "\\") ||
                EF.Functions.ILike(c.Name, pattern, "\\") ||
                (c.Email != null && EF.Functions.ILike(c.Email, pattern, "\\")) ||
                (c.Phone != null && EF.Functions.ILike(c.Phone, pattern, "\\")));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("code", true) => query.OrderByDescending(c => c.Code),
            ("code", false) => query.OrderBy(c => c.Code),
            ("name", true) => query.OrderByDescending(c => c.Name),
            ("name", false) => query.OrderBy(c => c.Name),
            ("isactive", true) => query.OrderByDescending(c => c.IsActive),
            ("isactive", false) => query.OrderBy(c => c.IsActive),
            ("createdat", true) => query.OrderByDescending(c => c.CreatedAt),
            ("createdat", false) => query.OrderBy(c => c.CreatedAt),
            _ => query.OrderBy(c => c.Name),
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((q.Page - 1) * q.Limit)
            .Take(q.Limit)
            .Select(c => new CompanyResponse(
                c.Id,
                c.Code,
                c.Name,
                c.Address,
                c.Phone,
                c.Email,
                c.IsActive,
                c.CreatedAt,
                c.Users.Count(),
                c.Warehouses.Count(),
                c.LogoStorageKey != null))
            .ToListAsync(ct);
        return (items, total);
    }

    public IAsyncEnumerable<CompanyResponse> StreamAllAsync(DataTableQuery q, int limit, CancellationToken ct = default)
    {
        var query = _db.Companies.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(c =>
                EF.Functions.ILike(c.Code, pattern, "\\") ||
                EF.Functions.ILike(c.Name, pattern, "\\") ||
                (c.Email != null && EF.Functions.ILike(c.Email, pattern, "\\")) ||
                (c.Phone != null && EF.Functions.ILike(c.Phone, pattern, "\\")));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("code", true) => query.OrderByDescending(c => c.Code),
            ("code", false) => query.OrderBy(c => c.Code),
            ("name", true) => query.OrderByDescending(c => c.Name),
            ("name", false) => query.OrderBy(c => c.Name),
            ("isactive", true) => query.OrderByDescending(c => c.IsActive),
            ("isactive", false) => query.OrderBy(c => c.IsActive),
            ("createdat", true) => query.OrderByDescending(c => c.CreatedAt),
            ("createdat", false) => query.OrderBy(c => c.CreatedAt),
            _ => query.OrderBy(c => c.Name),
        };

        return query
            .AsNoTracking()
            .Take(limit)
            .Select(c => new CompanyResponse(
                c.Id,
                c.Code,
                c.Name,
                c.Address,
                c.Phone,
                c.Email,
                c.IsActive,
                c.CreatedAt,
                c.Users.Count(),
                c.Warehouses.Count(),
                c.LogoStorageKey != null))
            .AsAsyncEnumerable();
    }

    public async Task<List<Company>> GetActiveAsync(string? code = null, CancellationToken ct = default)
    {
        return await _db.Companies
            .IgnoreQueryFilters()
            .Where(c => c.IsActive && (code == null || c.Code == code))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public Task<Company> CreateAsync(Company company, CancellationToken ct = default)
    {
        _db.Companies.Add(company);
        return Task.FromResult(company);
    }

    public Task UpdateAsync(Company company, CancellationToken ct = default)
    {
        company.UpdatedAt = DateTime.UtcNow;
        _db.Companies.Update(company);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken ct = default)
    {
        return await _db.Companies
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Id == id, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
    {
        return await _db.Companies
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Code == code, ct);
    }
}
