namespace WAMS.Application.Interfaces.Users;

using WAMS.Application.DTOs.Users;
using WAMS.Application.DTOs.Roles;

public interface IUserCompanyService
{
    Task<UserCompanyResponse> AddMembershipAsync(
        long userId,
        long companyId,
        long actorUserId,
        AddUserCompanyRequest? request = null,
        CancellationToken ct = default);

    Task RemoveMembershipAsync(
        long userId,
        long companyId,
        long actorUserId,
        CancellationToken ct = default);

    Task<UserCompanyResponse> RestoreMembershipAsync(
        long userId,
        long companyId,
        long actorUserId,
        CancellationToken ct = default);

    Task<UserCompanyResponse?> GetMembershipAsync(
        long userId,
        long companyId,
        CancellationToken ct = default);

    Task<List<UserCompanyResponse>> GetMembershipsAsync(
        long userId,
        bool includeAllCompanies = false,
        CancellationToken ct = default);

    Task<UserResponse> CreateUserAsync(
        CreateUserRequest request,
        long actingCompanyId,
        long createdBy,
        CancellationToken ct = default);

    Task AssignRoleAsync(long userId, long companyId, long roleId, long actorUserId, CancellationToken ct = default);
    Task RemoveRoleAsync(long userId, long companyId, long roleId, long actorUserId, CancellationToken ct = default);
    Task AssignWarehouseAsync(long userId, long companyId, long warehouseId, bool isPrimary, long actorUserId, CancellationToken ct = default);
    Task RemoveWarehouseAsync(long userId, long companyId, long warehouseId, long actorUserId, CancellationToken ct = default);
    Task ReplaceProvincesAsync(long userId, long companyId, IReadOnlyCollection<long> provinceIds, long actorUserId, CancellationToken ct = default);
    Task GrantPermissionAsync(long userId, long companyId, long permissionId, bool isGranted, UserPermissionOverrideRequest request, long actorUserId, CancellationToken ct = default);
    Task RemovePermissionAsync(long userId, long companyId, long permissionId, long actorUserId, CancellationToken ct = default);
    Task<List<UserPermissionOverrideResponse>> GetPermissionOverridesAsync(long userId, long companyId, CancellationToken ct = default);
}
