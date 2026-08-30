namespace WAMS.Infrastructure.Caching.Rbac;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Infrastructure.Caching.Common;

/// <summary>
/// Caches HasPermissionAsync/HasGlobalAccessAsync (per user, hot path) and
/// GetAllPermissionsAsync (catalog) for IRbacService.
/// Role-level mutations clear RbacAllPerms (all users); user-override mutations clear
/// RbacUser(userId) (just that user). Both also clear WarehouseShadows, since a permission
/// change can change what CachedWarehouseShadowService has cached downstream.
/// TTL is a self-healing backstop: everything also expires on its own within the configured TTL.
/// </summary>
public sealed class CachedRbacService : IRbacService
{
    private readonly IRbacService _inner;
    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _permOpts;
    private readonly HybridCacheEntryOptions _catalogOpts;

    public CachedRbacService(
        [FromKeyedServices(ServiceKeys.Real)] IRbacService inner,
        HybridCache cache,
        IOptions<WamsCacheOptions> options)
    {
        _inner = inner;
        _cache = cache;
        var o = options.Value;
        _permOpts = o.RbacPermission.ToHybridOptions();
        _catalogOpts = o.PermissionsCatalog.ToHybridOptions();
    }

    // Hot path - cached
    public async Task<bool> HasPermissionAsync(
        long userId,
        string module,
        string resource,
        string action,
        CancellationToken ct = default
    )
        => await _cache.GetOrCreateAsync(
            CacheKeys.RbacPerm(userId, module, resource, action),
            async cancel => await _inner.HasPermissionAsync(userId, module, resource, action, cancel),
            _permOpts,
            [CacheTags.RbacUser(userId), CacheTags.RbacAllPerms],
            ct
        );

    public async Task<bool> HasGlobalAccessAsync(long userId, CancellationToken ct = default)
        => await _cache.GetOrCreateAsync(
            CacheKeys.RbacGlobal(userId),
            async cancel => await _inner.HasGlobalAccessAsync(userId, cancel),
            _permOpts,
            [CacheTags.RbacUser(userId), CacheTags.RbacAllPerms],
            ct
        );

    public async Task<List<PermissionInfo>> GetAllPermissionsAsync(CancellationToken ct = default)
        => await _cache.GetOrCreateAsync(
            CacheKeys.PermissionsCatalog,
            async cancel => await _inner.GetAllPermissionsAsync(cancel),
            _catalogOpts,
            [CacheTags.PermissionsCatalog],
            ct
        );

    // Roles - passthrough + invalidation
    public async Task<PaginatedResponse<RoleResponse>> GetAllRolesAsync(
        DataTableQuery query,
        CancellationToken ct = default
    )
        => await _inner.GetAllRolesAsync(query, ct);

    public IAsyncEnumerable<RoleResponse> StreamAllRolesAsync(
        DataTableQuery query,
        int limit,
        CancellationToken ct = default
    )
        => _inner.StreamAllRolesAsync(query, limit, ct);

    public async Task<RoleResponse> GetRoleByIdAsync(long id, CancellationToken ct = default)
        => await _inner.GetRoleByIdAsync(id, ct);

    public async Task<RoleResponse> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken ct = default
    )
    {
        var result = await _inner.CreateRoleAsync(request, ct);
        // New role may be assigned to users - clear all perm caches conservatively
        await _cache.RemoveByTagAsync(CacheTags.RbacAllPerms, ct);
        await _cache.RemoveByTagAsync(CacheTags.WarehouseShadows, ct);
        return result;
    }

    public async Task<RoleResponse> UpdateRoleAsync(
        long id,
        UpdateRoleRequest request,
        CancellationToken ct = default
    )
    {
        var result = await _inner.UpdateRoleAsync(id, request, ct);
        await _cache.RemoveByTagAsync(CacheTags.RbacAllPerms, ct);
        await _cache.RemoveByTagAsync(CacheTags.WarehouseShadows, ct);
        return result;
    }

    public async Task DeleteRoleAsync(long id, CancellationToken ct = default)
    {
        await _inner.DeleteRoleAsync(id, ct);
        await _cache.RemoveByTagAsync(CacheTags.RbacAllPerms, ct);
        await _cache.RemoveByTagAsync(CacheTags.WarehouseShadows, ct);
    }

    public async Task AssignPermissionAsync(
        long roleId,
        AssignPermissionRequest request,
        long? grantedBy,
        CancellationToken ct = default
    )
    {
        await _inner.AssignPermissionAsync(roleId, request, grantedBy, ct);
        await _cache.RemoveByTagAsync(CacheTags.RbacAllPerms, ct);
        await _cache.RemoveByTagAsync(CacheTags.WarehouseShadows, ct);
    }

    public async Task RemovePermissionAsync(
        long roleId,
        long permissionId,
        CancellationToken ct = default
    )
    {
        await _inner.RemovePermissionAsync(roleId, permissionId, ct);
        await _cache.RemoveByTagAsync(CacheTags.RbacAllPerms, ct);
        await _cache.RemoveByTagAsync(CacheTags.WarehouseShadows, ct);
    }

    public async Task SyncPermissionsAsync(
        long roleId,
        SyncPermissionsRequest request,
        long? updatedBy,
        CancellationToken ct = default
    )
    {
        await _inner.SyncPermissionsAsync(roleId, request, updatedBy, ct);
        await _cache.RemoveByTagAsync(CacheTags.RbacAllPerms, ct);
        await _cache.RemoveByTagAsync(CacheTags.WarehouseShadows, ct);
    }

    // User-level overrides - passthrough + per-user invalidation
    public async Task<List<UserPermissionOverrideResponse>> GetUserPermissionOverridesAsync(
        long userId,
        CancellationToken ct = default
    )
        => await _inner.GetUserPermissionOverridesAsync(userId, ct);

    public async Task GrantUserPermissionAsync(
        long userId,
        long permissionId,
        UserPermissionOverrideRequest request,
        long grantedBy,
        CancellationToken ct = default
    )
    {
        await _inner.GrantUserPermissionAsync(userId, permissionId, request, grantedBy, ct);
        await _cache.RemoveByTagAsync(CacheTags.RbacUser(userId), ct);
        await _cache.RemoveByTagAsync(CacheTags.WarehouseShadowsForUser(userId), ct);
    }

    public async Task DenyUserPermissionAsync(
        long userId,
        long permissionId,
        UserPermissionOverrideRequest request,
        long grantedBy,
        CancellationToken ct = default
    )
    {
        await _inner.DenyUserPermissionAsync(userId, permissionId, request, grantedBy, ct);
        await _cache.RemoveByTagAsync(CacheTags.RbacUser(userId), ct);
        await _cache.RemoveByTagAsync(CacheTags.WarehouseShadowsForUser(userId), ct);
    }

    public async Task RemoveUserPermissionAsync(
        long userId,
        long permissionId,
        CancellationToken ct = default
    )
    {
        await _inner.RemoveUserPermissionAsync(userId, permissionId, ct);
        await _cache.RemoveByTagAsync(CacheTags.RbacUser(userId), ct);
        await _cache.RemoveByTagAsync(CacheTags.WarehouseShadowsForUser(userId), ct);
    }

    public async Task<List<EffectivePermissionResponse>> GetEffectivePermissionsAsync(
        long userId,
        CancellationToken ct = default
    )
        => await _inner.GetEffectivePermissionsAsync(userId, ct);
}
