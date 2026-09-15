using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Modules.Notifications.Domain.Entities;

namespace FoodDiary.Modules.Notifications.Application.Mappings;

public static class NotificationMappings {
    public static NotificationModel ToModel(this Notification notification, NotificationText notificationText) =>
        new(
            notification.Id.Value,
            notification.Type,
            notificationText.Title,
            notificationText.Body,
            NotificationTargetUrlResolver.Resolve(notification.Type, notification.ReferenceId),
            notification.ReferenceId,
            notification.IsRead,
            notification.CreatedOnUtc);

    public static NotificationModel ToModel(this NotificationReadModel notification, NotificationText notificationText) =>
        new(
            notification.Id,
            notification.Type,
            notificationText.Title,
            notificationText.Body,
            NotificationTargetUrlResolver.Resolve(notification.Type, notification.ReferenceId),
            notification.ReferenceId,
            notification.IsRead,
            notification.CreatedAtUtc);

    public static WebPushSubscriptionModel ToModel(this WebPushSubscription subscription) =>
        new(
            subscription.Endpoint,
            WebPushEndpointHost.Resolve(subscription.Endpoint),
            subscription.ExpirationTimeUtc,
            subscription.Locale,
            subscription.UserAgent,
            subscription.CreatedOnUtc,
            subscription.ModifiedOnUtc);

    public static WebPushSubscriptionModel ToModel(this WebPushSubscriptionReadModel subscription) =>
        new(
            subscription.Endpoint,
            WebPushEndpointHost.Resolve(subscription.Endpoint),
            subscription.ExpirationTimeUtc,
            subscription.Locale,
            subscription.UserAgent,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc);
}
