namespace FoodDiary.Modules.Notifications.Contracts.Common;

public interface INotificationWriter {
    Task AddAsync(
        NotificationRequest request,
        bool sendWebPush = false,
        CancellationToken cancellationToken = default);
}
