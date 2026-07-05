using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
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
            NotificationTargetUrlResolver.Resolve(notification.Type, notification.ReferenceId),
            notification.ReferenceId,
            notification.IsRead,
            notification.CreatedOnUtc);

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
