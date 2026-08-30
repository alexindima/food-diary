using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationWebPushOutbox {
    Task EnqueueAsync(NotificationId notificationId, CancellationToken cancellationToken = default);
}
