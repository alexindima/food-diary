using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;
using FoodDiary.Application.Notifications.Queries.GetNotifications;
using FoodDiary.Application.Notifications.Queries.GetUnreadCount;
using FoodDiary.Application.Notifications.Commands.MarkNotificationRead;
using FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;
using FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;

namespace FoodDiary.Presentation.Api.Features.Notifications.Mappings;

public static class NotificationHttpMappings {
    public static GetNotificationsQuery ToNotificationsQuery(this Guid userId) => new(userId);

    public static GetUnreadCountQuery ToUnreadCountQuery(this Guid userId) => new(userId);

    public static MarkNotificationReadCommand ToMarkReadCommand(this Guid notificationId, Guid userId) => new(userId, notificationId);

    public static MarkAllNotificationsReadCommand ToMarkAllReadCommand(this Guid userId) => new(userId);

    public static GetNotificationPreferencesQuery ToNotificationPreferencesQuery(this Guid userId) => new(userId);

    public static UpdateNotificationPreferencesCommand ToCommand(this UpdateNotificationPreferencesHttpRequest request, Guid userId) =>
        new(
            userId,
            request.PushNotificationsEnabled,
            request.FastingPushNotificationsEnabled,
            request.SocialPushNotificationsEnabled,
            request.FastingCheckInReminderHours,
            request.FastingCheckInFollowUpReminderHours);

    public static NotificationHttpResponse ToHttpResponse(this NotificationModel model) =>
        new(model.Id, model.Type, model.Title, model.Body, model.TargetUrl, model.ReferenceId, model.IsRead, model.CreatedAtUtc);

    public static NotificationPreferencesHttpResponse ToHttpResponse(this NotificationPreferencesModel model) =>
        new(
            model.PushNotificationsEnabled,
            model.FastingPushNotificationsEnabled,
            model.SocialPushNotificationsEnabled,
            model.FastingCheckInReminderHours,
            model.FastingCheckInFollowUpReminderHours);

    public static WebPushSubscriptionHttpResponse ToHttpResponse(this FoodDiary.Domain.Entities.Notifications.WebPushSubscription subscription) =>
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
