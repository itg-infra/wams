using FluentAssertions;
using WAMS.Application.DTOs.Notifications;
using WAMS.Infrastructure.Services.Notifications;
using Xunit;

namespace WAMS.Infrastructure.Tests.Services.Notifications;

public sealed class InMemoryNotificationRealtimeDispatcherTests
{
    [Fact]
    public async Task PublishAsync_DeliversToEverySubscriberForRecipient()
    {
        var sut = new InMemoryNotificationRealtimeDispatcher();
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        var firstReader = sut.Subscribe(10, firstCancellation.Token);
        var secondReader = sut.Subscribe(10, secondCancellation.Token);
        var notification = CreateNotification(10);

        await sut.PublishAsync(notification, TestContext.Current.CancellationToken);

        (await firstReader.WaitToReadAsync(TestContext.Current.CancellationToken)).Should().BeTrue();
        firstReader.TryRead(out var firstResult).Should().BeTrue();
        firstResult.Should().Be(notification);
        (await secondReader.WaitToReadAsync(TestContext.Current.CancellationToken)).Should().BeTrue();
        secondReader.TryRead(out var secondResult).Should().BeTrue();
        secondResult.Should().Be(notification);
    }

    [Fact]
    public async Task PublishAsync_DoesNotDeliverToAnotherUser()
    {
        var sut = new InMemoryNotificationRealtimeDispatcher();
        using var recipientCancellation = new CancellationTokenSource();
        using var anotherUserCancellation = new CancellationTokenSource();
        var recipientReader = sut.Subscribe(10, recipientCancellation.Token);
        var anotherUserReader = sut.Subscribe(20, anotherUserCancellation.Token);

        await sut.PublishAsync(CreateNotification(10), TestContext.Current.CancellationToken);

        (await recipientReader.WaitToReadAsync(TestContext.Current.CancellationToken)).Should().BeTrue();
        recipientReader.TryRead(out _).Should().BeTrue();
        anotherUserReader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task Subscribe_CancellationCompletesReaderAndRemovesSubscription()
    {
        var sut = new InMemoryNotificationRealtimeDispatcher();
        using var cancellation = new CancellationTokenSource();
        var reader = sut.Subscribe(10, cancellation.Token);

        cancellation.Cancel();

        (await reader.WaitToReadAsync(TestContext.Current.CancellationToken)).Should().BeFalse();
        await sut.PublishAsync(CreateNotification(10), TestContext.Current.CancellationToken);
        reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task PublishAsync_DropsOldestNotificationWhenSubscriberFallsBehind()
    {
        var sut = new InMemoryNotificationRealtimeDispatcher();
        using var cancellation = new CancellationTokenSource();
        var reader = sut.Subscribe(10, cancellation.Token);

        for (var id = 1; id <= 101; id++)
            await sut.PublishAsync(CreateNotification(10, id), TestContext.Current.CancellationToken);

        var results = new List<NotificationResponse>();
        while (reader.TryRead(out var notification))
            results.Add(notification);

        results.Should().HaveCount(100);
        results.Select(notification => notification.Id)
            .Should().Equal(Enumerable.Range(2, 100).Select(id => (long)id));
    }

    private static NotificationResponse CreateNotification(long recipientUserId, long id = 1) => new(
        id,
        "approval_completed",
        "Approved",
        "Done",
        "budget_plan",
        "123",
        "unread",
        DateTime.UtcNow,
        null,
        recipientUserId,
        99,
        "/budget-plans/123");
}
