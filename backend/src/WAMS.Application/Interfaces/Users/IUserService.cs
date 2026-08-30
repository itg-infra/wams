namespace WAMS.Application.Interfaces.Users;

using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Users;
using WAMS.Application.Common;

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PaginatedResponse<UserResponse>> GetAllAsync(DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<UserResponse> StreamAllAsync(DataTableQuery query, int limit, CancellationToken ct = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, long createdBy, CancellationToken ct = default);
    Task<UserResponse> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task ResetPasswordAsync(long id, ResetPasswordRequest request, long actorUserId, CancellationToken ct = default);
    Task AssignRoleAsync(long userId, AssignRoleRequest request, CancellationToken ct = default);
    Task RemoveRoleAsync(long userId, long roleId, CancellationToken ct = default);
    Task AssignWarehouseAsync(long userId, AssignWarehouseRequest request, CancellationToken ct = default);
    Task RemoveWarehouseAsync(long userId, long warehouseId, CancellationToken ct = default);
    Task<bool> HasGlobalAccessAsync(long userId, CancellationToken ct = default);
    Task<List<long>> GetUserWarehouseIdsAsync(long userId, CancellationToken ct = default);
    Task<List<long>> GetUserProvinceIdsAsync(long userId, CancellationToken ct = default);
}
