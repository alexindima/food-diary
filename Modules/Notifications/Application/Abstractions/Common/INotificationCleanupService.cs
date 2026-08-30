namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationCleanupService {
    Task<int> CleanupExpiredNotificationsAsync(NotificationCleanupPolicy policy, CancellationToken cancellationToken = default);
}
