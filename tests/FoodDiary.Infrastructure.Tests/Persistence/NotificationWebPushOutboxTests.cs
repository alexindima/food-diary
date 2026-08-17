using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Notifications;
using FoodDiary.Infrastructure.Options;
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
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
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
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            TimeProvider.System,
            NullLogger<NotificationWebPushOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        Assert.Equal(0, processed);
        NotificationWebPushOutboxMessage message = Assert.Single(context.NotificationWebPushOutbox);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.AttemptCount);
        Assert.True(message.NextAttemptOnUtc > DateTime.UtcNow);
        Assert.Equal("Outbox dispatch failed (InvalidOperationException).", message.LastError);
    }

    [Fact]
    public async Task ProcessDueAsync_WhenMaxAttemptReached_DeadLettersMessage() {
        await using FoodDiaryDbContext context = CreateContext();
        Notification notification = await SeedNotificationAsync(context, "notification-outbox-dead-letter@example.com");
        var message = NotificationWebPushOutboxMessage.Create(notification.Id, DateTime.UtcNow.AddMinutes(-1));
        for (int i = 0; i < 9; i++) {
            message.MarkFailed("previous failure", DateTime.UtcNow.AddMinutes(-1));
        }

        context.NotificationWebPushOutbox.Add(message);
        await context.SaveChangesAsync();
        var processor = new NotificationWebPushOutboxProcessor(
            context,
            new ThrowingWebPushNotificationSender(),
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            TimeProvider.System,
            NullLogger<NotificationWebPushOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10, CancellationToken.None);

        NotificationWebPushOutboxMessage stored = Assert.Single(context.NotificationWebPushOutbox);
        Assert.Multiple(
            () => Assert.Equal(0, processed),
            () => Assert.Equal(10, stored.AttemptCount),
            () => Assert.NotNull(stored.DeadLetteredOnUtc),
            () => Assert.Null(stored.LockedUntilUtc),
            () => Assert.Null(stored.LockedBy),
            () => Assert.Equal("Outbox dispatch failed (InvalidOperationException).", stored.LastError));
    }

    [Fact]
    public async Task ProcessDueAsync_WhenBatchSizeIsNotPositive_ReturnsZero() {
        await using FoodDiaryDbContext context = CreateContext();
        var processor = new NotificationWebPushOutboxProcessor(
            context,
            new RecordingWebPushNotificationSender(),
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            TimeProvider.System,
            NullLogger<NotificationWebPushOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 0, CancellationToken.None);

        Assert.Equal(0, processed);
    }

    [Fact]
    public void Create_WithEmptyNotificationId_Throws() {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            NotificationWebPushOutboxMessage.Create(NotificationId.Empty, DateTime.UtcNow));

        Assert.Equal("notificationId", ex.ParamName);
    }

    [Fact]
    public void Create_NormalizesLocalDate() {
        var localDate = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Local);
        var notificationId = NotificationId.New();

        var message = NotificationWebPushOutboxMessage.Create(notificationId, localDate);

        Assert.Multiple(
            () => Assert.Equal(notificationId, message.NotificationId),
            () => Assert.Equal(DateTimeKind.Utc, message.CreatedOnUtc.Kind),
            () => Assert.Equal(message.CreatedOnUtc, message.NextAttemptOnUtc));
    }

    [Fact]
    public void MarkFailed_WithBlankError_ClearsLastError() {
        var message = NotificationWebPushOutboxMessage.Create(NotificationId.New(), DateTime.UtcNow);

        message.MarkFailed(" ", DateTime.UtcNow.AddMinutes(1));

        Assert.Multiple(
            () => Assert.Equal(1, message.AttemptCount),
            () => Assert.Null(message.LastError));
    }

    [Fact]
    public void MarkDeadLettered_ClearsLockAndStoresTrimmedError() {
        var message = NotificationWebPushOutboxMessage.Create(NotificationId.New(), DateTime.UtcNow);
        message.MarkClaimed(DateTime.UtcNow.AddMinutes(5), "worker");

        message.MarkDeadLettered("  dead-letter reason  ", DateTime.UtcNow);

        Assert.Multiple(
            () => Assert.Equal(1, message.AttemptCount),
            () => Assert.NotNull(message.DeadLetteredOnUtc),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.LockedBy),
            () => Assert.Equal("dead-letter reason", message.LastError));
    }

    [Fact]
    public void MarkReplayed_ClearsDeadLetterAndLockState() {
        DateTime now = DateTime.UtcNow;
        var message = NotificationWebPushOutboxMessage.Create(NotificationId.New(), now.AddMinutes(-2));
        message.MarkClaimed(now.AddMinutes(5), "worker");
        message.MarkDeadLettered("failure", now.AddMinutes(-1));

        message.MarkReplayed(now);

        Assert.Multiple(
            () => Assert.Equal(now, message.NextAttemptOnUtc),
            () => Assert.Null(message.DeadLetteredOnUtc),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.LockedBy),
            () => Assert.Null(message.LastError));
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
