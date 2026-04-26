using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Notifications.Common;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationCleanupService(
    INotificationRepository notificationRepository,
    IDateTimeProvider dateTimeProvider) : INotificationCleanupService {
    public Task<int> CleanupExpiredNotificationsAsync(NotificationCleanupPolicy policy, CancellationToken cancellationToken = default) {
        if (policy.BatchSize <= 0) {
            return Task.FromResult(0);
        }

        var utcNow = dateTimeProvider.UtcNow;

        return notificationRepository.DeleteExpiredBatchAsync(
            policy.TransientTypes,
            utcNow.AddDays(-Math.Max(policy.TransientReadRetentionDays, 1)),
            utcNow.AddDays(-Math.Max(policy.TransientUnreadRetentionDays, 1)),
            utcNow.AddDays(-Math.Max(policy.StandardReadRetentionDays, 1)),
            utcNow.AddDays(-Math.Max(policy.StandardUnreadRetentionDays, 1)),
            policy.BatchSize,
            cancellationToken);
    }
}
