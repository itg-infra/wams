namespace WAMS.Application.Services.Rbac;

using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Exceptions;

public class RbacService : IRbacService
{
    private readonly IRbacRepository _rbacRepo;
    private readonly IUnitOfWork _uow;

    public RbacService(IRbacRepository rbacRepo, IUnitOfWork uow)
    {
        _rbacRepo = rbacRepo;
        _uow = uow;
    }

    /// <summary>
    /// Permission resolution order (Explicit Deny > Explicit Grant > Role Grant > Default Deny):
    /// 1. If there is an active user-level DENIAL for the requested permission key → 403
    /// 2. If there is an active user-level GRANT for the requested permission key → allow
    /// 3. Check role-based permissions (with wildcard matching) → allow if matched
    /// 4. No match → deny
    /// </summary>
    public async Task<bool> HasPermissionAsync(
        long userId,
        string module,
        string resource,
        string action,
        CancellationToken ct = default
    )
    {
        var snapshot = await _rbacRepo.GetUserRbacSnapshotAsync(userId, ct);

        return EvaluatePermission(snapshot, module, resource, action);
    }

    public async Task<bool> HasGlobalAccessAsync(long userId, CancellationToken ct = default)
    {
        var snapshot = await _rbacRepo.GetUserRbacSnapshotAsync(userId, ct);

        // `*.*.*` is the canonical global-access permission key. `Role.GlobalAccess` flag is the
        // schema-level marker; either grants bypass.
        return snapshot.HasGlobalAccess || snapshot.RolePermissionKeys.Contains(Permissions.Wildcards.All);
    }

    // Evaluation order matches the original contract:
    //   1. Active user-level DENY  → false
    //   2. Active user-level GRANT → true
    //   3. Role wildcard match     → true
    //   4. Otherwise               → false
    internal static bool EvaluatePermission(UserRbacSnapshot snapshot, string module, string resource, string action)
    {
        foreach (var up in snapshot.ActiveOverrides)
        {
            if (Matches(up.Module, up.Resource, up.Action, module, resource, action))
            {
                if (!up.IsGranted) return false;
                return true;
            }
        }

        var keys = snapshot.RolePermissionKeys;
        var targetKey = $"{module}.{resource}.{action}";
        return
            keys.Contains(Permissions.Wildcards.All) ||
            keys.Contains($"*.*.{action}") ||
            keys.Contains($"{module}.*.*") ||
            keys.Contains($"{module}.{resource}.*") ||
            keys.Contains($"{module}.*.{action}") ||
            keys.Contains($"*.{resource}.{action}") ||
            keys.Contains(targetKey);
    }

    private static bool Matches(string om, string or_, string oa, string module, string resource, string action)
        => (om == "*" || om == module)
        && (or_ == "*" || or_ == resource)
        && (oa == "*" || oa == action);

    public async Task<PaginatedResponse<RoleResponse>> GetAllRolesAsync(
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var (items, total) = await _rbacRepo.GetAllRolesAsync(query, ct);
        var totalPages = (int)Math.Ceiling((double)total / query.Limit);

        return new PaginatedResponse<RoleResponse>(
            true,
            [.. items.Select(MapToResponse)],
            new PaginationMeta(query.Page, query.Limit, total, totalPages)
        );
    }

    public async IAsyncEnumerable<RoleResponse> StreamAllRolesAsync(
        DataTableQuery query,
        int limit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default
    )
    {
        await foreach (var role in _rbacRepo.StreamAllRolesAsync(query, limit, ct))
        {
            yield return MapToResponse(role);
        }
    }

    public async Task<RoleResponse> GetRoleByIdAsync(long id, CancellationToken ct = default)
    {
        var role = await _rbacRepo.GetRoleByIdAsync(id, ct)
            ?? throw new NotFoundException("Role", id);

        return MapToResponse(role);
    }

    public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var existing = await _rbacRepo.GetRoleByNameAsync(request.Name, ct);
        if (existing != null)
            throw new ConflictException(ErrorMessages.Role.AlreadyExists(request.Name));

        var role = new Role
        {
            Name = request.Name.ToUpperInvariant(),
            DisplayName = request.DisplayName,
            Description = request.Description,
            GlobalAccess = request.GlobalAccess,
            IsSystem = false
        };

        var created = await _rbacRepo.CreateRoleAsync(role, ct);
        await _uow.CommitAsync(ct);

        if (request.PermissionIds is { Count: > 0 })
        {
            var allPermissions = await _rbacRepo.GetAllPermissionsAsync(ct);
            var validIds = allPermissions.Select(p => p.Id).ToHashSet();
            foreach (var permId in request.PermissionIds.Distinct())
            {
                if (!validIds.Contains(permId))
                    throw new NotFoundException("Permission", permId);
                await _rbacRepo.AssignPermissionToRoleAsync(created.Id, permId, null, ct);
            }
            await _uow.CommitAsync(ct);
            created = await _rbacRepo.GetRoleByIdAsync(created.Id, ct) ?? created;
        }

        return MapToResponse(created);
    }

    public async Task<RoleResponse> UpdateRoleAsync(
        long id,
        UpdateRoleRequest request,
        CancellationToken ct = default
    )
    {
        var role = await _rbacRepo.GetRoleByIdAsync(id, ct)
            ?? throw new NotFoundException("Role", id);

        if (role.IsSystem)
            throw new ForbiddenException(ErrorMessages.Role.SystemRoleCannotBeModified);

        if (request.DisplayName != null) role.DisplayName = request.DisplayName;
        if (request.Description != null) role.Description = request.Description;
        if (request.GlobalAccess.HasValue) role.GlobalAccess = request.GlobalAccess.Value;
        role.UpdatedAt = DateTime.UtcNow;

        await _rbacRepo.UpdateRoleAsync(role, ct);

        if (request.PermissionIds is not null)
        {
            var allPermissions = await _rbacRepo.GetAllPermissionsAsync(ct);
            var validIds = allPermissions.Select(p => p.Id).ToHashSet();
            var incoming = request.PermissionIds.Distinct().ToList();

            foreach (var permId in incoming)
            {
                if (!validIds.Contains(permId))
                    throw new NotFoundException("Permission", permId);
            }

            var currentIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
            var incomingSet = incoming.ToHashSet();

            foreach (var toRemove in currentIds.Except(incomingSet))
                await _rbacRepo.RemovePermissionFromRoleAsync(id, toRemove, ct);

            foreach (var toAdd in incomingSet.Except(currentIds))
                await _rbacRepo.AssignPermissionToRoleAsync(id, toAdd, null, ct);
        }

        await _uow.CommitAsync(ct);

        return MapToResponse(role);
    }

    public async Task DeleteRoleAsync(long id, CancellationToken ct = default)
    {
        var role = await _rbacRepo.GetRoleByIdAsync(id, ct)
            ?? throw new NotFoundException("Role", id);

        if (role.IsSystem)
            throw new ForbiddenException(ErrorMessages.Role.SystemRoleCannotBeDeleted);

        await _rbacRepo.DeleteRoleAsync(id, ct);
        await _uow.CommitAsync(ct);
    }

    public async Task AssignPermissionAsync(
        long roleId,
        AssignPermissionRequest request,
        long? grantedBy,
        CancellationToken ct = default
    )
    {
        var role = await _rbacRepo.GetRoleByIdAsync(roleId, ct)
            ?? throw new NotFoundException("Role", roleId);

        var permissions = await _rbacRepo.GetAllPermissionsAsync(ct);
        if (!permissions.Any(p => p.Id == request.PermissionId))
            throw new NotFoundException("Permission", request.PermissionId);

        await _rbacRepo.AssignPermissionToRoleAsync(roleId, request.PermissionId, grantedBy, ct);
        await _uow.CommitAsync(ct);
    }

    public async Task RemovePermissionAsync(long roleId, long permissionId, CancellationToken ct = default)
    {
        var role = await _rbacRepo.GetRoleByIdAsync(roleId, ct)
            ?? throw new NotFoundException("Role", roleId);

        await _rbacRepo.RemovePermissionFromRoleAsync(roleId, permissionId, ct);
        await _uow.CommitAsync(ct);
    }

    public async Task SyncPermissionsAsync(
        long roleId,
        SyncPermissionsRequest request,
        long? updatedBy,
        CancellationToken ct = default
    )
    {
        var role = await _rbacRepo.GetRoleByIdAsync(roleId, ct)
            ?? throw new NotFoundException("Role", roleId);

        if (role.IsSystem)
            throw new ForbiddenException(ErrorMessages.Role.SystemRolePermissionsCannotBeModified);

        var allPermissions = await _rbacRepo.GetAllPermissionsAsync(ct);
        var validIds = allPermissions.Select(p => p.Id).ToHashSet();

        var incoming = request.PermissionIds.Distinct().ToList();
        foreach (var permId in incoming)
        {
            if (!validIds.Contains(permId))
                throw new NotFoundException("Permission", permId);
        }

        var currentIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        var incomingSet = incoming.ToHashSet();

        foreach (var toRemove in currentIds.Except(incomingSet))
            await _rbacRepo.RemovePermissionFromRoleAsync(roleId, toRemove, ct);

        foreach (var toAdd in incomingSet.Except(currentIds))
            await _rbacRepo.AssignPermissionToRoleAsync(roleId, toAdd, updatedBy, ct);

        await _uow.CommitAsync(ct);
    }

    public async Task<List<PermissionInfo>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        var permissions = await _rbacRepo.GetAllPermissionsAsync(ct);

        return [.. permissions.Select(p => new PermissionInfo(p.Id, p.Module, p.Resource, p.Action, p.Description))];
    }

    // User-level permission overrides
    public async Task<List<UserPermissionOverrideResponse>> GetUserPermissionOverridesAsync(
        long userId,
        CancellationToken ct = default
    )
    {
        var overrides = await _rbacRepo.GetUserPermissionOverridesAsync(userId, ct);

        return [.. overrides.Select(MapToOverrideResponse)];
    }

    public async Task GrantUserPermissionAsync(
        long userId,
        long permissionId,
        UserPermissionOverrideRequest request,
        long grantedBy,
        CancellationToken ct = default
    )
    {
        var permissions = await _rbacRepo.GetAllPermissionsAsync(ct);
        if (!permissions.Any(p => p.Id == permissionId))
            throw new NotFoundException("Permission", permissionId);

        await _rbacRepo.UpsertUserPermissionAsync(new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            IsGranted = true,
            GrantedBy = grantedBy,
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt,
            Reason = request.Reason
        }, ct);
        await _uow.CommitAsync(ct);
    }

    public async Task DenyUserPermissionAsync(
        long userId,
        long permissionId,
        UserPermissionOverrideRequest request,
        long grantedBy,
        CancellationToken ct = default
    )
    {
        var permissions = await _rbacRepo.GetAllPermissionsAsync(ct);
        if (!permissions.Any(p => p.Id == permissionId))
            throw new NotFoundException("Permission", permissionId);

        await _rbacRepo.UpsertUserPermissionAsync(new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            IsGranted = false,
            GrantedBy = grantedBy,
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt,
            Reason = request.Reason
        }, ct);
        await _uow.CommitAsync(ct);
    }

    public async Task RemoveUserPermissionAsync(long userId, long permissionId, CancellationToken ct = default)
    {
        await _rbacRepo.RemoveUserPermissionAsync(userId, permissionId, ct);
        await _uow.CommitAsync(ct);
    }

    public async Task<List<EffectivePermissionResponse>> GetEffectivePermissionsAsync(
        long userId,
        CancellationToken ct = default
    )
    {
        var effective = await _rbacRepo.GetEffectivePermissionsAsync(userId, ct);

        return [.. effective.Select(ep => new EffectivePermissionResponse(
            ep.PermissionId,
            $"{ep.Module}.{ep.Resource}.{ep.Action}",
            ep.Granted,
            ep.Source,
            ep.RoleName,
            ep.Reason,
            ep.ExpiresAt
        ))];
    }

    private static UserPermissionOverrideResponse MapToOverrideResponse(UserPermission up) => new(
        up.PermissionId,
        up.Permission.Module,
        up.Permission.Resource,
        up.Permission.Action,
        up.IsGranted,
        up.GrantedBy,
        up.GrantedAt,
        up.ExpiresAt,
        up.Reason
    );

    private static RoleResponse MapToResponse(Role role) => new(
        role.Id,
        role.Name,
        role.DisplayName,
        role.Description,
        role.IsSystem,
        role.GlobalAccess,
        role.CreatedAt,
        [.. role.RolePermissions.Select(rp => new PermissionInfo(
            rp.Permission.Id,
            rp.Permission.Module,
            rp.Permission.Resource,
            rp.Permission.Action,
            rp.Permission.Description
        ))]
    );
}
