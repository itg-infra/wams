namespace WAMS.E2eReset;

using Microsoft.EntityFrameworkCore;
using WAMS.Domain.Constants;
using WAMS.Infrastructure.Data;

public sealed record UserCompanyMembershipReconciliation(
    IReadOnlyList<long> UsersMissingLiveMembership,
    IReadOnlyList<string> DuplicateLiveMembershipGroups,
    IReadOnlyList<string> OrphanedRoleAssignments,
    IReadOnlyList<string> OrphanedWarehouseAssignments,
    IReadOnlyList<string> OrphanedProvinceAssignments,
    IReadOnlyList<string> OrphanedPermissionAssignments,
    IReadOnlyList<string> RoleCompanyMismatches,
    IReadOnlyList<string> WarehouseCompanyMismatches,
    IReadOnlyList<long> RefreshTokensMissingLiveMembership,
    IReadOnlyList<long> RefreshTokensWithVersionMismatch)
{
    public bool IsHealthy =>
        UsersMissingLiveMembership.Count == 0 &&
        DuplicateLiveMembershipGroups.Count == 0 &&
        OrphanedRoleAssignments.Count == 0 &&
        OrphanedWarehouseAssignments.Count == 0 &&
        OrphanedProvinceAssignments.Count == 0 &&
        OrphanedPermissionAssignments.Count == 0 &&
        RoleCompanyMismatches.Count == 0 &&
        WarehouseCompanyMismatches.Count == 0 &&
        RefreshTokensMissingLiveMembership.Count == 0 &&
        RefreshTokensWithVersionMismatch.Count == 0;
}

/// <summary>
/// Read-only post-migration invariant check. It deliberately does not repair rows: a
/// deployment must stop and preserve the database for operator diagnosis if an invariant fails.
/// </summary>
public sealed class UserCompanyMembershipReconciler(AppDbContext db)
{
    public async Task<UserCompanyMembershipReconciliation> ReconcileAsync(CancellationToken ct = default)
    {
        var liveMemberships = db.UserCompanies.IgnoreQueryFilters().Where(m => m.RemovedAt == null);

        var missingUsers = await db.Users.IgnoreQueryFilters()
            .Where(u => u.DeletedAt == null && !liveMemberships.Any(m => m.UserId == u.Id && m.CompanyId == u.CompanyId))
            .Select(u => u.Id)
            .ToListAsync(ct);
        var duplicateGroups = await liveMemberships
            .GroupBy(m => new { m.UserId, m.CompanyId })
            .Where(g => g.Count() > 1)
            .Select(g => $"user={g.Key.UserId},company={g.Key.CompanyId}")
            .ToListAsync(ct);

        var orphanRoles = await db.UserCompanyRoles
            .Where(x => !liveMemberships.Any(m => m.Id == x.UserCompanyId))
            .Select(x => $"membership={x.UserCompanyId},role={x.RoleId}")
            .ToListAsync(ct);
        var orphanWarehouses = await db.UserCompanyWarehouses
            .Where(x => !liveMemberships.Any(m => m.Id == x.UserCompanyId))
            .Select(x => $"membership={x.UserCompanyId},warehouse={x.WarehouseId}")
            .ToListAsync(ct);
        var orphanProvinces = await db.UserCompanyProvinces
            .Where(x => !liveMemberships.Any(m => m.Id == x.UserCompanyId))
            .Select(x => $"membership={x.UserCompanyId},province={x.ProvinceId}")
            .ToListAsync(ct);
        var orphanPermissions = await db.UserCompanyPermissions
            .Where(x => !liveMemberships.Any(m => m.Id == x.UserCompanyId))
            .Select(x => $"membership={x.UserCompanyId},permission={x.PermissionId}")
            .ToListAsync(ct);

        var roleCompanyMismatches = await db.UserCompanyRoles
            .Join(liveMemberships, assignment => assignment.UserCompanyId, membership => membership.Id,
                (assignment, membership) => new { assignment, membership })
            .Join(db.Roles.IgnoreQueryFilters(), joined => joined.assignment.RoleId, role => role.Id,
                (joined, role) => new { joined.assignment, joined.membership, role })
            .Where(x => x.role.Name == RoleCodes.SuperAdmin ||
                (x.role.CompanyId.HasValue && x.role.CompanyId != x.membership.CompanyId))
            .Select(x => $"membership={x.assignment.UserCompanyId},role={x.assignment.RoleId},roleCompany={x.role.CompanyId},roleName={x.role.Name}")
            .ToListAsync(ct);
        var warehouseCompanyMismatches = await db.UserCompanyWarehouses
            .Join(liveMemberships, assignment => assignment.UserCompanyId, membership => membership.Id,
                (assignment, membership) => new { assignment, membership })
            .Join(db.WarehouseShadows.IgnoreQueryFilters(), joined => joined.assignment.WarehouseId, warehouse => warehouse.Id,
                (joined, warehouse) => new { joined.assignment, joined.membership, warehouse })
            .Where(x => x.warehouse.CompanyId != x.membership.CompanyId)
            .Select(x => $"membership={x.assignment.UserCompanyId},warehouse={x.assignment.WarehouseId},membershipCompany={x.membership.CompanyId},warehouseCompany={x.warehouse.CompanyId}")
            .ToListAsync(ct);

        var refreshTokensMissingMembership = await db.RefreshTokens
            .Where(t => t.UserCompanyId != null && !liveMemberships.Any(m => m.Id == t.UserCompanyId))
            .Select(t => t.Id)
            .ToListAsync(ct);
        var refreshTokensWithVersionMismatch = await db.RefreshTokens
            .Where(t => t.UserCompanyId != null && t.MembershipAuthorizationVersion != null &&
                liveMemberships.Any(m => m.Id == t.UserCompanyId && m.AuthorizationVersion != t.MembershipAuthorizationVersion))
            .Select(t => t.Id)
            .ToListAsync(ct);

        return new UserCompanyMembershipReconciliation(
            missingUsers,
            duplicateGroups,
            orphanRoles,
            orphanWarehouses,
            orphanProvinces,
            orphanPermissions,
            roleCompanyMismatches,
            warehouseCompanyMismatches,
            refreshTokensMissingMembership,
            refreshTokensWithVersionMismatch);
    }
}
