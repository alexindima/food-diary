using FoodDiary.Modules.Notifications.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface INotificationWebPushOutbox {
    Task EnqueueAsync(NotificationId notificationId, CancellationToken cancellationToken = default);
}
