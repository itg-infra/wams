namespace WAMS.Application.Interfaces.Users;

using WAMS.Application.Common;
using WAMS.Application.DTOs.Users;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.WorkOrders;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<User?> GetByIdUnfilteredAsync(long id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken ct = default);
    Task<(List<User> Items, int TotalCount)> GetAllAsync(DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<UserResponse> StreamAllAsync(DataTableQuery query, int limit, CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsAsync(long id, CancellationToken ct = default);
    Task<List<long>> GetUserWarehouseIdsAsync(long userId, CancellationToken ct = default);
    Task<List<long>> GetUserProvinceIdsAsync(long userId, CancellationToken ct = default);
    Task ReplaceUserProvincesAsync(long userId, IReadOnlyCollection<long> provinceIds, CancellationToken ct = default);
    Task<List<User>> GetUsersByRolesAndWarehouseAsync(long companyId, long warehouseId, IReadOnlyCollection<string> roleNames, CancellationToken ct = default);

    /// <summary>
    /// Reverse lookup: users in this warehouse holding <paramref name="permissionKey"/>.
    /// Powers eligibility lists (PIC candidates, assignee pickers). Matches the key exactly -
    /// wildcard grants do not qualify. See Permissions.WorkOrder.Execute.
    /// </summary>
    Task<List<User>> GetUsersByPermissionAndWarehouseAsync(long companyId, long warehouseId, string permissionKey, CancellationToken ct = default);
    Task<bool> HasGlobalAccessAsync(long userId, CancellationToken ct = default);
    Task<(bool WarehouseExists, bool HasAccess)> CheckWarehouseAccessAsync(long userId, long warehouseId, CancellationToken ct = default);
    Task AssignWarehouseAsync(long userId, long warehouseId, bool isPrimary, CancellationToken ct = default);
    Task RemoveWarehouseAsync(long userId, long warehouseId, CancellationToken ct = default);
    Task ClearWarehouseAssignmentsAsync(long userId, CancellationToken ct = default);
}
