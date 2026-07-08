using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Tests.Notifications;

public partial class NotificationsFeatureTests {

    [Fact]
    public void NotificationMappings_ToModel_MapsDomainNotification() {
        var notification = Notification.Create(
            UserId.New(),
            NotificationTypes.FastingCompleted,
            "{}",
            referenceId: "fasting-123");
        notification.MarkAsRead();
        var text = new NotificationText("Fast completed", "Nice work");

        NotificationModel model = notification.ToModel(text);

        Assert.Equal(notification.Id.Value, model.Id);
        Assert.Equal(NotificationTypes.FastingCompleted, model.Type);
        Assert.Equal("Fast completed", model.Title);
        Assert.Equal("Nice work", model.Body);
        Assert.Equal(notification.ReferenceId, model.ReferenceId);
        Assert.True(model.IsRead);
        Assert.Equal(notification.CreatedOnUtc, model.CreatedAtUtc);
    }

    [Fact]
    public void NotificationMappings_ToModel_MapsDomainWebPushSubscription() {
        var subscription = WebPushSubscription.Create(
            UserId.New(),
            "https://updates.push.example.com/subscriptions/123",
            "p256",
            "auth",
            expirationTimeUtc: new DateTime(2026, 5, 29, 8, 0, 0, DateTimeKind.Utc),
            locale: "ru",
            userAgent: "Firefox");

        WebPushSubscriptionModel model = subscription.ToModel();

        Assert.Equal(subscription.Endpoint, model.Endpoint);
        Assert.Equal("updates.push.example.com", model.EndpointHost);
        Assert.Equal(subscription.ExpirationTimeUtc, model.ExpirationTimeUtc);
        Assert.Equal("ru", model.Locale);
        Assert.Equal("Firefox", model.UserAgent);
        Assert.Equal(subscription.CreatedOnUtc, model.CreatedAtUtc);
        Assert.Equal(subscription.ModifiedOnUtc, model.UpdatedAtUtc);
    }

    [Fact]
    public async Task NotificationCleanup_WithNonPositiveBatchSize_DoesNotCallRepository() {
        var repository = new InMemoryNotificationRepository();
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var service = new NotificationCleanupService(
            repository,
            new FixedDateTimeProvider(new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc)),
            unitOfWork);

        int deleted = await service.CleanupExpiredNotificationsAsync(
            new NotificationCleanupPolicy(["Fast"], 3, 4, 30, 60, 0),
            CancellationToken.None);

        Assert.Equal(0, deleted);
        Assert.False(repository.DeleteExpiredBatchCalled);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotificationCleanup_UsesUtcNowRetentionCutoffsAndCancellationToken() {
        using var cts = new CancellationTokenSource();
        var repository = new InMemoryNotificationRepository { DeleteExpiredBatchResult = 7 };
        var utcNow = new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var service = new NotificationCleanupService(repository, new FixedDateTimeProvider(utcNow), unitOfWork);

        int deleted = await service.CleanupExpiredNotificationsAsync(
            new NotificationCleanupPolicy(["Fast"], 3, 4, 30, 60, 25),
            cts.Token);

        Assert.Equal(7, deleted);
        Assert.True(repository.DeleteExpiredBatchCalled);
        Assert.Equal(["Fast"], repository.TransientTypes);
        Assert.Equal(utcNow.AddDays(-3), repository.TransientReadOlderThanUtc);
        Assert.Equal(utcNow.AddDays(-4), repository.TransientUnreadOlderThanUtc);
        Assert.Equal(utcNow.AddDays(-30), repository.StandardReadOlderThanUtc);
        Assert.Equal(utcNow.AddDays(-60), repository.StandardUnreadOlderThanUtc);
        Assert.Equal(25, repository.BatchSize);
        Assert.Equal(cts.Token, repository.DeleteExpiredBatchCancellationToken);
        await unitOfWork.Received(1).SaveChangesAsync(cts.Token);
    }

    [Fact]
    public async Task NotificationWriter_WhenWebPushRequested_EnqueuesOutboxMessage() {
        var repository = new InMemoryNotificationRepository();
        var outbox = new RecordingNotificationWebPushOutbox();
        var writer = new NotificationWriter(repository, outbox);
        var notification = Notification.Create(UserId.New(), "info", "{}");

        await writer.AddAsync(notification, sendWebPush: true, CancellationToken.None);

        Assert.Same(notification, Assert.Single(repository.Notifications));
        Assert.Equal(notification.Id, Assert.Single(outbox.NotificationIds));
    }
}
