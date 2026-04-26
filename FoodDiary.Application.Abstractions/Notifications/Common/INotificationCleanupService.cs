namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationCleanupService {
    Task<int> CleanupExpiredNotificationsAsync(NotificationCleanupPolicy policy, CancellationToken cancellationToken = default);
}

public sealed record NotificationCleanupPolicy(
    IReadOnlyCollection<string> TransientTypes,
    int TransientReadRetentionDays,
    int TransientUnreadRetentionDays,
    int StandardReadRetentionDays,
    int StandardUnreadRetentionDays,
    int BatchSize);
