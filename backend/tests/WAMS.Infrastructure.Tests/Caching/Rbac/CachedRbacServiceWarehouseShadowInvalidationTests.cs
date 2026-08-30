namespace WAMS.Infrastructure.Tests.Caching;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Infrastructure.Caching.Rbac;
using WAMS.Infrastructure.Caching.Warehouses;
using Xunit;

/// <summary>
/// Reproduces the bug where changing a user's effective permissions via CachedRbacService
/// (role permission grant/revoke, role update, user override) left CachedWarehouseShadowService's
/// per-user warehouse list cached and stale - e.g. a role temporarily granted "*.*.*" so
/// HasGlobalAccessAsync returned true and WarehouseShadowService.GetAllAsync returned every
/// warehouse; after revoking the permission, the previously-cached "all warehouses" result kept
/// being served for up to the local WarehouseShadow TTL because nothing told
/// CachedWarehouseShadowService its cached entries were now invalid.
///
/// Both decorators share one HybridCache instance here (as they do via DI in Program.cs), so a
/// tag cleared by one is visible to the other.
/// </summary>
public sealed class CachedRbacServiceWarehouseShadowInvalidationTests : IDisposable
{
    private readonly CacheTestFixture _fx = new();
    private readonly IRbacService _rbacInner = Substitute.For<IRbacService>();
    private readonly IWarehouseShadowService _warehouseInner = Substitute.For<IWarehouseShadowService>();
    private readonly CachedRbacService _rbacSut;
    private readonly CachedWarehouseShadowService _warehouseSut;

    private static readonly RoleResponse AnyRole = new(1, "admin", null, null, false, false, DateTime.UtcNow, []);
    private static readonly WarehouseQuery AnyQuery = new();

    public CachedRbacServiceWarehouseShadowInvalidationTests()
    {
        _rbacSut = new CachedRbacService(_rbacInner, _fx.Cache, _fx.Options);
        _warehouseSut = new CachedWarehouseShadowService(_warehouseInner, _fx.Cache, _fx.Options);
    }

    public void Dispose() => _fx.Dispose();

    private static PaginatedResponse<WarehouseResponse> AllWarehouses(int count) => new(
        true,
        Enumerable.Range(1, count)
            .Select(i => new WarehouseResponse(i, $"WH{i}", $"Warehouse {i}", null, true, DateTime.UtcNow, DateTime.UtcNow))
            .ToList(),
        new PaginationMeta(1, 20, count, 1));

    [Fact]
    public async Task UpdateRoleAsync_ClearsWarehouseShadowCacheForAllUsers()
    {
        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(5)); // e.g. role temporarily has *.*.*, sees everything

        var stale = await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);
        stale.Data.Should().HaveCount(5);

        _rbacInner.UpdateRoleAsync(1, Arg.Any<UpdateRoleRequest>(), Arg.Any<CancellationToken>()).Returns(AnyRole);
        await _rbacSut.UpdateRoleAsync(1, new UpdateRoleRequest(null, null, null), TestContext.Current.CancellationToken);

        // permission revoked - inner now reflects the user's real, scoped warehouse set
        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(2));

        var fresh = await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);
        fresh.Data.Should().HaveCount(2, "UpdateRoleAsync must invalidate cached warehouse-shadow results too");
    }

    [Fact]
    public async Task AssignPermissionAsync_ClearsWarehouseShadowCacheForAllUsers()
    {
        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(5));
        await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);

        await _rbacSut.AssignPermissionAsync(1, new AssignPermissionRequest(99), null, TestContext.Current.CancellationToken);

        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(2));
        var fresh = await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);
        fresh.Data.Should().HaveCount(2, "AssignPermissionAsync must invalidate cached warehouse-shadow results too");
    }

    [Fact]
    public async Task RemovePermissionAsync_ClearsWarehouseShadowCacheForAllUsers()
    {
        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(5));
        await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);

        await _rbacSut.RemovePermissionAsync(1, 99, TestContext.Current.CancellationToken);

        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(2));
        var fresh = await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);
        fresh.Data.Should().HaveCount(2, "RemovePermissionAsync must invalidate cached warehouse-shadow results too");
    }

    [Fact]
    public async Task SyncPermissionsAsync_ClearsWarehouseShadowCacheForAllUsers()
    {
        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(5));
        await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);

        await _rbacSut.SyncPermissionsAsync(1, new SyncPermissionsRequest([]), null, TestContext.Current.CancellationToken);

        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(2));
        var fresh = await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);
        fresh.Data.Should().HaveCount(2, "SyncPermissionsAsync must invalidate cached warehouse-shadow results too");
    }

    [Fact]
    public async Task GrantUserPermissionAsync_ClearsWarehouseShadowCacheForThatUserOnly()
    {
        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(5));
        _warehouseInner.GetAllAsync(2, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(5));
        await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);
        await _warehouseSut.GetAllAsync(2, AnyQuery, TestContext.Current.CancellationToken);

        await _rbacSut.GrantUserPermissionAsync(1, 99, new(null, null), grantedBy: 0, TestContext.Current.CancellationToken);

        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(2));
        _warehouseInner.GetAllAsync(2, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(2));

        var user1 = await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);
        var user2 = await _warehouseSut.GetAllAsync(2, AnyQuery, TestContext.Current.CancellationToken);

        user1.Data.Should().HaveCount(2, "user 1's warehouse-shadow cache was cleared");
        user2.Data.Should().HaveCount(5, "user 2's warehouse-shadow cache must NOT be cleared by another user's override change");
    }

    [Fact]
    public async Task DenyUserPermissionAsync_ClearsWarehouseShadowCacheForThatUserOnly()
    {
        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(5));
        await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);

        await _rbacSut.DenyUserPermissionAsync(1, 99, new(null, null), grantedBy: 0, TestContext.Current.CancellationToken);

        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(2));
        var fresh = await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);
        fresh.Data.Should().HaveCount(2, "DenyUserPermissionAsync must invalidate that user's cached warehouse-shadow results too");
    }

    [Fact]
    public async Task RemoveUserPermissionAsync_ClearsWarehouseShadowCacheForThatUserOnly()
    {
        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(5));
        await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);

        await _rbacSut.RemoveUserPermissionAsync(1, 99, TestContext.Current.CancellationToken);

        _warehouseInner.GetAllAsync(1, Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns(AllWarehouses(2));
        var fresh = await _warehouseSut.GetAllAsync(1, AnyQuery, TestContext.Current.CancellationToken);
        fresh.Data.Should().HaveCount(2, "RemoveUserPermissionAsync must invalidate that user's cached warehouse-shadow results too");
    }
}
