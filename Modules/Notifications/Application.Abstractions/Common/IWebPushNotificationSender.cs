using FoodDiary.Modules.Notifications.Domain.Entities;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface IWebPushNotificationSender {
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
