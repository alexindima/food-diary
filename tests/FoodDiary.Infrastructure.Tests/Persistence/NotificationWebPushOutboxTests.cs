using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class NotificationWebPushOutboxTests {
    [Fact]
    public async Task EnqueueAsync_PersistsDueMessage() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create("notification-outbox-enqueue@example.com", "hash");
        var notification = Notification.Create(user.Id, "info", "{}");
        context.Users.Add(user);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        var outbox = new NotificationWebPushOutbox(context, TimeProvider.System);

        await outbox.EnqueueAsync(notification.Id, CancellationToken.None);
        await context.SaveChangesAsync();

        NotificationWebPushOutboxMessage message = Assert.Single(context.NotificationWebPushOutbox);
        Assert.Equal(notification.Id, message.NotificationId);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(0, message.AttemptCount);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenSendSucceeds_MarksMessageProcessed() {
        await using FoodDiaryDbContext context = CreateContext();
        Notification notification = await SeedNotificationAsync(context, "notification-outbox-success@example.com");
        context.NotificationWebPushOutbox.Add(NotificationWebPushOutboxMessage.Create(notification.Id, DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var sender = new RecordingWebPushNotificationSender();
        var processor = new NotificationWebPushOutboxProcessor(
            context,
            sender,
            TimeProvider.System,
            NullLogger<NotificationWebPushOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal([notification.Id], sender.NotificationIds);
        NotificationWebPushOutboxMessage message = Assert.Single(context.NotificationWebPushOutbox);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Null(message.LastError);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenSendFails_SchedulesRetry() {
        await using FoodDiaryDbContext context = CreateContext();
        Notification notification = await SeedNotificationAsync(context, "notification-outbox-retry@example.com");
        context.NotificationWebPushOutbox.Add(NotificationWebPushOutboxMessage.Create(notification.Id, DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var processor = new NotificationWebPushOutboxProcessor(
            context,
            new ThrowingWebPushNotificationSender(),
            TimeProvider.System,
            NullLogger<NotificationWebPushOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        Assert.Equal(0, processed);
        NotificationWebPushOutboxMessage message = Assert.Single(context.NotificationWebPushOutbox);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.AttemptCount);
        Assert.True(message.NextAttemptOnUtc > DateTime.UtcNow);
        Assert.Contains("Simulated", message.LastError, StringComparison.Ordinal);
    }

    private static async Task<Notification> SeedNotificationAsync(FoodDiaryDbContext context, string email) {
        var user = User.Create(email, "hash");
        var notification = Notification.Create(user.Id, "info", "{}");
        context.Users.Add(user);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return notification;
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingWebPushNotificationSender : IWebPushNotificationSender {
        public List<FoodDiary.Domain.ValueObjects.Ids.NotificationId> NotificationIds { get; } = [];

        public Task SendAsync(Notification notification, CancellationToken cancellationToken = default) {
            NotificationIds.Add(notification.Id);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingWebPushNotificationSender : IWebPushNotificationSender {
        public Task SendAsync(Notification notification, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Simulated web-push failure."));
    }
}
