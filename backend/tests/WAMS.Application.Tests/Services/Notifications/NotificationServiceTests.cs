namespace WAMS.Application.Tests.Services;

using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.DTOs.Notifications;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Application.Services.Notifications;
using WAMS.Domain.Entities.Notifications;
using WAMS.Domain.Exceptions;
using Xunit;

public class NotificationServiceTests
{
    private readonly INotificationRepository _notificationRepo = Substitute.For<INotificationRepository>();
    private readonly INotificationRealtimeDispatcher _dispatcher = Substitute.For<INotificationRealtimeDispatcher>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _sut = new NotificationService(
            _notificationRepo,
            _dispatcher,
            _uow,
            NullLogger<NotificationService>.Instance);
    }

    [Fact]
    public async Task PublishAsync_PersistsAndDispatchesNotifications()
    {
        var requests = new[]
        {
            new NotificationCreateRequest(1, 10, 99, "approval_completed", "Approved", "Done", "budget_plan", "123")
        };

        await _sut.PublishAsync(requests, TestContext.Current.CancellationToken);

        await _notificationRepo.Received(1).CreateRangeAsync(Arg.Any<IEnumerable<Notification>>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).PublishAsync(Arg.Is<NotificationResponse>(n =>
            n.Type == "approval_completed" &&
            n.ReferenceType == "budget_plan" &&
            n.ReferenceId == "123" &&
            n.RecipientUserId == 10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithEmptyNotifications_DoesNothing()
    {
        await _sut.PublishAsync([], TestContext.Current.CancellationToken);

        await _notificationRepo.DidNotReceive().CreateRangeAsync(Arg.Any<IEnumerable<Notification>>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _dispatcher.DidNotReceive().PublishAsync(Arg.Any<NotificationResponse>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_DeduplicatesEquivalentNotifications()
    {
        var requests = new[]
        {
            new NotificationCreateRequest(1, 10, 99, "approval_completed", "Approved", "Done", "budget_plan", "123"),
            new NotificationCreateRequest(1, 10, 99, "approval_completed", "Approved", "Done", "budget_plan", "123")
        };

        await _sut.PublishAsync(requests, TestContext.Current.CancellationToken);

        await _notificationRepo.Received(1).CreateRangeAsync(
            Arg.Is<IEnumerable<Notification>>(items => items.Count() == 1),
            Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).PublishAsync(Arg.Any<NotificationResponse>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WhenRealtimeDispatchFails_DoesNotThrow()
    {
        _dispatcher.PublishAsync(Arg.Any<NotificationResponse>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("dispatcher failed"));

        var act = () => _sut.PublishAsync([
            new NotificationCreateRequest(1, 10, 99, "approval_completed", "Approved", "Done", "budget_plan", "123")
        ], TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _notificationRepo.Received(1).CreateRangeAsync(Arg.Any<IEnumerable<Notification>>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyNotificationsAsync_MapsRepositoryResults()
    {
        var notifications = new List<Notification>
        {
            new()
            {
                Id = 1,
                CompanyId = 1,
                RecipientUserId = 10,
                ActorUserId = 99,
                Type = "budget_plan_rejected",
                Title = "Rejected",
                Message = "Plan rejected",
                ReferenceType = "budget_plan",
                ReferenceId = "42",
                IsRead = false,
                CreatedAt = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc)
            }
        };
        _notificationRepo.GetByRecipientAsync(10, Arg.Any<NotificationQuery>(), Arg.Any<CancellationToken>())
            .Returns((notifications, 1));

        var (items, total) = await _sut.GetMyNotificationsAsync(10, new NotificationQuery(), TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].Type.Should().Be("budget_plan_rejected");
        items[0].Status.Should().Be("unread");
        items[0].ReferenceId.Should().Be("42");
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationMissing_ThrowsNotFound()
    {
        _notificationRepo.GetByIdAsync(77, 10, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.MarkAsReadAsync(77, 10, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationExists_UpdatesState()
    {
        var notification = new Notification
        {
            Id = 5,
            CompanyId = 1,
            RecipientUserId = 10,
            Type = "approval_completed",
            Title = "Approved",
            Message = "Done",
            ReferenceType = "budget_plan",
            ReferenceId = "123"
        };

        _notificationRepo.GetByIdAsync(5, 10, Arg.Any<CancellationToken>()).Returns(notification);

        await _sut.MarkAsReadAsync(5, 10, TestContext.Current.CancellationToken);

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenAlreadyRead_DoesNotCommitAgain()
    {
        var notification = new Notification
        {
            Id = 5,
            CompanyId = 1,
            RecipientUserId = 10,
            Type = "approval_completed",
            Title = "Approved",
            Message = "Done",
            ReferenceType = "budget_plan",
            ReferenceId = "123",
            IsRead = true,
            ReadAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _notificationRepo.GetByIdAsync(5, 10, Arg.Any<CancellationToken>()).Returns(notification);

        await _sut.MarkAsReadAsync(5, 10, TestContext.Current.CancellationToken);

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ForwardsToRepositoryAndReturnsCount()
    {
        _notificationRepo.MarkAllAsReadAsync(10, Arg.Any<CancellationToken>()).Returns(3);

        var result = await _sut.MarkAllAsReadAsync(10, TestContext.Current.CancellationToken);

        result.Should().Be(3);
        await _notificationRepo.Received(1).MarkAllAsReadAsync(10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenNoneUnread_ReturnsZero()
    {
        _notificationRepo.MarkAllAsReadAsync(10, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _sut.MarkAllAsReadAsync(10, TestContext.Current.CancellationToken);

        result.Should().Be(0);
    }

    [Theory]
    [InlineData("budget_plan", "42", "/budgeting/plan/42")]
    [InlineData("budget_plan_batch", "stage_1", "/budgeting/plan?status=InApproval")]
    [InlineData("budget_plan_batch", "stage_2", "/budgeting/plan?status=InApproval")]
    [InlineData("unknown_type", "1", null)]
    public async Task GetMyNotificationsAsync_ResolvesRoute(string referenceType, string referenceId, string? expectedRoute)
    {
        var notifications = new List<Notification>
        {
            new()
            {
                Id = 1,
                CompanyId = 1,
                RecipientUserId = 10,
                Type = "budget_plan_rejected",
                Title = "Rejected",
                Message = "Plan rejected",
                ReferenceType = referenceType,
                ReferenceId = referenceId
            }
        };
        _notificationRepo.GetByRecipientAsync(10, Arg.Any<NotificationQuery>(), Arg.Any<CancellationToken>())
            .Returns((notifications, 1));

        var (items, _) = await _sut.GetMyNotificationsAsync(10, new NotificationQuery(), TestContext.Current.CancellationToken);

        items[0].Route.Should().Be(expectedRoute);
    }

    [Fact]
    public void Subscribe_ForwardsDispatcherSubscription()
    {
        var channel = Channel.CreateUnbounded<NotificationResponse>();
        _dispatcher.Subscribe(12, Arg.Any<CancellationToken>()).Returns(channel.Reader);

        var reader = _sut.Subscribe(12, TestContext.Current.CancellationToken);

        reader.Should().BeSameAs(channel.Reader);
    }
}
