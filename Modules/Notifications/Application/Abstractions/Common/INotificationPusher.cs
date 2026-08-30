namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationPusher {
    Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default);
    Task PushNotificationsChangedAsync(Guid userId, CancellationToken cancellationToken = default);
}
