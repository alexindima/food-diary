namespace FoodDiary.Application.Notifications.Common;

public interface INotificationPusher {
    Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default);
}
