namespace WAMS.Application.DTOs.Roles;

public record CreateRoleRequest(
    string Name,
    string? DisplayName,
    string? Description,
    bool GlobalAccess = false,
    List<long>? PermissionIds = null
);

public record SyncPermissionsRequest(List<long> PermissionIds);

public record UpdateRoleRequest(
    string? DisplayName,
    string? Description,
    bool? GlobalAccess,
    List<long>? PermissionIds = null
);

public record AssignPermissionRequest(long PermissionId);

public record RoleResponse(
    long Id,
    string Name,
    string? DisplayName,
    string? Description,
    bool IsSystem,
    bool GlobalAccess,
    DateTime CreatedAt,
    List<PermissionInfo> Permissions
);

public record PermissionInfo(long Id, string Module, string Resource, string Action, string? Description);

// User-level permission overrides
public record UserPermissionOverrideRequest(
    DateTime? ExpiresAt,
    string? Reason
);

public record UserPermissionOverrideResponse(
    long PermissionId,
    string Module,
    string Resource,
    string Action,
    bool IsGranted,
    long GrantedBy,
    DateTime GrantedAt,
    DateTime? ExpiresAt,
    string? Reason
);

public record EffectivePermissionResponse(
    long PermissionId,
    string Permission,       // "module.resource.action"
    bool Granted,
    string Source,           // "role" | "user_grant" | "user_deny"
    string? RoleName,
    string? Reason,
    DateTime? ExpiresAt
);
