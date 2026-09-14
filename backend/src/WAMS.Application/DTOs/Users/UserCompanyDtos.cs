namespace WAMS.Application.DTOs.Users;

public record AddUserCompanyRequest(
    List<long>? RoleIds = null,
    List<long>? WarehouseIds = null,
    long? PrimaryWarehouseId = null,
    List<long>? ProvinceIds = null);

public record UserCompanyResponse(
    long Id,
    long UserId,
    long CompanyId,
    string CompanyCode,
    string CompanyName,
    int AuthorizationVersion,
    List<UserRoleInfo> Roles,
    List<UserWarehouseInfo> Warehouses,
    List<UserProvinceInfo> Scopes,
    DateTime CreatedAt,
    DateTime? RemovedAt);
