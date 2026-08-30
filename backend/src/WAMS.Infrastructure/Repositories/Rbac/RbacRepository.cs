namespace WAMS.Infrastructure.Repositories.Rbac;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Domain.Entities.Roles;
using WAMS.Infrastructure.Data;

public class RbacRepository : IRbacRepository
{
    private readonly AppDbContext _db;

    public RbacRepository(AppDbContext db) => _db = db;

    public async Task<Role?> GetRoleByIdAsync(long id, CancellationToken ct = default)
        => await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default)
        => await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<(List<Role> Items, int TotalCount)> GetAllRolesAsync(
        DataTableQuery q,
        CancellationToken ct = default
    )
    {
        // Base query for filtering and counting - no includes to avoid cartesian product on COUNT
        var baseQuery = _db.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            baseQuery = baseQuery.Where(r =>
                EF.Functions.ILike(r.Name, pattern, "\\") ||
                (r.DisplayName != null && EF.Functions.ILike(r.DisplayName, pattern, "\\")) ||
                (r.Description != null && EF.Functions.ILike(r.Description, pattern, "\\")));
        }

        baseQuery = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("name", true) => baseQuery.OrderByDescending(r => r.Name),
            ("name", false) => baseQuery.OrderBy(r => r.Name),
            ("displayname", true) => baseQuery.OrderByDescending(r => r.DisplayName),
            ("displayname", false) => baseQuery.OrderBy(r => r.DisplayName),
            ("issystem", true) => baseQuery.OrderByDescending(r => r.IsSystem),
            ("issystem", false) => baseQuery.OrderBy(r => r.IsSystem),
            ("globalaccess", true) => baseQuery.OrderByDescending(r => r.GlobalAccess),
            ("globalaccess", false) => baseQuery.OrderBy(r => r.GlobalAccess),
            ("createdat", true) => baseQuery.OrderByDescending(r => r.CreatedAt),
            ("createdat", false) => baseQuery.OrderBy(r => r.CreatedAt),
            _ => baseQuery.OrderBy(r => r.Name),
        };

        var total = await baseQuery.CountAsync(ct);
        // AsSplitQuery avoids cartesian explosion from roles × permissions JOIN
        var items = await baseQuery
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .AsSplitQuery()
            .Skip((q.Page - 1) * q.Limit)
            .Take(q.Limit)
            .ToListAsync(ct);
        return (items, total);
    }

    public IAsyncEnumerable<Role> StreamAllRolesAsync(
        DataTableQuery q,
        int limit,
        CancellationToken ct = default
    )
    {
        var baseQuery = _db.Roles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            baseQuery = baseQuery.Where(r =>
                EF.Functions.ILike(r.Name, pattern, "\\") ||
                (r.DisplayName != null && EF.Functions.ILike(r.DisplayName, pattern, "\\")) ||
                (r.Description != null && EF.Functions.ILike(r.Description, pattern, "\\")));
        }

        baseQuery = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("name", true) => baseQuery.OrderByDescending(r => r.Name),
            ("name", false) => baseQuery.OrderBy(r => r.Name),
            ("displayname", true) => baseQuery.OrderByDescending(r => r.DisplayName),
            ("displayname", false) => baseQuery.OrderBy(r => r.DisplayName),
            ("issystem", true) => baseQuery.OrderByDescending(r => r.IsSystem),
            ("issystem", false) => baseQuery.OrderBy(r => r.IsSystem),
            ("globalaccess", true) => baseQuery.OrderByDescending(r => r.GlobalAccess),
            ("globalaccess", false) => baseQuery.OrderBy(r => r.GlobalAccess),
            ("createdat", true) => baseQuery.OrderByDescending(r => r.CreatedAt),
            ("createdat", false) => baseQuery.OrderBy(r => r.CreatedAt),
            _ => baseQuery.OrderBy(r => r.Name),
        };

        return baseQuery
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .Take(limit)
            .AsAsyncEnumerable();
    }

    public Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Add(role);
        return Task.FromResult(role);
    }

    public Task UpdateRoleAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Update(role);
        return Task.CompletedTask;
    }

    public async Task DeleteRoleAsync(long id, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is not null)
        {
            _db.Roles.Remove(role);
        }
    }

    public async Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct = default)
        => await _db.Permissions
            .OrderBy(p => p.Module).ThenBy(p => p.Resource).ThenBy(p => p.Action)
            .ToListAsync(ct);

    public async Task<List<string>> GetUserPermissionKeysAsync(long userId, CancellationToken ct = default)
        => await GetUserPermissionKeysAsync(userId, null, ct);

    public async Task<UserRbacSnapshot> GetUserRbacSnapshotAsync(long userId, CancellationToken ct = default)
    {
        // Single UNION ALL: role permission keys + active user overrides + global-access flag.
        // Replaces 2 separate round-trips for HasPermission and a 3rd for HasGlobalAccess.
        // Uses ADO.NET directly (instead of EF SqlQuery) to avoid column→property mapping fragility on
        // a heterogeneous result set.
        const string sql = """
            SELECT 'role'::text                                                   AS source,
                   p."Module"                                                     AS module,
                   p."Resource"                                                   AS resource,
                   p."Action"                                                     AS action,
                   NULL::boolean                                                  AS is_granted,
                   NULL::timestamptz                                              AS expires_at,
                   BOOL_OR(r.global_access)                                       AS global_access
            FROM user_roles ur
            JOIN roles r              ON r."Id"  = ur.role_id
            JOIN role_permissions rp  ON rp.role_id = r."Id"
            JOIN permissions p        ON p."Id"  = rp.permission_id
            WHERE ur.user_id = @p_user_id
            GROUP BY p."Module", p."Resource", p."Action"

            UNION ALL

            SELECT 'override'::text   AS source,
                   p."Module"         AS module,
                   p."Resource"       AS resource,
                   p."Action"         AS action,
                   up.is_granted      AS is_granted,
                   up.expires_at      AS expires_at,
                   FALSE              AS global_access
            FROM user_permissions up
            JOIN permissions p ON p."Id" = up.permission_id
            WHERE up.user_id = @p_user_id
            """;

        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var p = cmd.CreateParameter();
        p.ParameterName = "p_user_id";
        p.Value = userId;
        cmd.Parameters.Add(p);

        var now = DateTime.UtcNow;
        var roleKeys = new HashSet<string>(StringComparer.Ordinal);
        var overrides = new List<UserPermissionOverrideKey>();
        var hasGlobal = false;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var source = reader.GetString(0);
            var module = reader.GetString(1);
            var resource = reader.GetString(2);
            var action = reader.GetString(3);

            if (source == "role")
            {
                roleKeys.Add($"{module}.{resource}.{action}");
                if (!reader.IsDBNull(6) && reader.GetBoolean(6)) hasGlobal = true;
            }
            else // override
            {
                if (!reader.IsDBNull(5))
                {
                    var exp = reader.GetDateTime(5);
                    if (exp <= now) continue;
                }
                var granted = !reader.IsDBNull(4) && reader.GetBoolean(4);
                overrides.Add(new UserPermissionOverrideKey(module, resource, action, granted));
            }
        }

        return new UserRbacSnapshot(roleKeys, overrides, hasGlobal);
    }

    public async Task<List<string>> GetUserPermissionKeysAsync(
        long userId,
        long? companyId,
        CancellationToken ct = default
    )
    {
        var query = _db.UserRoles
            .Where(ur => ur.UserId == userId);

        if (companyId.HasValue)
        {
            // During login, tenant context isn't set, so we need to ignore query filters
            // and explicitly filter by company ID
            query = query.IgnoreQueryFilters()
                .Where(ur => ur.User != null && ur.User.CompanyId == companyId.Value && ur.User.DeletedAt == null);
        }

        return await query
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => $"{rp.Permission.Module}.{rp.Permission.Resource}.{rp.Permission.Action}")
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task AssignPermissionToRoleAsync(
        long roleId,
        long permissionId,
        long? grantedBy,
        CancellationToken ct = default
    )
    {
        var exists = await _db.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, ct);

        if (!exists)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.UtcNow
            });
        }
    }

    public async Task RemovePermissionFromRoleAsync(
        long roleId,
        long permissionId,
        CancellationToken ct = default
    )
    {
        var permissions = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.PermissionId == permissionId)
            .ToListAsync(ct);

        if (permissions.Count > 0)
        {
            _db.RolePermissions.RemoveRange(permissions);
        }
    }

    public async Task<bool> AssignRoleToUserAsync(long userId, long roleId, CancellationToken ct = default)
    {
        var exists = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

        if (exists)
            return false;

        _db.UserRoles.Add(new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        });
        return true;
    }

    public async Task RemoveRoleFromUserAsync(long userId, long roleId, CancellationToken ct = default)
    {
        var userRoles = await _db.UserRoles
            .Where(ur => ur.UserId == userId && ur.RoleId == roleId)
            .ToListAsync(ct);

        if (userRoles.Count > 0)
        {
            _db.UserRoles.RemoveRange(userRoles);
        }
    }

    // User-level permission overrides
    public async Task<List<UserPermission>> GetUserPermissionOverridesAsync(long userId, CancellationToken ct = default)
        => await _db.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.UserId == userId)
            .ToListAsync(ct);

    public async Task<UserPermission?> GetUserPermissionOverrideAsync(
        long userId,
        long permissionId,
        CancellationToken ct = default
    )
        => await _db.UserPermissions
            .Include(up => up.Permission)
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId, ct);

    public async Task UpsertUserPermissionAsync(UserPermission userPermission, CancellationToken ct = default)
    {
        var existing = await _db.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userPermission.UserId && up.PermissionId == userPermission.PermissionId, ct);

        if (existing is null)
        {
            _db.UserPermissions.Add(userPermission);
        }
        else
        {
            existing.IsGranted = userPermission.IsGranted;
            existing.GrantedBy = userPermission.GrantedBy;
            existing.GrantedAt = userPermission.GrantedAt;
            existing.ExpiresAt = userPermission.ExpiresAt;
            existing.Reason = userPermission.Reason;
            existing.Constraints = userPermission.Constraints;
        }
    }

    public async Task RemoveUserPermissionAsync(long userId, long permissionId, CancellationToken ct = default)
    {
        var overrides = await _db.UserPermissions
            .Where(up => up.UserId == userId && up.PermissionId == permissionId)
            .ToListAsync(ct);

        if (overrides.Count > 0)
        {
            _db.UserPermissions.RemoveRange(overrides);
        }
    }

    public async Task<List<EffectivePermission>> GetEffectivePermissionsAsync(long userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Role-based permissions
        var rolePerms = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => new
            {
                rp.Permission.Id,
                rp.Permission.Module,
                rp.Permission.Resource,
                rp.Permission.Action,
                rp.Permission.Description,
                ur.Role.Name
            }))
            .ToListAsync(ct);

        // Active user overrides (not expired)
        var userOverrides = await _db.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.UserId == userId && (up.ExpiresAt == null || up.ExpiresAt > now))
            .ToListAsync(ct);

        var denyIds = userOverrides.Where(up => !up.IsGranted).Select(up => up.PermissionId).ToHashSet();
        var grantOverrides = userOverrides.Where(up => up.IsGranted).ToList();

        var result = new List<EffectivePermission>();

        // Add role-based grants (skip if user-denied)
        var seenFromRole = new HashSet<long>();
        foreach (var rp in rolePerms)
        {
            if (seenFromRole.Contains(rp.Id)) continue;
            seenFromRole.Add(rp.Id);

            var isDenied = denyIds.Contains(rp.Id);
            result.Add(new EffectivePermission(
                rp.Id, rp.Module, rp.Resource, rp.Action, rp.Description,
                Granted: !isDenied,
                Source: isDenied ? "user_deny" : "role",
                RoleName: isDenied ? null : rp.Name,
                Reason: null,
                ExpiresAt: null
            ));
        }

        // Add user-level denials for permissions NOT in roles (edge case, for completeness)
        foreach (var deny in userOverrides.Where(up => !up.IsGranted && !seenFromRole.Contains(up.PermissionId)))
        {
            result.Add(new EffectivePermission(
                deny.PermissionId, deny.Permission.Module, deny.Permission.Resource,
                deny.Permission.Action, deny.Permission.Description,
                Granted: false,
                Source: "user_deny",
                RoleName: null,
                Reason: deny.Reason,
                ExpiresAt: deny.ExpiresAt
            ));
        }

        // Add user-level grants not already covered by roles
        foreach (var grant in grantOverrides.Where(g => !seenFromRole.Contains(g.PermissionId)))
        {
            result.Add(new EffectivePermission(
                grant.PermissionId, grant.Permission.Module, grant.Permission.Resource,
                grant.Permission.Action, grant.Permission.Description,
                Granted: true,
                Source: "user_grant",
                RoleName: null,
                Reason: grant.Reason,
                ExpiresAt: grant.ExpiresAt
            ));
        }

        return result;
    }

    // Bulk operations for seeding
    public async Task<int> CountPermissionsAsync(CancellationToken ct = default)
        => await _db.Permissions.CountAsync(ct);

    public async Task<int> CountRolesAsync(CancellationToken ct = default)
        => await _db.Roles.CountAsync(ct);

    public async Task CreatePermissionsAsync(IEnumerable<Permission> permissions, CancellationToken ct = default)
    {
        await _db.Permissions.AddRangeAsync(permissions, ct);
    }

    public async Task CreateRolesAsync(IEnumerable<Role> roles, CancellationToken ct = default)
    {
        await _db.Roles.AddRangeAsync(roles, ct);
    }

    public async Task BulkAssignPermissionsToRoleAsync(
        long roleId,
        IEnumerable<long> permissionIds,
        long? grantedBy,
        CancellationToken ct = default
    )
    {
        var existing = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        var toAdd = permissionIds
            .Where(pid => !existing.Contains(pid))
            .Select(pid => new RolePermission
            {
                RoleId = roleId,
                PermissionId = pid,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.UtcNow
            });

        await _db.RolePermissions.AddRangeAsync(toAdd, ct);
    }
}
