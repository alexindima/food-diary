using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationWriter {
    Task AddAsync(
        Notification notification,
        bool sendWebPush = false,
        CancellationToken cancellationToken = default);
}
