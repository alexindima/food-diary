using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;

namespace FoodDiary.Application.Notifications.Services;

public sealed class NotificationCleanupService(
    INotificationWriteRepository notificationRepository,
    TimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : INotificationCleanupService {
    public async Task<int> CleanupExpiredNotificationsAsync(NotificationCleanupPolicy policy, CancellationToken cancellationToken = default) {
        if (policy.BatchSize <= 0) {
            return 0;
        }

        DateTime utcNow = dateTimeProvider.GetUtcNow().UtcDateTime;

        int deleted = await notificationRepository.DeleteExpiredBatchAsync(
            policy.TransientTypes,
            utcNow.AddDays(-Math.Max(policy.TransientReadRetentionDays, 1)),
            utcNow.AddDays(-Math.Max(policy.TransientUnreadRetentionDays, 1)),
            utcNow.AddDays(-Math.Max(policy.StandardReadRetentionDays, 1)),
            utcNow.AddDays(-Math.Max(policy.StandardUnreadRetentionDays, 1)),
            policy.BatchSize,
            cancellationToken).ConfigureAwait(false);

        if (deleted > 0) {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return deleted;
    }
}
