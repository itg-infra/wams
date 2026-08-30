namespace WAMS.Api.Tests.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using WAMS.Api.Controllers.Notifications;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Notifications;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Application.Validators.Notifications;
using WAMS.Domain.Exceptions;
using Xunit;

public class NotificationsControllerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly NotificationsController _sut;

    public NotificationsControllerTests()
    {
        _sut = new NotificationsController(
            _notificationService,
            new SendTestNotificationRequestValidator(),
            Options.Create(new NotificationOptions { HeartbeatIntervalSeconds = 15 }));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _sut.ControllerContext.HttpContext.Items["RequestId"] = "req-test";
        _sut.ControllerContext.HttpContext.User = BuildUser(userId: 7, companyId: 3);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedResponse()
    {
        var query = new NotificationQuery { Page = 0, Limit = 0, UnreadOnly = true };
        var items = new List<NotificationResponse>
        {
            new(1, "test_notification", "Realtime Test", "Hello", "test", "manual-1", "unread", DateTime.UtcNow, null, 7, 7, null)
        };
        _notificationService.GetMyNotificationsAsync(7, query, Arg.Any<CancellationToken>())
            .Returns((items, 1));

        var result = await _sut.GetAll(query, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<PaginatedResponse<NotificationResponse>>().Subject;
        payload.Success.Should().BeTrue();
        payload.RequestId.Should().Be("req-test");
        payload.Meta.Should().BeEquivalentTo(new PaginationMeta(1, 20, 1, 1));
        payload.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task MarkAsRead_ReturnsNoContentAndCallsService()
    {
        var result = await _sut.MarkAsRead(15, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await _notificationService.Received(1).MarkAsReadAsync(15, 7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsOkWithUpdatedCount()
    {
        _notificationService.MarkAllAsReadAsync(7, Arg.Any<CancellationToken>()).Returns(4);

        var result = await _sut.MarkAllAsRead(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { updatedCount = 4 });
        await _notificationService.Received(1).MarkAllAsReadAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsRead_WhenNoneUnread_ReturnsOkWithZero()
    {
        _notificationService.MarkAllAsReadAsync(7, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _sut.MarkAllAsRead(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { updatedCount = 0 });
    }

    [Fact]
    public async Task SendTestNotification_WithValidRequest_ReturnsAcceptedAndPublishesToCurrentUser()
    {
        var request = new SendTestNotificationRequest(
            "test_notification",
            "Realtime Test",
            "This is a test",
            "test",
            "manual-1");

        var result = await _sut.SendTestNotification(request, CancellationToken.None);

        var accepted = result.Should().BeOfType<AcceptedResult>().Subject;
        accepted.Value.Should().BeOfType<ApiResponse<object>>();
        await _notificationService.Received(1).PublishAsync(
            Arg.Is<IEnumerable<NotificationCreateRequest>>(items =>
                items.Single().CompanyId == 3 &&
                items.Single().RecipientUserId == 7 &&
                items.Single().ActorUserId == 7 &&
                items.Single().Type == "test_notification"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendTestNotification_WithInvalidRequest_ThrowsValidationException()
    {
        var request = new SendTestNotificationRequest("", "", "", "", "");

        var act = () => _sut.SendTestNotification(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _notificationService.DidNotReceive().PublishAsync(Arg.Any<IEnumerable<NotificationCreateRequest>>(), Arg.Any<CancellationToken>());
    }

    private static ClaimsPrincipal BuildUser(long userId, long companyId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("company_id", companyId.ToString()),
        ], "jwt");

        return new ClaimsPrincipal(identity);
    }
}
