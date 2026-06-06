using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationTestSchedulerTests {
    [Fact]
    public async Task ScheduleAsync_NormalizesInput_AndDispatchesNotification() {
        var repository = new RecordingNotificationRepository();
        var pusher = new RecordingNotificationPusher();
        var sender = new RecordingWebPushNotificationSender();
        using ServiceProvider serviceProvider = BuildServiceProvider(repository, pusher, sender);
        using var lifetime = new TestHostApplicationLifetime();
        var scheduler = new NotificationTestScheduler(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            lifetime,
            NullLogger<NotificationTestScheduler>.Instance);
        var userId = Guid.NewGuid();

        ScheduledNotificationData response = await scheduler.ScheduleAsync(userId, 0, " unknown-type ", CancellationToken.None);

        Assert.Equal(NotificationTypes.FastingCompleted, response.Type);
        Assert.Equal(1, response.DelaySeconds);

        await sender.WaitAsync();

        Notification notification = Assert.Single(repository.Notifications);
        Assert.Equal(NotificationTypes.FastingCompleted, notification.Type);
        Assert.Equal(new UserId(userId), notification.UserId);
        Assert.Equal(1, pusher.UnreadCount);
        Assert.Equal(userId, pusher.UserId);
        Assert.True(pusher.NotificationsChangedPushed);
    }

    [Fact]
    public async Task ScheduleAsync_WhenDeliveryThrows_SwallowsFailure() {
        var repository = new RecordingNotificationRepository();
        var pusher = new RecordingNotificationPusher();
        var sender = new ThrowingWebPushNotificationSender();
        using ServiceProvider serviceProvider = BuildServiceProvider(repository, pusher, sender);
        using var lifetime = new TestHostApplicationLifetime();
        var scheduler = new NotificationTestScheduler(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            lifetime,
            NullLogger<NotificationTestScheduler>.Instance);

        ScheduledNotificationData response = await scheduler.ScheduleAsync(Guid.NewGuid(), 1, NotificationTypes.EatingWindowStarted, CancellationToken.None);
        Assert.Equal(NotificationTypes.EatingWindowStarted, response.Type);

        await sender.WaitAsync();

        Assert.Single(repository.Notifications);
        Assert.False(pusher.NotificationsChangedPushed);
        Assert.Equal(0, pusher.UnreadCount);
    }

    private static ServiceProvider BuildServiceProvider(
        INotificationRepository repository,
        INotificationPusher pusher,
        IWebPushNotificationSender sender) {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton<INotificationRepository>(repository);
        services.AddSingleton(pusher);
        services.AddSingleton<INotificationPusher>(pusher);
        services.AddSingleton(sender);
        services.AddSingleton<IWebPushNotificationSender>(sender);
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationRepository : INotificationRepository {
        public List<Notification> Notifications { get; } = [];

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult<Notification?>(Notifications.FirstOrDefault(x => x.Id == id));

        public Task<Notification> AddAsync(Notification notification, CancellationToken ct = default) {
            Notifications.Add(notification);
            return Task.FromResult(notification);
        }

        public Task UpdateAsync(Notification notification, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken ct = default) =>
            Task.FromResult(Notifications.Any(x => x.UserId == userId && string.Equals(x.Type, type, StringComparison.Ordinal) && string.Equals(x.ReferenceId, referenceId, StringComparison.Ordinal)));

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult(Notifications.Count(x => x.UserId == userId && !x.IsRead));

        public Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken ct = default) =>
            Task.FromResult(Notifications.Count(x => x.UserId == userId && !x.IsRead && string.Equals(x.Type, type, StringComparison.Ordinal)));

        public Task MarkAllReadAsync(UserId userId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(Notifications.Where(x => x.UserId == userId).Take(limit).ToList());

        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationPusher : INotificationPusher {
        public Guid UserId { get; private set; }
        public int UnreadCount { get; private set; }
        public bool NotificationsChangedPushed { get; private set; }

        public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default) {
            UserId = userId;
            UnreadCount = count;
            return Task.CompletedTask;
        }

        public Task PushNotificationsChangedAsync(Guid userId, CancellationToken cancellationToken = default) {
            UserId = userId;
            NotificationsChangedPushed = true;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingWebPushNotificationSender : IWebPushNotificationSender {
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SendAsync(Notification notification, CancellationToken cancellationToken = default) {
            completion.TrySetResult();
            return Task.CompletedTask;
        }

        public async Task WaitAsync() {
            Task finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
            Assert.Same(completion.Task, finished);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingWebPushNotificationSender : IWebPushNotificationSender {
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SendAsync(Notification notification, CancellationToken cancellationToken = default) {
            completion.TrySetResult();
            throw new InvalidOperationException("send failed");
        }

        public async Task WaitAsync() {
            Task finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(3))).ConfigureAwait(false);
            Assert.Same(completion.Task, finished);
            await Task.Delay(50).ConfigureAwait(false);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable {
        private readonly CancellationTokenSource cts = new();

        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => cts.Token;
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() => cts.Cancel();

        public void Dispose() => cts.Dispose();
    }
}
