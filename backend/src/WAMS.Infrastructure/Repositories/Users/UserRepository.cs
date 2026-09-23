namespace WAMS.Infrastructure.Repositories.Users;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Users;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Users;
using WAMS.Infrastructure.Data;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    private readonly ITenantContext? _tenantContext;

    public UserRepository(AppDbContext db, ITenantContext? tenantContext = null)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        if (_tenantContext?.CompanyId is long companyId)
        {
            return await _db.Users
                .IgnoreQueryFilters()
                .Where(u => u.DeletedAt == null &&
                    u.UserCompanies.Any(m => m.CompanyId == companyId && m.RemovedAt == null) &&
                    u.Id == id)
                .Include(u => u.Company)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCompanies).ThenInclude(uc => uc.Company)
                .Include(u => u.UserCompanies).ThenInclude(uc => uc.Roles).ThenInclude(ucr => ucr.Role)
                .Include(u => u.UserCompanies).ThenInclude(uc => uc.Warehouses).ThenInclude(ucw => ucw.Warehouse)
                .Include(u => u.UserCompanies).ThenInclude(uc => uc.Provinces).ThenInclude(ucp => ucp.Province)
                .Include(u => u.UserWarehouses).ThenInclude(uw => uw.Warehouse)
                .Include(u => u.UserProvinces).ThenInclude(up => up.Province)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }

        return await _db.Users
            .Include(u => u.Company)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Company)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Roles).ThenInclude(ucr => ucr.Role)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Warehouses).ThenInclude(ucw => ucw.Warehouse)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Provinces).ThenInclude(ucp => ucp.Province)
            .Include(u => u.UserWarehouses).ThenInclude(uw => uw.Warehouse)
            .Include(u => u.UserProvinces).ThenInclude(up => up.Province)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Company)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Roles).ThenInclude(ucr => ucr.Role)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Warehouses).ThenInclude(ucw => ucw.Warehouse)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Provinces).ThenInclude(ucp => ucp.Province)
            .Include(u => u.UserWarehouses).ThenInclude(uw => uw.Warehouse)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    /// <summary>
    /// Find user by email only (email is globally unique) - uses IgnoreQueryFilters because
    /// the tenant context isn't set yet during login. Includes role permissions so the caller
    /// can determine wildcard (Super Admin) status before deciding which company to scope to.
    /// </summary>
    public async Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken ct = default)
        => await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserWarehouses).ThenInclude(uw => uw.Warehouse)
            .AsSplitQuery()
            .Where(u => u.Email == email && u.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

    public async Task<(User User, UserCompany? Membership)?> GetLoginIdentityAsync(
        string email,
        long companyId,
        CancellationToken ct = default)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserCompanies.Where(uc =>
                uc.CompanyId == companyId && uc.RemovedAt == null))
                .ThenInclude(uc => uc.Company)
            .Include(u => u.UserCompanies.Where(uc =>
                uc.CompanyId == companyId && uc.RemovedAt == null))
                .ThenInclude(uc => uc.Roles)
                    .ThenInclude(ucr => ucr.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .AsSplitQuery()
            .Where(u => u.Email == email && u.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        return user is null
            ? null
            : (user, user.UserCompanies.SingleOrDefault(uc =>
                uc.CompanyId == companyId && uc.RemovedAt == null));
    }

    public async Task<(List<User> Items, int TotalCount)> GetAllAsync(DataTableQuery q, CancellationToken ct = default)
    {
        // Base query without includes - avoids cartesian product on the COUNT
        IQueryable<User> baseQuery = _tenantContext?.CompanyId is long companyId
            ? _db.Users
                .IgnoreQueryFilters()
                .Where(u => u.DeletedAt == null &&
                    u.UserCompanies.Any(m => m.CompanyId == companyId && m.RemovedAt == null))
            : _db.Users;

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            baseQuery = baseQuery.Where(u =>
                EF.Functions.ILike(u.Email, pattern, "\\") ||
                EF.Functions.ILike(u.Fullname, pattern, "\\"));
        }

        baseQuery = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("email", true) => baseQuery.OrderByDescending(u => u.Email),
            ("email", false) => baseQuery.OrderBy(u => u.Email),
            ("fullname", true) => baseQuery.OrderByDescending(u => u.Fullname),
            ("fullname", false) => baseQuery.OrderBy(u => u.Fullname),
            ("isactive", true) => baseQuery.OrderByDescending(u => u.IsActive),
            ("isactive", false) => baseQuery.OrderBy(u => u.IsActive),
            ("createdat", true) => baseQuery.OrderByDescending(u => u.CreatedAt),
            ("createdat", false) => baseQuery.OrderBy(u => u.CreatedAt),
            _ => baseQuery.OrderByDescending(u => u.CreatedAt),
        };

        var total = await baseQuery.CountAsync(ct);
        // AsSplitQuery avoids cartesian explosion from users × roles × warehouses JOIN
        var items = await baseQuery
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Company)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Roles).ThenInclude(ucr => ucr.Role)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Warehouses).ThenInclude(ucw => ucw.Warehouse)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Provinces).ThenInclude(ucp => ucp.Province)
            .Include(u => u.UserWarehouses).ThenInclude(uw => uw.Warehouse)
            .Include(u => u.UserProvinces).ThenInclude(up => up.Province)
            .AsNoTracking()
            .AsSplitQuery()
            .Skip((q.Page - 1) * q.Limit)
            .Take(q.Limit)
            .ToListAsync(ct);
        return (items, total);
    }

    public IAsyncEnumerable<UserResponse> StreamAllAsync(DataTableQuery q, int limit, CancellationToken ct = default)
    {
        IQueryable<User> baseQuery = _tenantContext?.CompanyId is long companyId
            ? _db.Users
                .IgnoreQueryFilters()
                .Where(u => u.DeletedAt == null &&
                    u.UserCompanies.Any(m => m.CompanyId == companyId && m.RemovedAt == null))
            : _db.Users;

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            baseQuery = baseQuery.Where(u =>
                EF.Functions.ILike(u.Email, pattern, "\\") ||
                EF.Functions.ILike(u.Fullname, pattern, "\\"));
        }

        baseQuery = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("email", true) => baseQuery.OrderByDescending(u => u.Email),
            ("email", false) => baseQuery.OrderBy(u => u.Email),
            ("fullname", true) => baseQuery.OrderByDescending(u => u.Fullname),
            ("fullname", false) => baseQuery.OrderBy(u => u.Fullname),
            ("isactive", true) => baseQuery.OrderByDescending(u => u.IsActive),
            ("isactive", false) => baseQuery.OrderBy(u => u.IsActive),
            ("createdat", true) => baseQuery.OrderByDescending(u => u.CreatedAt),
            ("createdat", false) => baseQuery.OrderBy(u => u.CreatedAt),
            _ => baseQuery.OrderByDescending(u => u.CreatedAt),
        };

        return baseQuery
            .Take(limit)
            .Select(u => new UserResponse(
                u.Id,
                u.Email,
                u.Fullname,
                u.IsActive,
                u.CreatedAt,
                u.UserRoles.Select(ur => new UserRoleInfo(ur.RoleId, ur.Role.Name, ur.Role.DisplayName)).ToList(),
                u.UserWarehouses.Select(uw => new UserWarehouseInfo(uw.WarehouseId, uw.Warehouse.Code, uw.Warehouse.Name, uw.IsPrimary)).ToList(),
                u.UserProvinces.Select(up => new UserProvinceInfo(up.ProvinceId, up.Province.Name, up.Province.Display)).ToList(),
                u.EmployeeId))
            .AsNoTracking()
            .AsAsyncEnumerable();
    }

    public Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        return Task.FromResult(user);
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Entry(user).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task IncrementSessionVersionAsync(long userId, CancellationToken ct = default)
        => await _db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.SessionVersion, u => u.SessionVersion + 1)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow), ct);

    public Task<int> CountActiveSuperAdminsAsync(CancellationToken ct = default)
        => _db.Users.IgnoreQueryFilters()
            .Where(u => u.DeletedAt == null && u.IsActive &&
                u.UserRoles.Any(ur => ur.Role.Name == RoleCodes.SuperAdmin))
            .CountAsync(ct);

    public async Task SoftDeleteAsync(long id, CancellationToken ct = default)
        => await _db.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.DeletedAt, DateTime.UtcNow)
                .SetProperty(u => u.IsActive, false)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow), ct);

    public async Task<bool> ExistsAsync(long id, CancellationToken ct = default)
        => await _db.Users.AnyAsync(u => u.Id == id, ct);

    public async Task<List<long>> GetUserWarehouseIdsAsync(long userId, CancellationToken ct = default)
    {
        var provinceIds = await GetUserProvinceIdsAsync(userId, ct);

        var fromProvinces = _db.WarehouseShadows
            .Where(w => w.ProvinceId != null && provinceIds.Contains(w.ProvinceId.Value))
            .Select(w => w.Id);

        var explicitIds = _db.UserWarehouses
            .Where(uw => uw.UserId == userId)
            .Select(uw => uw.WarehouseId);

        return await fromProvinces.Union(explicitIds).Distinct().ToListAsync(ct);
    }

    public async Task<List<long>> GetUserWarehouseIdsAsync(
        long userId,
        long? userCompanyId,
        CancellationToken ct = default)
    {
        if (!userCompanyId.HasValue)
            return await GetUserWarehouseIdsAsync(userId, ct);

        var provinceIds = await GetUserProvinceIdsAsync(userId, userCompanyId, ct);
        var fromProvinces = _db.WarehouseShadows
            .Where(w => w.ProvinceId != null && provinceIds.Contains(w.ProvinceId.Value))
            .Select(w => w.Id);
        var explicitIds = _db.UserCompanyWarehouses
            .Where(ucw => ucw.UserCompanyId == userCompanyId &&
                         ucw.UserCompany.UserId == userId &&
                         ucw.UserCompany.RemovedAt == null &&
                         ucw.Warehouse.CompanyId == ucw.UserCompany.CompanyId)
            .Select(ucw => ucw.WarehouseId);

        return await fromProvinces.Union(explicitIds).Distinct().ToListAsync(ct);
    }

    // Provinces a user has province-level scope over: their DIRECT province
    // assignments plus the always-visible GLOBAL province. Deliberately does NOT
    // back-derive a province from an explicit warehouse pin - a fine-grained pin
    // grants only that one warehouse, never its province siblings or province-level data.
    public async Task<List<long>> GetUserProvinceIdsAsync(long userId, CancellationToken ct = default)
    {
        var direct = _db.UserProvinces
            .Where(up => up.UserId == userId)
            .Select(up => up.ProvinceId);

        var globalId = _db.Provinces
            .Where(p => p.Code == ProvinceCodes.Global)
            .Select(p => p.Id);

        return await direct.Union(globalId).Distinct().ToListAsync(ct);
    }

    public async Task<List<long>> GetUserProvinceIdsAsync(
        long userId,
        long? userCompanyId,
        CancellationToken ct = default)
    {
        if (!userCompanyId.HasValue)
            return await GetUserProvinceIdsAsync(userId, ct);

        var direct = _db.UserCompanyProvinces
            .Where(ucp => ucp.UserCompanyId == userCompanyId &&
                         ucp.UserCompany.UserId == userId &&
                         ucp.UserCompany.RemovedAt == null)
            .Select(ucp => ucp.ProvinceId);
        var globalId = _db.Provinces
            .Where(p => p.Code == ProvinceCodes.Global)
            .Select(p => p.Id);

        return await direct.Union(globalId).Distinct().ToListAsync(ct);
    }

    // Inverse of CheckWarehouseAccessAsync: that one asks "can this user reach this warehouse",
    // this one asks "which users reach this warehouse". Both must answer from the same rules -
    // global role, explicit pin, or the warehouse's province - or the two directions disagree and
    // a province-scoped user becomes invisible to approver lookups and PIC candidate lists while
    // still being able to open the warehouse themselves.
    private async Task<Expression<Func<User, bool>>> BuildWarehouseMembersPredicateAsync(
        long warehouseId,
        CancellationToken ct
    )
    {
        var warehouse = await _db.WarehouseShadows
            .Where(w => w.Id == warehouseId)
            .Select(w => new
            {
                w.ProvinceId,
                IsGlobal = w.Province != null && w.Province.Code == ProvinceCodes.Global,
            })
            .FirstOrDefaultAsync(ct);

        // GetUserProvinceIdsAsync unions GLOBAL into every user's scope, so a GLOBAL-province
        // warehouse is reachable by everyone and membership collapses to "no warehouse filter".
        // Callers still narrow by role or permission on top of this.
        if (warehouse?.IsGlobal == true)
            return _ => true;

        // Null when the warehouse is missing or has no province: the comparison below is
        // `province_id = NULL`, which matches nothing, leaving pins and global roles.
        var provinceId = warehouse?.ProvinceId;

        return u =>
            u.UserRoles.Any(ur => ur.Role.GlobalAccess) ||
            u.UserWarehouses.Any(uw => uw.WarehouseId == warehouseId) ||
            u.UserProvinces.Any(up => up.ProvinceId == provinceId);
    }

    public async Task<List<User>> GetUsersByRolesAndWarehouseAsync(
        long companyId,
        long warehouseId,
        IReadOnlyCollection<string> roleNames,
        CancellationToken ct = default
    )
    {
        if (roleNames.Count == 0)
            return [];

        var reachesWarehouse = await BuildWarehouseMembersPredicateAsync(warehouseId, ct);

        return await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserWarehouses)
            .Where(u => u.CompanyId == companyId && u.IsActive && u.DeletedAt == null)
            .Where(u => u.UserRoles.Any(ur => roleNames.Contains(ur.Role.Name)))
            .Where(reachesWarehouse)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<User>> GetUsersByRolesAndWarehouseForCompanyAsync(
        long companyId,
        long warehouseId,
        IReadOnlyCollection<string> roleNames,
        CancellationToken ct = default)
    {
        if (roleNames.Count == 0) return [];
        var warehouse = await _db.WarehouseShadows.IgnoreQueryFilters()
            .Where(w => w.Id == warehouseId)
            .Select(w => new { w.CompanyId, w.ProvinceId, IsGlobal = w.Province != null && w.Province.Code == ProvinceCodes.Global })
            .FirstOrDefaultAsync(ct);
        if (warehouse is null || warehouse.CompanyId != companyId) return [];

        return await _db.UserCompanies.IgnoreQueryFilters()
            .Where(m => m.CompanyId == companyId && m.RemovedAt == null && m.User.IsActive && m.User.DeletedAt == null)
            .Where(m => m.Roles.Any(r => roleNames.Contains(r.Role.Name) && (r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow)))
            .Where(m => warehouse.IsGlobal || m.Warehouses.Any(w => w.WarehouseId == warehouseId) ||
                (warehouse.ProvinceId != null && m.Provinces.Any(p => p.ProvinceId == warehouse.ProvinceId)))
            .Select(m => m.User)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<User>> GetUsersByPermissionAndWarehouseAsync(
        long companyId,
        long warehouseId,
        string permissionKey,
        CancellationToken ct = default
    )
    {
        var parts = permissionKey.Split('.');
        if (parts.Length != 3)
            return [];

        var permissionId = await _db.Permissions
            .Where(p => p.Module == parts[0] && p.Resource == parts[1] && p.Action == parts[2])
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (permissionId is null)
            return [];

        var now = DateTime.UtcNow;

        var reachesWarehouse = await BuildWarehouseMembersPredicateAsync(warehouseId, ct);

        return await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserWarehouses)
            .Where(u => u.CompanyId == companyId && u.IsActive && u.DeletedAt == null)
            .Where(reachesWarehouse)
            // Exact key only - a wildcard grant such as workorder.*.* deliberately does not qualify,
            // otherwise every admin role would show up in what is meant to be a field-worker list.
            .Where(u =>
                u.UserRoles.Any(ur => ur.Role.RolePermissions.Any(rp => rp.PermissionId == permissionId)) ||
                u.UserPermissions.Any(up => up.PermissionId == permissionId && up.IsGranted && (up.ExpiresAt == null || up.ExpiresAt > now)))
            // Active user-level deny wins over any role grant, matching RbacService resolution order.
            .Where(u => !u.UserPermissions.Any(up =>
                up.PermissionId == permissionId && !up.IsGranted && (up.ExpiresAt == null || up.ExpiresAt > now)))
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<User>> GetUsersByPermissionAndWarehouseForCompanyAsync(
        long companyId,
        long warehouseId,
        string permissionKey,
        CancellationToken ct = default)
    {
        var parts = permissionKey.Split('.');
        if (parts.Length != 3) return [];
        var permissionId = await _db.Permissions
            .Where(p => p.Module == parts[0] && p.Resource == parts[1] && p.Action == parts[2])
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(ct);
        if (permissionId is null) return [];

        var warehouse = await _db.WarehouseShadows.IgnoreQueryFilters()
            .Where(w => w.Id == warehouseId)
            .Select(w => new { w.CompanyId, w.ProvinceId, IsGlobal = w.Province != null && w.Province.Code == ProvinceCodes.Global })
            .FirstOrDefaultAsync(ct);
        if (warehouse is null || warehouse.CompanyId != companyId) return [];
        var now = DateTime.UtcNow;

        return await _db.UserCompanies.IgnoreQueryFilters()
            .Where(m => m.CompanyId == companyId && m.RemovedAt == null && m.User.IsActive && m.User.DeletedAt == null)
            .Where(m => warehouse.IsGlobal || m.Warehouses.Any(w => w.WarehouseId == warehouseId) ||
                (warehouse.ProvinceId != null && m.Provinces.Any(p => p.ProvinceId == warehouse.ProvinceId)))
            .Where(m => m.Roles.Any(r => r.Role.RolePermissions.Any(rp => rp.PermissionId == permissionId) &&
                (r.ExpiresAt == null || r.ExpiresAt > now)) ||
                m.Permissions.Any(p => p.PermissionId == permissionId && p.IsGranted && (p.ExpiresAt == null || p.ExpiresAt > now)))
            .Where(m => !m.Permissions.Any(p => p.PermissionId == permissionId && !p.IsGranted &&
                (p.ExpiresAt == null || p.ExpiresAt > now)))
            .Select(m => m.User)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> HasGlobalAccessAsync(long userId, CancellationToken ct = default)
        => await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .AnyAsync(ur => ur.Role.GlobalAccess, ct);

    // Single round-trip: warehouse existence + access in one query. Access is granted by
    // global role, an explicit UserWarehouse pin (that warehouse only), the warehouse's
    // province being one of the user's DIRECT provinces, or the warehouse's province being
    // GLOBAL. This matches GetUserWarehouseIdsAsync exactly: a pin is that warehouse only and
    // a province is all of its warehouses, so the list and detail/act paths agree.
    // Returns (false, false) when the warehouse does not exist so callers can still distinguish 404 from 403.
    public async Task<(bool WarehouseExists, bool HasAccess)> CheckWarehouseAccessAsync(
        long userId,
        long warehouseId,
        CancellationToken ct = default
    )
    {
        var row = await _db.WarehouseShadows
            .Where(w => w.Id == warehouseId)
            .Select(w => new
            {
                HasAccess =
                    _db.UserRoles.Any(ur => ur.UserId == userId && ur.Role.GlobalAccess) ||
                    _db.UserWarehouses.Any(uw => uw.UserId == userId && uw.WarehouseId == warehouseId) ||
                    (w.ProvinceId != null && (
                        _db.UserProvinces.Any(up => up.UserId == userId && up.ProvinceId == w.ProvinceId) ||
                        _db.Provinces.Any(p => p.Id == w.ProvinceId && p.Code == ProvinceCodes.Global))),
            })
            .FirstOrDefaultAsync(ct);

        return row is null ? (false, false) : (true, row.HasAccess);
    }

    public async Task<(bool WarehouseExists, bool HasAccess)> CheckWarehouseAccessAsync(
        long userId,
        long warehouseId,
        long? userCompanyId,
        CancellationToken ct = default)
    {
        if (!userCompanyId.HasValue)
            return await CheckWarehouseAccessAsync(userId, warehouseId, ct);

        var warehouse = await _db.WarehouseShadows
            .Where(w => w.Id == warehouseId)
            .Select(w => new { w.CompanyId, w.ProvinceId, IsGlobal = w.Province != null && w.Province.Code == ProvinceCodes.Global })
            .FirstOrDefaultAsync(ct);
        if (warehouse is null)
            return (false, false);

        var membership = _db.UserCompanyProvinces
            .Where(ucp => ucp.UserCompanyId == userCompanyId &&
                         ucp.UserCompany.UserId == userId &&
                         ucp.UserCompany.RemovedAt == null);
        var hasAccess = await _db.UserCompanyRoles.AnyAsync(ucr =>
                ucr.UserCompanyId == userCompanyId &&
                ucr.UserCompany.UserId == userId &&
                ucr.UserCompany.RemovedAt == null &&
                ucr.Role.GlobalAccess, ct)
            || await _db.UserCompanyWarehouses.AnyAsync(ucw =>
                ucw.UserCompanyId == userCompanyId &&
                ucw.UserCompany.UserId == userId &&
                ucw.UserCompany.RemovedAt == null &&
                ucw.WarehouseId == warehouseId &&
                ucw.Warehouse.CompanyId == ucw.UserCompany.CompanyId, ct)
            || warehouse.IsGlobal
            || (warehouse.ProvinceId.HasValue && await membership.AnyAsync(
                ucp => ucp.ProvinceId == warehouse.ProvinceId.Value, ct));

        return (true, hasAccess && warehouse.CompanyId ==
            await _db.UserCompanies.Where(uc => uc.Id == userCompanyId).Select(uc => uc.CompanyId).FirstAsync(ct));
    }

    public async Task AssignWarehouseAsync(
        long userId,
        long warehouseId,
        bool isPrimary,
        CancellationToken ct = default
    )
    {
        if (isPrimary)
            await _db.UserWarehouses
                .Where(uw => uw.UserId == userId && uw.IsPrimary)
                .ExecuteUpdateAsync(s => s.SetProperty(uw => uw.IsPrimary, false), ct);

        var existing = await _db.UserWarehouses
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WarehouseId == warehouseId, ct);

        if (existing != null)
            existing.IsPrimary = isPrimary;
        else
            _db.UserWarehouses.Add(new UserWarehouse { UserId = userId, WarehouseId = warehouseId, IsPrimary = isPrimary });
    }

    public async Task RemoveWarehouseAsync(long userId, long warehouseId, CancellationToken ct = default)
        => await _db.UserWarehouses
            .Where(uw => uw.UserId == userId && uw.WarehouseId == warehouseId)
            .ExecuteDeleteAsync(ct);

    /// <summary>
    /// Get user by ID bypassing tenant filter - used for company assignment.
    /// </summary>
    public async Task<User?> GetByIdUnfilteredAsync(long id, CancellationToken ct = default)
    {
        return await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.DeletedAt == null)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <summary>
    /// Get user by ID bypassing tenant filtering without adding the entity to the change tracker.
    /// Use this for authorization and token/session validation reads.
    /// </summary>
    public async Task<User?> GetByIdUnfilteredReadOnlyAsync(long id, CancellationToken ct = default)
    {
        return await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Company)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Company)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Roles).ThenInclude(ucr => ucr.Role)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Warehouses).ThenInclude(ucw => ucw.Warehouse)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Provinces).ThenInclude(ucp => ucp.Province)
            .Include(u => u.UserCompanies).ThenInclude(uc => uc.Permissions).ThenInclude(ucp => ucp.Permission)
            .Include(u => u.UserWarehouses).ThenInclude(uw => uw.Warehouse)
            .Include(u => u.UserProvinces).ThenInclude(up => up.Province)
            .AsSplitQuery()
            .AsNoTracking()
            .Where(u => u.DeletedAt == null)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <summary>
    /// Clear all warehouse assignments for a user - called when moving user to different company.
    /// </summary>
    public async Task ClearWarehouseAssignmentsAsync(long userId, CancellationToken ct = default)
        => await _db.UserWarehouses
            .Where(uw => uw.UserId == userId)
            .ExecuteDeleteAsync(ct);

    /// <summary>
    /// Replace a user's province scoping with the given set. Delete-then-insert:
    /// the delete is executed immediately; the inserts are flushed by the caller's CommitAsync.
    /// </summary>
    public async Task ReplaceUserProvincesAsync(
        long userId,
        IReadOnlyCollection<long> provinceIds,
        CancellationToken ct = default
    )
    {
        await _db.UserProvinces
            .Where(up => up.UserId == userId)
            .ExecuteDeleteAsync(ct);

        foreach (var provinceId in provinceIds.Distinct())
            _db.UserProvinces.Add(new UserProvince { UserId = userId, ProvinceId = provinceId });
    }
}
