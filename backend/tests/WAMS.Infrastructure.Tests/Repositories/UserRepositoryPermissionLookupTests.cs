using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Repositories.Users;
using Xunit;

namespace WAMS.Infrastructure.Tests.Repositories;

/// <summary>
/// Reverse permission lookup -who holds a given permission in a warehouse. Backs the work order
/// PIC picker, which follows workorder.workorder.execute eligibility. Resolution must agree with
/// RbacService.HasPermissionAsync, otherwise a user can hold a permission but not appear in the
/// list that permission exists to build.
/// </summary>
public class UserRepositoryPermissionLookupTests
{
    private const long CompanyId = 1;
    private const long WarehouseId = 100;
    private const long OtherWarehouseId = 200;
    private const string Key = Permissions.WorkOrder.Execute;

    private static ITenantContext BypassTenant()
    {
        var tc = Substitute.For<ITenantContext>();
        tc.IsSet.Returns(false);
        tc.CompanyId.Returns((long?)null);
        return tc;
    }

    private static DbContextOptions<AppDbContext> NewDb()
        => new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static AppDbContext Open(DbContextOptions<AppDbContext> o) => new(o, BypassTenant());

    // One company, one permission, and two roles: FIELD holds the permission, ADMIN does not.
    private static async Task<(long fieldRoleId, long adminRoleId, long permissionId)> SeedBaseAsync(
        DbContextOptions<AppDbContext> o, bool adminIsGlobal = false)
    {
        await using var db = Open(o);
        db.Companies.Add(new Company { Id = CompanyId, Name = "C", Code = "C001", IsActive = true });

        var permission = new Permission { Module = "workorder", Resource = "workorder", Action = "execute" };
        var noise = new Permission { Module = "workorder", Resource = "workorder", Action = "update" };
        db.Permissions.AddRange(permission, noise);

        var field = new Role { Name = "FOREMAN", CompanyId = CompanyId, GlobalAccess = false };
        var admin = new Role { Name = "WAREHOUSE_ADMIN", CompanyId = CompanyId, GlobalAccess = adminIsGlobal };
        db.Roles.AddRange(field, admin);
        await db.SaveChangesAsync();

        db.RolePermissions.Add(new RolePermission { RoleId = field.Id, PermissionId = permission.Id });
        // Admin holds a different work order permission - proves we match the exact key, not the module.
        db.RolePermissions.Add(new RolePermission { RoleId = admin.Id, PermissionId = noise.Id });
        await db.SaveChangesAsync();

        return (field.Id, admin.Id, permission.Id);
    }

    private static async Task SeedUserAsync(
        DbContextOptions<AppDbContext> o, long userId, long roleId, long? warehouseId,
        bool isActive = true)
    {
        await using var db = Open(o);
        var user = new User
        {
            Id = userId, Email = $"u{userId}@t.c", Fullname = $"User {userId}",
            CompanyId = CompanyId, IsActive = isActive
        };
        user.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        if (warehouseId is not null)
            user.UserWarehouses.Add(new UserWarehouse { UserId = userId, WarehouseId = warehouseId.Value });
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    private static async Task AddOverrideAsync(
        DbContextOptions<AppDbContext> o, long userId, long permissionId, bool granted, DateTime? expiresAt = null)
    {
        await using var db = Open(o);
        db.UserPermissions.Add(new UserPermission
        {
            UserId = userId, PermissionId = permissionId, IsGranted = granted,
            GrantedBy = 1, ExpiresAt = expiresAt
        });
        await db.SaveChangesAsync();
    }

    private static async Task<List<User>> QueryAsync(DbContextOptions<AppDbContext> o)
    {
        await using var db = Open(o);
        var repo = new UserRepository(db);
        return await repo.GetUsersByPermissionAndWarehouseAsync(CompanyId, WarehouseId, Key, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RoleHoldsPermission_UserInWarehouse_IsReturned()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 1, fieldRole, WarehouseId);

        var result = await QueryAsync(o);

        result.Should().ContainSingle(u => u.Id == 1);
    }

    [Fact]
    public async Task RoleLacksPermission_IsNotReturned()
    {
        var o = NewDb();
        var (_, adminRole, _) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 2, adminRole, WarehouseId);

        var result = await QueryAsync(o);

        result.Should().BeEmpty("holding workorder.workorder.update must not confer PIC eligibility");
    }

    [Fact]
    public async Task UserInDifferentWarehouse_IsNotReturned()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 3, fieldRole, OtherWarehouseId);

        var result = await QueryAsync(o);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task InactiveUser_IsNotReturned()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 4, fieldRole, WarehouseId, isActive: false);

        var result = await QueryAsync(o);

        result.Should().BeEmpty();
    }

    // A global-access role reaches every warehouse, matching GetUsersByRolesAndWarehouseAsync.
    [Fact]
    public async Task GlobalAccessRole_NeedsNoWarehouseAssignment()
    {
        var o = NewDb();
        await using (var db = Open(o))
        {
            db.Companies.Add(new Company { Id = CompanyId, Name = "C", Code = "C001", IsActive = true });
            var permission = new Permission { Module = "workorder", Resource = "workorder", Action = "execute" };
            db.Permissions.Add(permission);
            var role = new Role { Name = "ROVING_FOREMAN", CompanyId = CompanyId, GlobalAccess = true };
            db.Roles.Add(role);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            var user = new User { Id = 5, Email = "u5@t.c", Fullname = "Roving", CompanyId = CompanyId, IsActive = true };
            user.UserRoles.Add(new UserRole { UserId = 5, RoleId = role.Id });
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await QueryAsync(o);

        result.Should().ContainSingle(u => u.Id == 5);
    }

    [Fact]
    public async Task UserLevelGrant_MakesUserEligibleWithoutTheRole()
    {
        var o = NewDb();
        var (_, adminRole, permissionId) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 6, adminRole, WarehouseId);
        await AddOverrideAsync(o, 6, permissionId, granted: true);

        var result = await QueryAsync(o);

        result.Should().ContainSingle(u => u.Id == 6);
    }

    [Fact]
    public async Task UserLevelDeny_RemovesUserDespiteRoleGrant()
    {
        var o = NewDb();
        var (fieldRole, _, permissionId) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 7, fieldRole, WarehouseId);
        await AddOverrideAsync(o, 7, permissionId, granted: false);

        var result = await QueryAsync(o);

        result.Should().BeEmpty("an active deny outranks the role grant, same as RbacService");
    }

    [Fact]
    public async Task ExpiredDeny_IsIgnored()
    {
        var o = NewDb();
        var (fieldRole, _, permissionId) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 8, fieldRole, WarehouseId);
        await AddOverrideAsync(o, 8, permissionId, granted: false, expiresAt: DateTime.UtcNow.AddDays(-1));

        var result = await QueryAsync(o);

        result.Should().ContainSingle(u => u.Id == 8);
    }

    [Fact]
    public async Task ExpiredGrant_DoesNotQualify()
    {
        var o = NewDb();
        var (_, adminRole, permissionId) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 9, adminRole, WarehouseId);
        await AddOverrideAsync(o, 9, permissionId, granted: true, expiresAt: DateTime.UtcNow.AddDays(-1));

        var result = await QueryAsync(o);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UnknownPermissionKey_ReturnsEmptyRatherThanEveryone()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 10, fieldRole, WarehouseId);

        await using var db = Open(o);
        var repo = new UserRepository(db);
        var result = await repo.GetUsersByPermissionAndWarehouseAsync(
            CompanyId, WarehouseId, "does.not.exist", TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    // Deliberately uses a real module and resource with a bogus action: a lookup that matched on
    // anything less than the full triple would resolve to some other workorder permission and hand
    // back a populated list. Loosening the match to a wildcard-style prefix must fail here.
    [Fact]
    public async Task KeyMatchIsExact_PartialKeyDoesNotResolveToAnotherPermission()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 11, fieldRole, WarehouseId);

        await using var db = Open(o);
        var repo = new UserRepository(db);
        var result = await repo.GetUsersByPermissionAndWarehouseAsync(
            CompanyId, WarehouseId, "workorder.workorder.nonexistent", TestContext.Current.CancellationToken);

        result.Should().BeEmpty("module and resource matching must not be enough to resolve a permission");
    }

    // --- Province scope ------------------------------------------------------------------
    // A province-scoped user holds no UserWarehouse pin at all, so a pin-only lookup drops them
    // from the PIC list even though GetUserWarehouseIdsAsync / CheckWarehouseAccessAsync let them
    // open the warehouse. Both directions must resolve scope the same way.

    private const long ProvinceId = 50;
    private const long OtherProvinceId = 51;

    private static async Task SeedWarehouseInProvinceAsync(
        DbContextOptions<AppDbContext> o, long warehouseId, long provinceId, string provinceCode)
    {
        await using var db = Open(o);
        if (!await db.Provinces.AnyAsync(p => p.Id == provinceId))
            db.Provinces.Add(new Province
            {
                Id = provinceId, Code = provinceCode, Name = provinceCode, Display = provinceCode
            });
        db.WarehouseShadows.Add(new WarehouseShadow
        {
            Id = warehouseId, Code = $"WH{warehouseId}", Name = $"Warehouse {warehouseId}",
            CompanyId = CompanyId, ProvinceId = provinceId, IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task AssignProvinceAsync(DbContextOptions<AppDbContext> o, long userId, long provinceId)
    {
        await using var db = Open(o);
        db.UserProvinces.Add(new UserProvince { UserId = userId, ProvinceId = provinceId });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ProvinceScopedUser_WithNoWarehousePin_IsReturned()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedWarehouseInProvinceAsync(o, WarehouseId, ProvinceId, "ID-SU");
        await SeedUserAsync(o, 13, fieldRole, warehouseId: null);
        await AssignProvinceAsync(o, 13, ProvinceId);

        var result = await QueryAsync(o);

        result.Should().ContainSingle(u => u.Id == 13,
            "the warehouse sits in a province the user is scoped to, so they can already act on it");
    }

    [Fact]
    public async Task ProvinceScopedUser_InAnotherProvince_IsNotReturned()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedWarehouseInProvinceAsync(o, WarehouseId, ProvinceId, "ID-SU");
        await SeedUserAsync(o, 14, fieldRole, warehouseId: null);
        await AssignProvinceAsync(o, 14, OtherProvinceId);

        var result = await QueryAsync(o);

        result.Should().BeEmpty();
    }

    // GetUserProvinceIdsAsync unions GLOBAL into every user's province scope, so a warehouse
    // parked in GLOBAL (transit/on-water) is reachable by everyone holding the permission.
    [Fact]
    public async Task GlobalProvinceWarehouse_ReachesUserWithNoScopeOfTheirOwn()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedWarehouseInProvinceAsync(o, WarehouseId, ProvinceId, ProvinceCodes.Global);
        await SeedUserAsync(o, 15, fieldRole, warehouseId: null);

        var result = await QueryAsync(o);

        result.Should().ContainSingle(u => u.Id == 15);
    }

    // Same scope rule, other lookup: this one drives budget plan approver notifications.
    [Fact]
    public async Task RolesLookup_ProvinceScopedUser_IsReturned()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedWarehouseInProvinceAsync(o, WarehouseId, ProvinceId, "ID-SU");
        await SeedUserAsync(o, 16, fieldRole, warehouseId: null);
        await AssignProvinceAsync(o, 16, ProvinceId);

        await using var db = Open(o);
        var repo = new UserRepository(db);
        var result = await repo.GetUsersByRolesAndWarehouseAsync(
            CompanyId, WarehouseId, ["FOREMAN"], TestContext.Current.CancellationToken);

        result.Should().ContainSingle(u => u.Id == 16);
    }

    [Fact]
    public async Task RolesLookup_ProvinceScopedUser_InAnotherProvince_IsNotReturned()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedWarehouseInProvinceAsync(o, WarehouseId, ProvinceId, "ID-SU");
        await SeedUserAsync(o, 17, fieldRole, warehouseId: null);
        await AssignProvinceAsync(o, 17, OtherProvinceId);

        await using var db = Open(o);
        var repo = new UserRepository(db);
        var result = await repo.GetUsersByRolesAndWarehouseAsync(
            CompanyId, WarehouseId, ["FOREMAN"], TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task MalformedKey_ReturnsEmpty()
    {
        var o = NewDb();
        var (fieldRole, _, _) = await SeedBaseAsync(o);
        await SeedUserAsync(o, 12, fieldRole, WarehouseId);

        await using var db = Open(o);
        var repo = new UserRepository(db);

        foreach (var bad in new[] { "workorder", "workorder.workorder", "a.b.c.d", "" })
        {
            var result = await repo.GetUsersByPermissionAndWarehouseAsync(
                CompanyId, WarehouseId, bad, TestContext.Current.CancellationToken);
            result.Should().BeEmpty($"'{bad}' is not a valid permission key");
        }
    }
}
