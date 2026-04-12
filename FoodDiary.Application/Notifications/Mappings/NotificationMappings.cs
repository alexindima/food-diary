using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Notifications.Mappings;

public static class NotificationMappings {
    public static NotificationModel ToModel(this Notification notification, NotificationText notificationText) =>
        new(
            notification.Id.Value,
            notification.Type,
            notificationText.Title,
            notificationText.Body,
            NotificationTargetUrlResolver.Resolve(notification.Type),
            notification.ReferenceId,
            notification.IsRead,
            notification.CreatedOnUtc);
}
