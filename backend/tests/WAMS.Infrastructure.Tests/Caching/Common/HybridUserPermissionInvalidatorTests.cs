namespace WAMS.Infrastructure.Tests.Caching;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Infrastructure.Caching.Common;
using WAMS.Infrastructure.Caching.Rbac;
using Xunit;

public sealed class HybridUserPermissionInvalidatorTests : IDisposable
{
    private readonly CacheTestFixture _fx = new();

    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task InvalidateAsync_ClearsCachedEntriesForUser()
    {
        var inner = Substitute.For<IRbacService>();
        inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        var sut = new CachedRbacService(inner, _fx.Cache, _fx.Options);
        var invalidator = new HybridUserPermissionInvalidator(_fx.Cache);

        // Populate cache
        var firstCall = await sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        firstCall.Should().BeTrue();

        // Inner now returns false - without invalidation, cache would serve stale true
        inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);
        var cachedCall = await sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        cachedCall.Should().BeTrue("cache not yet cleared");

        // Act
        await invalidator.InvalidateAsync(userId: 1, TestContext.Current.CancellationToken);

        // Assert - next read hits inner and gets updated value
        var afterInvalidation = await sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        afterInvalidation.Should().BeFalse("cache was cleared by invalidator");
    }

    [Fact]
    public async Task InvalidateAsync_DoesNotClearOtherUsersEntries()
    {
        var inner = Substitute.For<IRbacService>();
        inner.HasPermissionAsync(1, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        inner.HasPermissionAsync(2, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(true);
        var sut = new CachedRbacService(inner, _fx.Cache, _fx.Options);
        var invalidator = new HybridUserPermissionInvalidator(_fx.Cache);

        await sut.HasPermissionAsync(1, "m", "r", "a", TestContext.Current.CancellationToken);
        await sut.HasPermissionAsync(2, "m", "r", "a", TestContext.Current.CancellationToken);

        // Act - invalidate only user 1
        await invalidator.InvalidateAsync(userId: 1, TestContext.Current.CancellationToken);

        inner.HasPermissionAsync(2, "m", "r", "a", Arg.Any<CancellationToken>()).Returns(false);

        // User 2's cache is unaffected
        var user2Result = await sut.HasPermissionAsync(2, "m", "r", "a", TestContext.Current.CancellationToken);
        user2Result.Should().BeTrue("user 2 cache was not invalidated");
    }
}
