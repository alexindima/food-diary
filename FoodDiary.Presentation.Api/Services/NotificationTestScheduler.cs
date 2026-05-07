using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Services;

public sealed class NotificationTestScheduler(
    IServiceScopeFactory serviceScopeFactory,
    IHostApplicationLifetime applicationLifetime,
    ILogger<NotificationTestScheduler> logger)
    : INotificationTestScheduler {
    public Task<ScheduledNotificationHttpResponse> ScheduleAsync(
        Guid userId,
        int delaySeconds,
        string type,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedDelaySeconds = Math.Clamp(delaySeconds, 1, 3600);
        var normalizedType = NormalizeType(type);
        var scheduledAtUtc = DateTime.UtcNow.AddSeconds(normalizedDelaySeconds);

        _ = RunScheduledAsync(userId, normalizedDelaySeconds, normalizedType, applicationLifetime.ApplicationStopping);

        return Task.FromResult(new ScheduledNotificationHttpResponse(
            normalizedType,
            normalizedDelaySeconds,
            scheduledAtUtc));
    }

    private async Task RunScheduledAsync(Guid userId, int delaySeconds, string type, CancellationToken cancellationToken) {
        try {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

            using var scope = serviceScopeFactory.CreateScope();
            var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var notificationPusher = scope.ServiceProvider.GetRequiredService<INotificationPusher>();
            var webPushNotificationSender = scope.ServiceProvider.GetRequiredService<IWebPushNotificationSender>();
            var domainUserId = new FoodDiary.Domain.ValueObjects.Ids.UserId(userId);
            var referenceId = $"test-notification:{type}:{Guid.NewGuid():N}";

            var notification = type switch {
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

            await notificationRepository.AddAsync(notification, cancellationToken);
            await webPushNotificationSender.SendAsync(notification, cancellationToken);
            var unreadCount = await notificationRepository.GetUnreadCountAsync(domainUserId, cancellationToken);
            await notificationPusher.PushUnreadCountAsync(userId, unreadCount, cancellationToken);
            await notificationPusher.PushNotificationsChangedAsync(userId, cancellationToken);
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
