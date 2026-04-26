using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface IWebPushNotificationSender {
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
