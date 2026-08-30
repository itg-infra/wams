namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.SyncLogs;
using WAMS.Application.Interfaces.SyncLogs;
using WAMS.Application.Services.SyncLogs;
using WAMS.Domain.Entities.SyncLogs;
using Xunit;

public class SyncLogServiceTests
{
    private readonly ISyncLogRepository _repo = Substitute.For<ISyncLogRepository>();
    private readonly SyncLogService _sut;

    public SyncLogServiceTests()
    {
        _sut = new SyncLogService(_repo);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPaginatedResponse()
    {
        var query = new SyncLogQuery { Page = 1, Limit = 20 };
        var started = new DateTime(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc);
        var finished = started.AddSeconds(3.5);
        var log = new SyncLog
        {
            Id = 1,
            ServiceName = "WarehouseSync",
            CompanyCode = "SAGARA",
            StartedAt = started,
            FinishedAt = finished,
            Outcome = SyncOutcome.Success,
            Added = 5,
            Updated = 2,
            Deactivated = 1,
            AbortReason = null,
        };
        _repo.GetPagedAsync(query, Arg.Any<CancellationToken>())
            .Returns((new List<SyncLog> { log }, 1));

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Meta.Should().BeEquivalentTo(new PaginationMeta(1, 20, 1, 1));
        result.Data.Should().ContainSingle();

        var item = result.Data[0];
        item.Id.Should().Be(1);
        item.ServiceName.Should().Be("WarehouseSync");
        item.CompanyCode.Should().Be("SAGARA");
        item.StartedAt.Should().Be(started);
        item.FinishedAt.Should().Be(finished);
        item.Outcome.Should().Be("Success");
        item.Added.Should().Be(5);
        item.Updated.Should().Be(2);
        item.Deactivated.Should().Be(1);
        item.AbortReason.Should().BeNull();
        item.DurationMs.Should().BeApproximately(3500, 1);
    }

    [Fact]
    public async Task GetPagedAsync_WhenFinishedAtIsNull_DurationMsIsNull()
    {
        var query = new SyncLogQuery { Page = 1, Limit = 20 };
        var log = new SyncLog
        {
            Id = 2,
            ServiceName = "VendorSync",
            CompanyCode = "SAGARA",
            StartedAt = DateTime.UtcNow,
            FinishedAt = null,
            Outcome = SyncOutcome.Exception,
        };
        _repo.GetPagedAsync(query, Arg.Any<CancellationToken>())
            .Returns((new List<SyncLog> { log }, 1));

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        result.Data[0].DurationMs.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_PaginationMeta_ComputesTotalPagesCorrectly()
    {
        var query = new SyncLogQuery { Page = 2, Limit = 10 };
        _repo.GetPagedAsync(query, Arg.Any<CancellationToken>())
            .Returns((new List<SyncLog>(), 25));

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        result.Meta.Should().BeEquivalentTo(new PaginationMeta(2, 10, 25, 3));
    }

    [Fact]
    public async Task GetPagedAsync_EmptyResult_ReturnsEmptyDataWithZeroTotal()
    {
        var query = new SyncLogQuery { Page = 1, Limit = 20 };
        _repo.GetPagedAsync(query, Arg.Any<CancellationToken>())
            .Returns((new List<SyncLog>(), 0));

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
        result.Meta.Total.Should().Be(0);
        result.Meta.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetLatestPerServiceAsync_ReturnsMappedLatestResponse()
    {
        var started = new DateTime(2026, 6, 21, 8, 0, 0, DateTimeKind.Utc);
        var finished = started.AddMilliseconds(1200);
        var log = new SyncLog
        {
            Id = 10,
            ServiceName = "ItemSync",
            CompanyCode = "SAGARA",
            StartedAt = started,
            FinishedAt = finished,
            Outcome = SyncOutcome.ErpUnavailable,
            Added = 0,
            Updated = 0,
            Deactivated = 0,
            AbortReason = "ERP returned null or empty response",
        };
        _repo.GetLatestPerServiceAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SyncLog> { log });

        var result = await _sut.GetLatestPerServiceAsync(CancellationToken.None);

        result.Should().ContainSingle();
        var item = result[0];
        item.ServiceName.Should().Be("ItemSync");
        item.CompanyCode.Should().Be("SAGARA");
        item.Outcome.Should().Be("ErpUnavailable");
        item.AbortReason.Should().Be("ERP returned null or empty response");
        item.DurationMs.Should().BeApproximately(1200, 1);
    }

    [Fact]
    public async Task GetLatestPerServiceAsync_EmptyRepo_ReturnsEmptyList()
    {
        _repo.GetLatestPerServiceAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SyncLog>());

        var result = await _sut.GetLatestPerServiceAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }
}
