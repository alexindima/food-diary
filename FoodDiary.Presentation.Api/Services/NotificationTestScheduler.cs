using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Services;

public sealed class NotificationTestScheduler(
    IServiceScopeFactory serviceScopeFactory,
    IHostApplicationLifetime applicationLifetime,
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
        DateTime scheduledAtUtc = DateTime.UtcNow.AddSeconds(normalizedDelaySeconds);

        _ = RunScheduledAsync(userId, normalizedDelaySeconds, normalizedType, applicationLifetime.ApplicationStopping);

        return Task.FromResult(new ScheduledNotificationData(
            normalizedType,
            normalizedDelaySeconds,
            scheduledAtUtc));
    }

    private async Task RunScheduledAsync(Guid userId, int delaySeconds, string type, CancellationToken cancellationToken) {
        try {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);

            using IServiceScope scope = serviceScopeFactory.CreateScope();
            INotificationRepository notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            INotificationPusher notificationPusher = scope.ServiceProvider.GetRequiredService<INotificationPusher>();
            IWebPushNotificationSender webPushNotificationSender = scope.ServiceProvider.GetRequiredService<IWebPushNotificationSender>();
            var domainUserId = new FoodDiary.Domain.ValueObjects.Ids.UserId(userId);
            string referenceId = $"test-notification:{type}:{Guid.NewGuid():N}";

            FoodDiary.Domain.Entities.Notifications.Notification notification = type switch {
                NotificationTypes.FastingCompleted => NotificationFactory.CreateFastingCompleted(
                    domainUserId,
                    "Extended",
                    "FastDay",
                    referenceId),
                NotificationTypes.FastingCheckInReminder => NotificationFactory.CreateFastingCheckInReminder(
                    domainUserId,
                    referenceId),
                NotificationTypes.EatingWindowStarted => NotificationFactory.CreateEatingWindowStarted(
                    domainUserId,
                    "Intermittent",
                    "EatingWindow",
                    referenceId),
                NotificationTypes.FastingWindowStarted => NotificationFactory.CreateFastingWindowStarted(
                    domainUserId,
                    "Intermittent",
                    "FastingWindow",
                    referenceId),
                _ => NotificationFactory.CreateFastingCompleted(
                    domainUserId,
                    "Extended",
                    "FastDay",
                    referenceId)
            };

            await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);
            await webPushNotificationSender.SendAsync(notification, cancellationToken).ConfigureAwait(false);
            int unreadCount = await notificationRepository.GetUnreadCountAsync(domainUserId, cancellationToken).ConfigureAwait(false);
            await notificationPusher.PushUnreadCountAsync(userId, unreadCount, cancellationToken).ConfigureAwait(false);
            await notificationPusher.PushNotificationsChangedAsync(userId, cancellationToken).ConfigureAwait(false);
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
            _ => NotificationTypes.FastingCompleted
        };
    }
}
