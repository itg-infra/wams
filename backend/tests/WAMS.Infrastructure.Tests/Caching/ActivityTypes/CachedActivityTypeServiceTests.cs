namespace WAMS.Infrastructure.Tests.Caching;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.ActivityTypes;
using WAMS.Application.Interfaces.ActivityTypes;
using WAMS.Infrastructure.Caching.ActivityTypes;
using Xunit;

public sealed class CachedActivityTypeServiceTests : IDisposable
{
    private readonly CacheTestFixture _fx = new();
    private readonly IActivityTypeService _inner = Substitute.For<IActivityTypeService>();
    private readonly CachedActivityTypeService _sut;

    private static readonly ActivityTypeResponse At1 = new(1, "RECV", "Receiving", true);
    private static readonly ActivityTypeResponse At1Updated = new(1, "RECV", "Receiving Updated", true);

    public CachedActivityTypeServiceTests()
    {
        _sut = new CachedActivityTypeService(_inner, _fx.Cache, _fx.Options);
    }

    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task GetAllAsync_CachesResult_InnerCalledOnce()
    {
        _inner.GetAllAsync(Arg.Any<CancellationToken>()).Returns([At1]);

        await _sut.GetAllAsync(TestContext.Current.CancellationToken);
        await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        await _inner.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_CachesResult_InnerCalledOnce()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(At1);

        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _inner.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(At1);
        _inner.CreateAsync(Arg.Any<CreateActivityTypeRequest>(), Arg.Any<CancellationToken>()).Returns(At1);
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.CreateAsync(new CreateActivityTypeRequest("SHIP", "Shipping"), TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(At1Updated);
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Name.Should().Be("Receiving Updated", "cache cleared after Create");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(At1);
        _inner.UpdateAsync(1, Arg.Any<UpdateActivityTypeRequest>(), Arg.Any<CancellationToken>()).Returns(At1Updated);
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.UpdateAsync(1, new UpdateActivityTypeRequest(null, "Receiving Updated", null), TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(At1Updated);
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Name.Should().Be("Receiving Updated", "cache cleared after Update");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(At1);
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(At1Updated);
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Name.Should().Be("Receiving Updated", "cache cleared after Delete");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }
}
