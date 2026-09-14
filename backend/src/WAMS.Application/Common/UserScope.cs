namespace WAMS.Application.Common;

using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;

/// <summary>Centralizes the compatibility boundary for request-scoped membership resolution.</summary>
public static class UserScope
{
    public static Task<bool> HasGlobalAccessAsync(
        IRbacService rbac,
        ITenantContext? tenant,
        long userId,
        CancellationToken ct = default)
        => tenant?.UserCompanyId.HasValue == true
            ? rbac.HasGlobalAccessAsync(userId, tenant.UserCompanyId, ct)
            : rbac.HasGlobalAccessAsync(userId, ct);

    public static Task<List<long>> GetWarehouseIdsAsync(
        IUserRepository users,
        ITenantContext? tenant,
        long userId,
        CancellationToken ct = default)
        => tenant?.UserCompanyId.HasValue == true
            ? users.GetUserWarehouseIdsAsync(userId, tenant.UserCompanyId, ct)
            : users.GetUserWarehouseIdsAsync(userId, ct);

    public static Task<List<long>> GetProvinceIdsAsync(
        IUserRepository users,
        ITenantContext? tenant,
        long userId,
        CancellationToken ct = default)
        => tenant?.UserCompanyId.HasValue == true
            ? users.GetUserProvinceIdsAsync(userId, tenant.UserCompanyId, ct)
            : users.GetUserProvinceIdsAsync(userId, ct);
}
