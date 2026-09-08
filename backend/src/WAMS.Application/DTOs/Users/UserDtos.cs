namespace WAMS.Application.DTOs.Users;

public record CreateUserRequest(
    string Email,
    string Password,
    string Fullname,
    // WarehouseIds/PrimaryWarehouseId (fine grain) and ProvinceIds (coarse grain) are
    // independent and additive - there is no precedence between them. Effective access is
    // the union: all warehouses in ProvinceIds PLUS each individually pinned WarehouseId.
    List<long>? WarehouseIds = null,
    long? PrimaryWarehouseId = null,
    List<long>? ProvinceIds = null,
    string? EmployeeId = null
);

public record UpdateUserRequest(
    string? Fullname,
    string? Email,
    bool? IsActive,
    List<long>? ProvinceIds = null, // null = leave scope untouched; non-null (incl. empty) = replace
    string? EmployeeId = null
);

public record ResetPasswordRequest(
    string NewPassword
);

public record AssignRoleRequest(long RoleId);
public record AssignWarehouseRequest(long WarehouseId, bool IsPrimary = false);

public record UserResponse(
    long Id,
    string Email,
    string Fullname,
    bool IsActive,
    DateTime CreatedAt,
    List<UserRoleInfo> Roles,
    List<UserWarehouseInfo> Warehouses,
    List<UserProvinceInfo> Scopes,
    string? EmployeeId = null
);

public record UserRoleInfo(long RoleId, string RoleName, string? DisplayName);
public record UserWarehouseInfo(long WarehouseId, string Code, string Name, bool IsPrimary);
public record UserProvinceInfo(long ProvinceId, string Name, string Display);
