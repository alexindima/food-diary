using FoodDiary.Application.Abstractions.Notifications.Common;
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
            GetEndpointHost(subscription.Endpoint),
            subscription.ExpirationTimeUtc,
            subscription.Locale,
            subscription.UserAgent,
            subscription.CreatedOnUtc,
            subscription.ModifiedOnUtc);

    private static string GetEndpointHost(string endpoint) {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            ? uri.Host
            : endpoint;
    }
}
