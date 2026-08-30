namespace WAMS.Application.Interfaces.Rbac;

using WAMS.Application.Common;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;

public interface IRbacRepository
{
    // Roles
    Task<Role?> GetRoleByIdAsync(long id, CancellationToken ct = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken ct = default);
    Task<(List<Role> Items, int TotalCount)> GetAllRolesAsync(DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<Role> StreamAllRolesAsync(DataTableQuery query, int limit, CancellationToken ct = default);
    Task<Role> CreateRoleAsync(Role role, CancellationToken ct = default);
    Task UpdateRoleAsync(Role role, CancellationToken ct = default);
    Task DeleteRoleAsync(long id, CancellationToken ct = default);

    // Permissions
    Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task<List<string>> GetUserPermissionKeysAsync(long userId, CancellationToken ct = default);
    Task<List<string>> GetUserPermissionKeysAsync(long userId, long? companyId, CancellationToken ct = default);

    /// <summary>
    /// One-shot snapshot of everything HasPermission/HasGlobalAccess need:
    /// role permission keys + active user overrides + global-access flag. Single UNION query.
    /// </summary>
    Task<UserRbacSnapshot> GetUserRbacSnapshotAsync(long userId, CancellationToken ct = default);

    // Role-Permission assignments
    Task AssignPermissionToRoleAsync(long roleId, long permissionId, long? grantedBy, CancellationToken ct = default);
    Task RemovePermissionFromRoleAsync(long roleId, long permissionId, CancellationToken ct = default);

    // User-Role assignments
    Task<bool> AssignRoleToUserAsync(long userId, long roleId, CancellationToken ct = default);
    Task RemoveRoleFromUserAsync(long userId, long roleId, CancellationToken ct = default);

    // User-level permission overrides
    Task<List<UserPermission>> GetUserPermissionOverridesAsync(long userId, CancellationToken ct = default);
    Task<UserPermission?> GetUserPermissionOverrideAsync(long userId, long permissionId, CancellationToken ct = default);
    Task UpsertUserPermissionAsync(UserPermission userPermission, CancellationToken ct = default);
    Task RemoveUserPermissionAsync(long userId, long permissionId, CancellationToken ct = default);

    // Effective permissions (role grants merged with user overrides)
    Task<List<EffectivePermission>> GetEffectivePermissionsAsync(long userId, CancellationToken ct = default);

    // Bulk operations for seeding
    Task<int> CountPermissionsAsync(CancellationToken ct = default);
    Task<int> CountRolesAsync(CancellationToken ct = default);
    Task CreatePermissionsAsync(IEnumerable<Permission> permissions, CancellationToken ct = default);
    Task CreateRolesAsync(IEnumerable<Role> roles, CancellationToken ct = default);
    Task BulkAssignPermissionsToRoleAsync(long roleId, IEnumerable<long> permissionIds, long? grantedBy, CancellationToken ct = default);
}

/// <summary>
/// Frozen view of everything the authorization filter needs for one user.
/// Loaded in a single UNION query; cached per-user via CachedRbacService.
/// </summary>
public sealed record UserRbacSnapshot(
    IReadOnlySet<string> RolePermissionKeys,
    IReadOnlyList<UserPermissionOverrideKey> ActiveOverrides,
    bool HasGlobalAccess);

public sealed record UserPermissionOverrideKey(
    string Module,
    string Resource,
    string Action,
    bool IsGranted);

/// <summary>Resolved permission with its source for the /effective endpoint.</summary>
public record EffectivePermission(
    long PermissionId,
    string Module,
    string Resource,
    string Action,
    string? Description,
    bool Granted,
    string Source,          // "role" | "user_grant" | "user_deny"
    string? RoleName,       // populated when Source == "role"
    string? Reason,         // populated when Source starts with "user"
    DateTime? ExpiresAt
);
