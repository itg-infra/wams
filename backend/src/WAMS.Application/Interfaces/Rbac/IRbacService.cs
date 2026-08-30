namespace WAMS.Application.Interfaces.Rbac;

using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.Common;

public interface IRbacService
{
    Task<bool> HasPermissionAsync(long userId, string module, string resource, string action, CancellationToken ct = default);
    Task<bool> HasGlobalAccessAsync(long userId, CancellationToken ct = default);

    // Roles
    Task<PaginatedResponse<RoleResponse>> GetAllRolesAsync(DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<RoleResponse> StreamAllRolesAsync(DataTableQuery query, int limit, CancellationToken ct = default);
    Task<RoleResponse> GetRoleByIdAsync(long id, CancellationToken ct = default);
    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleResponse> UpdateRoleAsync(long id, UpdateRoleRequest request, CancellationToken ct = default);
    Task DeleteRoleAsync(long id, CancellationToken ct = default);
    Task AssignPermissionAsync(long roleId, AssignPermissionRequest request, long? grantedBy, CancellationToken ct = default);
    Task RemovePermissionAsync(long roleId, long permissionId, CancellationToken ct = default);
    Task SyncPermissionsAsync(long roleId, SyncPermissionsRequest request, long? updatedBy, CancellationToken ct = default);

    // Permissions
    Task<List<PermissionInfo>> GetAllPermissionsAsync(CancellationToken ct = default);

    // User-level permission overrides
    Task<List<UserPermissionOverrideResponse>> GetUserPermissionOverridesAsync(long userId, CancellationToken ct = default);
    Task GrantUserPermissionAsync(long userId, long permissionId, UserPermissionOverrideRequest request, long grantedBy, CancellationToken ct = default);
    Task DenyUserPermissionAsync(long userId, long permissionId, UserPermissionOverrideRequest request, long grantedBy, CancellationToken ct = default);
    Task RemoveUserPermissionAsync(long userId, long permissionId, CancellationToken ct = default);
    Task<List<EffectivePermissionResponse>> GetEffectivePermissionsAsync(long userId, CancellationToken ct = default);
}
