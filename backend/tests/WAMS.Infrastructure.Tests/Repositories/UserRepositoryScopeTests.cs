using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WAMS.Application.Common;
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

public class UserRepositoryScopeTests
{
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

    private static AppDbContext Open(DbContextOptions<AppDbContext> o)
        => new AppDbContext(o, BypassTenant());

    // Seeds: GLOBAL province; province "LAMPUNG" with 3 warehouses A,B,C;
    // province "JAMBI" with 1 warehouse D. Returns the seeded ids.
    private static async Task<(long lampungId, long jambiId, long whA, long whB, long whC, long whD)>
        SeedGeoAsync(DbContextOptions<AppDbContext> o)
    {
        await using var db = Open(o);
        db.Companies.Add(new Company { Id = 1, Name = "C", Code = "C001", IsActive = true });

        var global = new Province { Code = ProvinceCodes.Global, Name = "GLOBAL", Display = "Global", IsActive = true };
        var lampung = new Province { Code = "ID-LA", Name = "LAMPUNG", Display = "Lampung", IsActive = true };
        var jambi = new Province { Code = "ID-JA", Name = "JAMBI", Display = "Jambi", IsActive = true };
        db.Provinces.AddRange(global, lampung, jambi);
        await db.SaveChangesAsync();

        WarehouseShadow W(string code, long provinceId) => new()
        {
            Code = code, Name = code, CompanyId = 1, IsActive = true, ProvinceId = provinceId,
            FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow
        };
        var a = W("A", lampung.Id); var b = W("B", lampung.Id); var c = W("C", lampung.Id);
        var d = W("D", jambi.Id);
        db.WarehouseShadows.AddRange(a, b, c, d);
        await db.SaveChangesAsync();

        return (lampung.Id, jambi.Id, a.Id, b.Id, c.Id, d.Id);
    }

    private static async Task SeedUserAsync(
        DbContextOptions<AppDbContext> o, long userId,
        long[] provinceIds, long[] warehouseIds)
    {
        await using var db = Open(o);
        var user = new User { Id = userId, Email = $"u{userId}@t.c", Fullname = "U", CompanyId = 1, IsActive = true };
        foreach (var p in provinceIds) user.UserProvinces.Add(new UserProvince { ProvinceId = p });
        foreach (var w in warehouseIds) user.UserWarehouses.Add(new UserWarehouse { WarehouseId = w });
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    // CASE 3 (the regression): no province, pinned to A and D only.
    // Must see EXACTLY A and D - never the LAMPUNG siblings B, C.
    [Fact]
    public async Task WarehousePinsOnly_DoNotExpandToProvinceSiblings()
    {
        var o = NewDb();
        var geo = await SeedGeoAsync(o);
        await SeedUserAsync(o, userId: 10, provinceIds: [], warehouseIds: [geo.whA, geo.whD]);
        await using var db = Open(o);
        var repo = new UserRepository(db);

        var whIds = await repo.GetUserWarehouseIdsAsync(10, TestContext.Current.CancellationToken);

        whIds.Should().BeEquivalentTo(new[] { geo.whA, geo.whD });
    }

    [Fact]
    public async Task WarehousePinsOnly_DoNotLeakIntoProvinceScope()
    {
        var o = NewDb();
        var geo = await SeedGeoAsync(o);
        await SeedUserAsync(o, userId: 11, provinceIds: [], warehouseIds: [geo.whA]);
        await using var db = Open(o);
        var repo = new UserRepository(db);

        var provinceIds = await repo.GetUserProvinceIdsAsync(11, TestContext.Current.CancellationToken);

        // Only GLOBAL - never the back-derived LAMPUNG.
        provinceIds.Should().NotContain(geo.lampungId);
    }

    // CASE 2 disagreement: list shows a sibling, detail must NOT diverge.
    [Fact]
    public async Task WarehousePinOnly_ListAndDetailAgree_OnSibling()
    {
        var o = NewDb();
        var geo = await SeedGeoAsync(o);
        await SeedUserAsync(o, userId: 12, provinceIds: [], warehouseIds: [geo.whA]);
        await using var db = Open(o);
        var repo = new UserRepository(db);

        var listHasSiblingB = (await repo.GetUserWarehouseIdsAsync(12, TestContext.Current.CancellationToken)).Contains(geo.whB);
        var (existsB, accessB) = await repo.CheckWarehouseAccessAsync(12, geo.whB, TestContext.Current.CancellationToken);

        existsB.Should().BeTrue();
        listHasSiblingB.Should().Be(accessB); // both false: sibling neither listed nor accessible
    }

    // CASE 1: province scope grants all siblings, detail agrees.
    [Fact]
    public async Task ProvinceScope_GrantsAllSiblings_ListAndDetailAgree()
    {
        var o = NewDb();
        var geo = await SeedGeoAsync(o);
        await SeedUserAsync(o, userId: 13, provinceIds: [geo.lampungId], warehouseIds: []);
        await using var db = Open(o);
        var repo = new UserRepository(db);

        var whIds = await repo.GetUserWarehouseIdsAsync(13, TestContext.Current.CancellationToken);
        var (_, accessB) = await repo.CheckWarehouseAccessAsync(13, geo.whB, TestContext.Current.CancellationToken);

        whIds.Should().Contain(new[] { geo.whA, geo.whB, geo.whC });
        whIds.Should().NotContain(geo.whD);
        accessB.Should().BeTrue();
    }

    // CASE 2 mixed: province LAMPUNG + a pin in JAMBI => all Lampung + the one Jambi pin.
    [Fact]
    public async Task ProvincePlusForeignPin_UnionsExactly()
    {
        var o = NewDb();
        var geo = await SeedGeoAsync(o);
        await SeedUserAsync(o, userId: 14, provinceIds: [geo.lampungId], warehouseIds: [geo.whD]);
        await using var db = Open(o);
        var repo = new UserRepository(db);

        var whIds = await repo.GetUserWarehouseIdsAsync(14, TestContext.Current.CancellationToken);

        whIds.Should().BeEquivalentTo(new[] { geo.whA, geo.whB, geo.whC, geo.whD });
    }

    [Fact]
    public async Task UnfilteredReadOnlyLookup_DoesNotBlockSubsequentUserUpdate()
    {
        var o = NewDb();
        await using (var seedDb = Open(o))
        {
            seedDb.Companies.Add(new Company { Id = 1, Name = "C", Code = "C001", IsActive = true });
            seedDb.Users.AddRange(
                new User { Id = 20, Email = "actor@t.c", Fullname = "Actor", CompanyId = 1, IsActive = true },
                new User { Id = 21, Email = "target@t.c", Fullname = "Before", CompanyId = 1, IsActive = true });
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = Open(o);
        var repo = new UserRepository(db);

        // Mirrors UserService authorization: these lookups must remain read-only.
        (await repo.GetByIdUnfilteredReadOnlyAsync(20, TestContext.Current.CancellationToken)).Should().NotBeNull();
        (await repo.GetByIdUnfilteredReadOnlyAsync(21, TestContext.Current.CancellationToken)).Should().NotBeNull();

        // Mirrors the email-conflict check performed before a user update.
        (await repo.GetByEmailAsync("target@t.c", TestContext.Current.CancellationToken)).Should().NotBeNull();

        var target = await repo.GetByIdAsync(21, TestContext.Current.CancellationToken);
        target.Should().NotBeNull();
        target!.Fullname = "After";

        var update = async () =>
        {
            await repo.UpdateAsync(target, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        };

        await update.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDetachedUserWithMembershipGraph()
    {
        var o = NewDb();
        await using (var seedDb = Open(o))
        {
            var company = new Company { Id = 1, Name = "C", Code = "C001", IsActive = true };
            var user = new User
            {
                Id = 22,
                Email = "target@t.c",
                Fullname = "Before",
                CompanyId = company.Id,
                Company = company,
                IsActive = true
            };
            user.UserCompanies.Add(new UserCompany
            {
                Id = 220,
                UserId = user.Id,
                CompanyId = company.Id,
                Company = company,
                User = user
            });
            seedDb.AddRange(company, user);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var tenant = Substitute.For<ITenantContext>();
        tenant.IsSet.Returns(true);
        tenant.CompanyId.Returns(1L);
        await using var db = new AppDbContext(o, tenant);
        var repo = new UserRepository(db, tenant);

        var userToUpdate = await repo.GetByIdAsync(22, TestContext.Current.CancellationToken);
        userToUpdate.Should().NotBeNull();
        userToUpdate!.Fullname = "After";

        await repo.UpdateAsync(userToUpdate, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var verifyDb = Open(o);
        (await verifyDb.Users.SingleAsync(u => u.Id == 22, TestContext.Current.CancellationToken))
            .Fullname.Should().Be("After");
    }

    [Fact]
    public async Task UnfilteredReadOnlyLookup_LoadsCurrentUserAcrossActingCompanyBoundary()
    {
        var o = NewDb();
        await using (var seedDb = Open(o))
        {
            var homeCompany = new Company { Id = 1, Name = "GCU", Code = "GCU", IsActive = true };
            var actingCompany = new Company { Id = 2, Name = "CFF", Code = "CFF", IsActive = true };
            var role = new WAMS.Domain.Entities.Roles.Role
            {
                Id = 1,
                Name = RoleCodes.SuperAdmin,
                DisplayName = "Super Admin",
                IsSystem = true,
                GlobalAccess = true
            };
            var user = new User
            {
                Id = 28,
                Email = "sa@example.com",
                Fullname = "Super Admin",
                CompanyId = homeCompany.Id,
                Company = homeCompany,
                IsActive = true
            };
            user.UserRoles.Add(new UserRole { User = user, Role = role });
            seedDb.AddRange(homeCompany, actingCompany, role, user);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var actingTenant = Substitute.For<ITenantContext>();
        actingTenant.IsSet.Returns(true);
        actingTenant.CompanyId.Returns(2L);
        await using var db = new AppDbContext(o, actingTenant);
        var repo = new UserRepository(db);

        var currentUser = await repo.GetByIdUnfilteredReadOnlyAsync(28, TestContext.Current.CancellationToken);

        currentUser.Should().NotBeNull();
        currentUser!.Company.Code.Should().Be("GCU");
        currentUser.UserRoles.Should().ContainSingle(ur => ur.Role.Name == RoleCodes.SuperAdmin);
    }

    [Fact]
    public async Task UserListAndDetail_StartFromLiveMembershipsInActingCompany()
    {
        var o = NewDb();
        await using (var seedDb = Open(o))
        {
            var homeCompany = new Company { Id = 1, Name = "Home", Code = "HOME", IsActive = true };
            var actingCompany = new Company { Id = 2, Name = "Acting", Code = "ACT", IsActive = true };
            var user = new User
            {
                Id = 40,
                Email = "secondary@example.com",
                Fullname = "Secondary Company User",
                CompanyId = homeCompany.Id,
                Company = homeCompany,
                IsActive = true
            };
            user.UserCompanies.Add(new UserCompany
            {
                UserId = user.Id,
                CompanyId = actingCompany.Id,
                User = user,
                Company = actingCompany
            });
            seedDb.AddRange(homeCompany, actingCompany, user);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var actingTenant = Substitute.For<ITenantContext>();
        actingTenant.IsSet.Returns(true);
        actingTenant.CompanyId.Returns(2L);
        await using var db = new AppDbContext(o, actingTenant);
        var repo = new UserRepository(db, actingTenant);

        var (items, total) = await repo.GetAllAsync(
            new DataTableQuery { Page = 1, Limit = 20 },
            TestContext.Current.CancellationToken);
        var detail = await repo.GetByIdAsync(40, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(u => u.Id == 40);
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(40);
    }
}
