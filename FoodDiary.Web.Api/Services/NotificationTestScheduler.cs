using FoodDiary.Application.Abstractions.Notifications.Common;

namespace FoodDiary.Web.Api.Services;

public sealed class NotificationTestScheduler(
    IServiceScopeFactory serviceScopeFactory,
    IHostApplicationLifetime applicationLifetime,
    TimeProvider timeProvider,
    ILogger<NotificationTestScheduler> logger)
    : INotificationTestScheduler {
    public Task<ScheduledNotificationData> ScheduleAsync(
        Guid userId,
        int delaySeconds,
        string type,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        int normalizedDelaySeconds = Math.Clamp(delaySeconds, 1, 3600);
        string normalizedType = NormalizeType(type);
        DateTime scheduledAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(normalizedDelaySeconds);
        _ = RunScheduledAsync(userId, normalizedDelaySeconds, normalizedType, applicationLifetime.ApplicationStopping);
        return Task.FromResult(new ScheduledNotificationData(normalizedType, normalizedDelaySeconds, scheduledAtUtc));
    }

    private async Task RunScheduledAsync(Guid userId, int delaySeconds, string type, CancellationToken cancellationToken) {
        try {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            ITestNotificationDeliveryDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<ITestNotificationDeliveryDispatcher>();
            await dispatcher.DispatchAsync(userId, type, cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        } catch (Exception ex) {
            logger.LogError(ex, "Failed to deliver scheduled test notification for user {UserId}.", userId);
        }
    }

    private static string NormalizeType(string? type) {
        if (string.IsNullOrWhiteSpace(type)) {
            return NotificationTypes.FastingCompleted;
        }

        return type.Trim() switch {
            NotificationTypes.FastingCompleted => NotificationTypes.FastingCompleted,
            NotificationTypes.FastingCheckInReminder => NotificationTypes.FastingCheckInReminder,
            NotificationTypes.EatingWindowStarted => NotificationTypes.EatingWindowStarted,
            NotificationTypes.FastingWindowStarted => NotificationTypes.FastingWindowStarted,
            _ => NotificationTypes.FastingCompleted,
        };
    }
}
