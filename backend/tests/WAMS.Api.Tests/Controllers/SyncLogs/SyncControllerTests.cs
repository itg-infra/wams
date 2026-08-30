namespace WAMS.Api.Tests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WAMS.Api.Controllers.SyncLogs;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.SyncLogs;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.SyncLogs;
using WAMS.Infrastructure.ExternalSync.Common;
using Xunit;

public class SyncControllerTests
{
    private readonly ISyncLogService _syncLogService = Substitute.For<ISyncLogService>();
    private readonly ICacheInvalidationService _cacheInvalidationService = Substitute.For<ICacheInvalidationService>();
    private readonly IEnumerable<IExternalSyncService> _syncServices = [];
    private readonly SyncController _sut;

    public SyncControllerTests()
    {
        _sut = new SyncController(_syncServices, _syncLogService, _cacheInvalidationService, NullLogger<SyncController>.Instance);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _sut.ControllerContext.HttpContext.Items["RequestId"] = "req-test";
    }

    [Fact]
    public async Task GetLogs_ReturnsOkWithPaginatedResponse()
    {
        var query = new SyncLogQuery { Page = 1, Limit = 20, ServiceName = "WarehouseSync" };
        var response = new PaginatedResponse<SyncLogResponse>(
            true,
            [BuildSyncLogResponse()],
            new PaginationMeta(1, 20, 1, 1));
        _syncLogService.GetPagedAsync(query, Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.GetLogs(query, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<PaginatedResponse<SyncLogResponse>>().Subject;
        payload.Success.Should().BeTrue();
        payload.Meta.Should().BeEquivalentTo(new PaginationMeta(1, 20, 1, 1));
        payload.RequestId.Should().Be("req-test");
        payload.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task GetLogs_EmptyResult_ReturnsOkWithEmptyData()
    {
        var query = new SyncLogQuery { Page = 1, Limit = 20 };
        var response = new PaginatedResponse<SyncLogResponse>(
            true,
            [],
            new PaginationMeta(1, 20, 0, 0));
        _syncLogService.GetPagedAsync(query, Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.GetLogs(query, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<PaginatedResponse<SyncLogResponse>>().Subject;
        payload.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatest_ReturnsOkWithListOfLatestResponses()
    {
        var latest = new List<SyncLogLatestResponse>
        {
            new("WarehouseSync", "SAGARA",
                new DateTime(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 21, 10, 0, 2, DateTimeKind.Utc),
                "Success", 3, 1, 0, null, 2000),
        };
        _syncLogService.GetLatestPerServiceAsync(Arg.Any<CancellationToken>())
            .Returns(latest);

        var result = await _sut.GetLatest(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<List<SyncLogLatestResponse>>>().Subject;
        payload.Success.Should().BeTrue();
        payload.Data.Should().ContainSingle();
        payload.Data![0].ServiceName.Should().Be("WarehouseSync");
        payload.Data![0].Outcome.Should().Be("Success");
    }

    [Fact]
    public async Task GetLatest_EmptyRepo_ReturnsOkWithEmptyList()
    {
        _syncLogService.GetLatestPerServiceAsync(Arg.Any<CancellationToken>())
            .Returns(new List<SyncLogLatestResponse>());

        var result = await _sut.GetLatest(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<List<SyncLogLatestResponse>>>().Subject;
        payload.Data.Should().BeEmpty();
    }

    private static SyncLogResponse BuildSyncLogResponse() => new(
        Id: 1,
        ServiceName: "WarehouseSync",
        CompanyCode: "SAGARA",
        StartedAt: new DateTime(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc),
        FinishedAt: new DateTime(2026, 6, 21, 10, 0, 1, DateTimeKind.Utc),
        Outcome: "Success",
        Added: 2,
        Updated: 0,
        Deactivated: 0,
        AbortReason: null,
        DurationMs: 1000);
}
