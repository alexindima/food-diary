using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Notifications.Mappings;

public static class NotificationMappings {
    public static NotificationModel ToModel(this Notification notification) =>
        new(
            notification.Id.Value,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.ReferenceId,
            notification.IsRead,
            notification.CreatedOnUtc);
}
