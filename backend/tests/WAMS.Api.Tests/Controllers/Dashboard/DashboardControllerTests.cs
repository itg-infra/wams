namespace WAMS.Api.Tests.Controllers;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WAMS.Api.Controllers.Dashboard;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Dashboard;
using WAMS.Application.Interfaces.Dashboard;
using Xunit;

public class DashboardControllerTests
{
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();
    private readonly DashboardController _sut;

    public DashboardControllerTests()
    {
        _sut = new DashboardController(_dashboardService);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _sut.ControllerContext.HttpContext.Items["RequestId"] = "req-test";
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "1"),
                new Claim(ClaimTypes.Role, "WAREHOUSE_ADMIN"),
            ], "jwt"));
    }

    // --- GetSummary ---

    [Fact]
    public async Task GetSummary_ReturnsOkWithSummaryResponse()
    {
        var summary = new DashboardSummaryResponse(88m, 1_000m, 880m, 14, 3, 42, 6, 8, 2);
        _dashboardService.GetSummaryAsync(1, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(summary);

        var result = await _sut.GetSummary(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<DashboardSummaryResponse>>().Subject;
        payload.Success.Should().BeTrue();
        payload.RequestId.Should().Be("req-test");
        payload.Data!.BudgetAchievedPercent.Should().Be(88m);
        payload.Data.OpenWorkOrderCount.Should().Be(42);
    }

    [Fact]
    public async Task GetSummary_PassesRoleNamesFromJwt()
    {
        _dashboardService.GetSummaryAsync(Arg.Any<long>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new DashboardSummaryResponse(0, 0, 0, 0, 0, 0, 0, 0, 0));

        await _sut.GetSummary(CancellationToken.None);

        await _dashboardService.Received(1).GetSummaryAsync(
            1,
            Arg.Is<IReadOnlyList<string>>(roles => roles.Contains("WAREHOUSE_ADMIN")),
            Arg.Any<CancellationToken>());
    }

    // --- GetActivities ---

    [Fact]
    public async Task GetActivities_ReturnsOkWithPaginatedActivities()
    {
        var query = new DashboardActivityQuery { Page = 1, Limit = 10 };
        var activity = new DashboardActivityResponse(1, "2603000001", "PT. XYZ, PT. ABC", "Bongkaran", true, "Lampung", DateTime.UtcNow, "Approved", "Approved");
        _dashboardService.GetTodayActivitiesAsync(query, 1, Arg.Any<CancellationToken>())
            .Returns(([activity], 1));

        var result = await _sut.GetActivities(query, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<PaginatedResponse<DashboardActivityResponse>>().Subject;
        payload.Success.Should().BeTrue();
        payload.RequestId.Should().Be("req-test");
        payload.Data.Should().ContainSingle();
        payload.Meta.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetActivities_EmptyResult_ReturnsPaginatedResponseWithZeroTotal()
    {
        var query = new DashboardActivityQuery { Page = 1, Limit = 20 };
        _dashboardService.GetTodayActivitiesAsync(query, 1, Arg.Any<CancellationToken>())
            .Returns(([], 0));

        var result = await _sut.GetActivities(query, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<PaginatedResponse<DashboardActivityResponse>>().Subject;
        payload.Data.Should().BeEmpty();
        payload.Meta.Total.Should().Be(0);
        payload.Meta.TotalPages.Should().Be(0);
    }

    // --- GetHistory ---

    [Fact]
    public async Task GetHistory_ReturnsOkWithHistoryResponse()
    {
        var history = new DashboardHistoryResponse(
            [new DashboardCalendarDay(new DateOnly(2026, 6, 1), 3)],
            [new DashboardEventEntry(DateTime.UtcNow, "Submitted", "K.Bongkar", "WHLPG01")]);
        _dashboardService.GetHistoryAsync(2026, 6, 1, Arg.Any<CancellationToken>())
            .Returns(history);

        var result = await _sut.GetHistory(2026, 6, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<DashboardHistoryResponse>>().Subject;
        payload.Success.Should().BeTrue();
        payload.Data!.CalendarDays.Should().ContainSingle();
        payload.Data.RecentEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task GetHistory_ForwardsYearAndMonthToService()
    {
        _dashboardService.GetHistoryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new DashboardHistoryResponse([], []));

        await _sut.GetHistory(2025, 12, CancellationToken.None);

        await _dashboardService.Received(1).GetHistoryAsync(2025, 12, 1, Arg.Any<CancellationToken>());
    }
}
