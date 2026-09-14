namespace WAMS.Infrastructure.Repositories.Users;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.Users;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Infrastructure.Data;

public sealed class UserCompanyRepository(AppDbContext db) : IUserCompanyRepository
{
    private readonly AppDbContext _db = db;

    public Task<UserCompany?> GetLiveAsync(long userId, long companyId, CancellationToken ct = default)
        => BuildMembershipQuery()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.CompanyId == companyId && m.RemovedAt == null, ct);

    public Task<UserCompany?> GetAnyAsync(long userId, long companyId, CancellationToken ct = default)
        => BuildMembershipQuery()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.CompanyId == companyId, ct);

    public Task<List<UserCompany>> GetForUserAsync(long userId, bool includeRemoved = false, CancellationToken ct = default)
    {
        var query = BuildMembershipQuery().Where(m => m.UserId == userId);
        if (includeRemoved) query = query.IgnoreQueryFilters();
        return query.OrderBy(m => m.Company.Name).ToListAsync(ct);
    }

    public Task<List<UserCompany>> GetAllForUserSystemAsync(long userId, CancellationToken ct = default)
        => BuildMembershipQuery()
            .IgnoreQueryFilters()
            .Where(m => m.UserId == userId && m.RemovedAt == null)
            .OrderBy(m => m.Company.Name)
            .ToListAsync(ct);

    public Task<User?> GetUserAsync(long userId, CancellationToken ct = default)
        => _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, ct);

    public Task<Company?> GetCompanyAsync(long companyId, CancellationToken ct = default)
        => _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == companyId, ct);

    public Task<List<Role>> GetRolesByIdsAsync(IReadOnlyCollection<long> roleIds, CancellationToken ct = default)
        => _db.Roles.IgnoreQueryFilters().Where(r => roleIds.Contains(r.Id)).ToListAsync(ct);

    public Task<List<WarehouseShadow>> GetWarehousesByIdsAsync(IReadOnlyCollection<long> warehouseIds, CancellationToken ct = default)
        => _db.WarehouseShadows.IgnoreQueryFilters().Where(w => warehouseIds.Contains(w.Id)).ToListAsync(ct);

    public Task<List<Province>> GetProvincesByIdsAsync(IReadOnlyCollection<long> provinceIds, CancellationToken ct = default)
        => _db.Set<Province>().IgnoreQueryFilters().Where(p => provinceIds.Contains(p.Id)).ToListAsync(ct);

    public Task<Permission?> GetPermissionByIdAsync(long permissionId, CancellationToken ct = default)
        => _db.Permissions.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == permissionId, ct);

    public Task AddAsync(UserCompany membership, CancellationToken ct = default)
    {
        _db.UserCompanies.Add(membership);
        return Task.CompletedTask;
    }

    public async Task ClearAssignmentsAsync(long userCompanyId, CancellationToken ct = default)
    {
        await _db.UserCompanyRoles.Where(x => x.UserCompanyId == userCompanyId).ExecuteDeleteAsync(ct);
        await _db.UserCompanyWarehouses.Where(x => x.UserCompanyId == userCompanyId).ExecuteDeleteAsync(ct);
        await _db.UserCompanyProvinces.Where(x => x.UserCompanyId == userCompanyId).ExecuteDeleteAsync(ct);
        await _db.UserCompanyPermissions.Where(x => x.UserCompanyId == userCompanyId).ExecuteDeleteAsync(ct);

        DetachAssignments<UserCompanyRole>(userCompanyId, x => x.UserCompanyId);
        DetachAssignments<UserCompanyWarehouse>(userCompanyId, x => x.UserCompanyId);
        DetachAssignments<UserCompanyProvince>(userCompanyId, x => x.UserCompanyId);
        DetachAssignments<UserCompanyPermission>(userCompanyId, x => x.UserCompanyId);
    }

    public Task ClearProvinceAssignmentsAsync(long userCompanyId, CancellationToken ct = default)
        => _db.UserCompanyProvinces.Where(x => x.UserCompanyId == userCompanyId).ExecuteDeleteAsync(ct);

    public Task IncrementAuthorizationVersionAsync(long userCompanyId, CancellationToken ct = default)
        => _db.UserCompanies
            .Where(x => x.Id == userCompanyId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.AuthorizationVersion, x => x.AuthorizationVersion + 1)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct);

    public Task RevokeRefreshTokensAsync(long userCompanyId, CancellationToken ct = default)
        => _db.RefreshTokens
            .Where(x => x.UserCompanyId == userCompanyId && x.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTime.UtcNow), ct);

    private IQueryable<UserCompany> BuildMembershipQuery()
        => _db.UserCompanies
            .Include(m => m.User)
            .Include(m => m.Company)
            .Include(m => m.Roles).ThenInclude(r => r.Role)
            .Include(m => m.Warehouses).ThenInclude(w => w.Warehouse)
            .Include(m => m.Provinces).ThenInclude(p => p.Province)
            .Include(m => m.Permissions).ThenInclude(p => p.Permission)
            .AsSplitQuery();

    private void DetachAssignments<TEntity>(long userCompanyId, Func<TEntity, long> membershipId)
        where TEntity : class
    {
        foreach (var entry in _db.ChangeTracker.Entries<TEntity>()
                     .Where(entry => membershipId(entry.Entity) == userCompanyId))
            entry.State = EntityState.Detached;
    }
}
