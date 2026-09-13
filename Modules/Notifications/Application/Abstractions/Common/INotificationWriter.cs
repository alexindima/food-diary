namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationWriter {
    Task AddAsync(
        NotificationRequest request,
        bool sendWebPush = false,
        CancellationToken cancellationToken = default);
}
