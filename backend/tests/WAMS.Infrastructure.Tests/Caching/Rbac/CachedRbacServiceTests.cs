namespace WAMS.Infrastructure.Tests.Caching;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Infrastructure.Caching.Rbac;
using Xunit;

public sealed class CachedRbacServiceTests : IDisposable
{
    private readonly CacheTestFixture _fx = new();
    private readonly IRbacService _inner = Substitute.For<IRbacService>();
    private readonly CachedRbacService _sut;

    private static readonly RoleResponse AnyRole = new(1, "admin", null, null, false, false, DateTime.UtcNow, []);
    private static readonly UserPermissionOverrideRequest AnyOverride = new(null, null);

    public CachedRbacServiceTests()
    {
        _sut = new CachedRbacService(_inner, _fx.Cache, _fx.Options);
    }

    public void Dispose() => _fx.Dispose();

    // HasPermissionAsync 
    [Fact]
    public async Task HasPermissionAsync_CachesResult_InnerCalledOnce()
    {
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);

        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);

        await _inner.Received(1).HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasPermissionAsync_DifferentUsers_CachedSeparately()
    {
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        _inner.HasPermissionAsync(2, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);

        var u1 = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        var u2 = await _sut.HasPermissionAsync(2, "m", "r", "a", TestContext.Current.CancellationToken);

        u1.Should().BeTrue();
        u2.Should().BeFalse();
        await _inner.Received(1).HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>());
        await _inner.Received(1).HasPermissionAsync(2, "m", "r", "a", Arg.Any<CancellationToken>());
    }

    // HasGlobalAccessAsync 
    [Fact]
    public async Task HasGlobalAccessAsync_CachesResult_InnerCalledOnce()
    {
        _inner.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.HasGlobalAccessAsync(1, TestContext.Current.CancellationToken);
        await _sut.HasGlobalAccessAsync(1, TestContext.Current.CancellationToken);

        await _inner.Received(1).HasGlobalAccessAsync(1, Arg.Any<CancellationToken>());
    }

    // Role mutations → RbacAllPerms invalidation 
    [Fact]
    public async Task DeleteRoleAsync_ClearsAllPermCaches()
    {
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);

        await _sut.DeleteRoleAsync(99, TestContext.Current.CancellationToken);

        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);
        var result = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        result.Should().BeFalse("DeleteRole clears all perm caches");
        await _inner.Received(2).HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRoleAsync_ClearsAllPermCaches()
    {
        _inner.CreateRoleAsync(Arg.Any<CreateRoleRequest>(), Arg.Any<CancellationToken>()).Returns(AnyRole);
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);

        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        await _sut.CreateRoleAsync(new CreateRoleRequest("admin", null, null), TestContext.Current.CancellationToken);

        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);
        var result = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        result.Should().BeFalse("CreateRole clears all perm caches");
    }

    [Fact]
    public async Task UpdateRoleAsync_ClearsAllPermCaches()
    {
        _inner.UpdateRoleAsync(1, Arg.Any<UpdateRoleRequest>(), Arg.Any<CancellationToken>()).Returns(AnyRole);
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);

        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        await _sut.UpdateRoleAsync(1, new UpdateRoleRequest(null, null, null), TestContext.Current.CancellationToken);

        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);
        var result = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        result.Should().BeFalse("UpdateRole clears all perm caches");
    }

    [Fact]
    public async Task AssignPermissionAsync_ClearsAllPermCaches()
    {
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);

        await _sut.AssignPermissionAsync(1, new AssignPermissionRequest(99), null, TestContext.Current.CancellationToken);

        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);
        var result = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        result.Should().BeFalse("AssignPermission clears all perm caches");
    }

    [Fact]
    public async Task RemovePermissionAsync_ClearsAllPermCaches()
    {
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);

        await _sut.RemovePermissionAsync(1, 99, TestContext.Current.CancellationToken);

        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);
        var result = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        result.Should().BeFalse("RemovePermission clears all perm caches");
    }

    [Fact]
    public async Task SyncPermissionsAsync_ClearsAllPermCaches()
    {
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);

        await _sut.SyncPermissionsAsync(1, new SyncPermissionsRequest([]), null, TestContext.Current.CancellationToken);

        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);
        var result = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        result.Should().BeFalse("SyncPermissions clears all perm caches");
    }

    // User-level override mutations → per-user invalidation 
    [Fact]
    public async Task GrantUserPermissionAsync_ClearsOnlyThatUsersCache()
    {
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        _inner.HasPermissionAsync(2, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        await _sut.HasPermissionAsync(2, "m", "r", "a", TestContext.Current.CancellationToken);

        await _sut.GrantUserPermissionAsync(1, 99, AnyOverride, grantedBy: 0, TestContext.Current.CancellationToken);

        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);
        _inner.HasPermissionAsync(2, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);

        var user1Result = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        var user2Result = await _sut.HasPermissionAsync(2, "m", "r", "a", TestContext.Current.CancellationToken);

        user1Result.Should().BeFalse("user 1 cache was cleared");
        user2Result.Should().BeTrue("user 2 cache was NOT cleared");
    }

    [Fact]
    public async Task DenyUserPermissionAsync_ClearsOnlyThatUsersCache()
    {
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        _inner.HasPermissionAsync(2, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        await _sut.HasPermissionAsync(2, "m", "r", "a", TestContext.Current.CancellationToken);

        await _sut.DenyUserPermissionAsync(1, 99, AnyOverride, grantedBy: 0, TestContext.Current.CancellationToken);

        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);

        var user1Result = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        var user2Result = await _sut.HasPermissionAsync(2, "m", "r", "a", TestContext.Current.CancellationToken);

        user1Result.Should().BeFalse("user 1 cache was cleared");
        user2Result.Should().BeTrue("user 2 cache was NOT cleared");
    }

    [Fact]
    public async Task RemoveUserPermissionAsync_ClearsOnlyThatUsersCache()
    {
        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        _inner.HasPermissionAsync(2, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        await _sut.HasPermissionAsync(2, "m", "r", "a", TestContext.Current.CancellationToken);

        await _sut.RemoveUserPermissionAsync(1, 99, TestContext.Current.CancellationToken);

        _inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);

        var user1Result = await _sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        var user2Result = await _sut.HasPermissionAsync(2, "m", "r", "a", TestContext.Current.CancellationToken);

        user1Result.Should().BeFalse("user 1 cache was cleared");
        user2Result.Should().BeTrue("user 2 cache was NOT cleared");
    }
}
