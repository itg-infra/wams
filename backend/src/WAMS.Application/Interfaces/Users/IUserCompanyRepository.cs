namespace WAMS.Application.Interfaces.Users;

using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Entities.Roles;

public interface IUserCompanyRepository
{
    Task<UserCompany?> GetLiveAsync(long userId, long companyId, CancellationToken ct = default);
    Task<UserCompany?> GetAnyAsync(long userId, long companyId, CancellationToken ct = default);
    Task<List<UserCompany>> GetForUserAsync(long userId, bool includeRemoved = false, CancellationToken ct = default);
    Task<List<UserCompany>> GetAllForUserSystemAsync(long userId, CancellationToken ct = default);
    Task<User?> GetUserAsync(long userId, CancellationToken ct = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<Company?> GetCompanyAsync(long companyId, CancellationToken ct = default);
    Task<List<Role>> GetRolesByIdsAsync(IReadOnlyCollection<long> roleIds, CancellationToken ct = default);
    Task<List<WarehouseShadow>> GetWarehousesByIdsAsync(IReadOnlyCollection<long> warehouseIds, CancellationToken ct = default);
    Task<List<Province>> GetProvincesByIdsAsync(IReadOnlyCollection<long> provinceIds, CancellationToken ct = default);
    Task<Permission?> GetPermissionByIdAsync(long permissionId, CancellationToken ct = default);
    Task AddAsync(UserCompany membership, CancellationToken ct = default);
    Task ClearAssignmentsAsync(long userCompanyId, CancellationToken ct = default);
    Task ClearProvinceAssignmentsAsync(long userCompanyId, CancellationToken ct = default);
    Task IncrementAuthorizationVersionAsync(long userCompanyId, CancellationToken ct = default);
    Task RevokeRefreshTokensAsync(long userCompanyId, CancellationToken ct = default);
}
