namespace WAMS.Infrastructure.Tests.Caching;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Items;
using WAMS.Application.DTOs.RateCards;
using WAMS.Application.DTOs.Uoms;
using WAMS.Application.DTOs.Vendors;
using WAMS.Application.Interfaces.RateCards;
using WAMS.Infrastructure.Caching.RateCards;
using Xunit;

public sealed class CachedRateCardServiceTests : IDisposable
{
    private readonly CacheTestFixture _fx = new();
    private readonly IRateCardService _inner = Substitute.For<IRateCardService>();
    private readonly CachedRateCardService _sut;

    private static readonly VendorSummaryResponse Vendor = new(1, "V001", "Acme");
    private static readonly ItemSummaryResponse Item = new(1, "I001", "Widget", "ACC", "Account");
    private static readonly UomResponse Uom = new(1, "PCS", "Pieces", true);

    private static RateCardResponse MakeCard(string status)
        => new(1, Vendor, status, [new RateCardItemResponse(1, Item, Uom, 10m, null, null, null)], DateTime.UtcNow, null);

    public CachedRateCardServiceTests()
    {
        _sut = new CachedRateCardService(_inner, _fx.Cache, _fx.Options);
    }

    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task GetByIdAsync_CachesResult_InnerCalledOnce()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeCard("Draft"));

        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _inner.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeCard("Draft"));
        _inner.UpdateAsync(1, Arg.Any<UpdateRateCardRequest>(), Arg.Any<CancellationToken>()).Returns(MakeCard("Draft"));
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.UpdateAsync(1, new UpdateRateCardRequest(1, []), TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeCard("Submitted"));
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Status.Should().Be("Submitted", "cache cleared after Update");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeCard("Draft"));
        _inner.SubmitAsync(1, Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(MakeCard("Submitted"));
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.SubmitAsync(1, userId: 99, TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeCard("Submitted"));
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Status.Should().Be("Submitted", "cache cleared after Submit");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeCard("Draft"));
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeCard("Submitted"));
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Status.Should().Be("Submitted", "cache cleared after Delete");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeCard("Draft"));
        _inner.CreateAsync(Arg.Any<long>(), Arg.Any<CreateRateCardRequest>(), Arg.Any<CancellationToken>()).Returns(MakeCard("Draft"));
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.CreateAsync(userId: 1, new CreateRateCardRequest(1, []), TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeCard("Submitted"));
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Status.Should().Be("Submitted", "cache cleared after Create");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }
}
