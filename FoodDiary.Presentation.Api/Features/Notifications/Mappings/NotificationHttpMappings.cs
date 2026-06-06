using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Notifications.Commands.RemoveWebPushSubscription;
using FoodDiary.Application.Notifications.Commands.ScheduleTestNotification;
using FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;
using FoodDiary.Application.Notifications.Queries.GetNotifications;
using FoodDiary.Application.Notifications.Queries.GetUnreadCount;
using FoodDiary.Application.Notifications.Queries.GetWebPushConfiguration;
using FoodDiary.Application.Notifications.Queries.GetWebPushSubscriptions;
using FoodDiary.Application.Notifications.Commands.MarkNotificationRead;
using FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;
using FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;
using FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;

namespace FoodDiary.Presentation.Api.Features.Notifications.Mappings;

public static class NotificationHttpMappings {
    extension(Guid userId) {
        public GetNotificationsQuery ToNotificationsQuery() => new(userId);
        public GetUnreadCountQuery ToUnreadCountQuery() => new(userId);
        public MarkNotificationReadCommand ToMarkReadCommand(Guid userId1) => new(userId1, userId);
        public MarkAllNotificationsReadCommand ToMarkAllReadCommand() => new(userId);
        public GetNotificationPreferencesQuery ToNotificationPreferencesQuery() => new(userId);
        public GetWebPushSubscriptionsQuery ToWebPushSubscriptionsQuery() => new(userId);
    }

    public static UpdateNotificationPreferencesCommand ToCommand(this UpdateNotificationPreferencesHttpRequest request, Guid userId) =>
        new(
            userId,
            request.PushNotificationsEnabled,
            request.FastingPushNotificationsEnabled,
            request.SocialPushNotificationsEnabled,
            request.FastingCheckInReminderHours,
            request.FastingCheckInFollowUpReminderHours);

    public static GetWebPushConfigurationQuery ToWebPushConfigurationQuery() => new();

    public static UpsertWebPushSubscriptionCommand ToCommand(this UpsertWebPushSubscriptionHttpRequest request, Guid userId) =>
        new(
            userId,
            request.Endpoint,
            request.Keys.P256dh,
            request.Keys.Auth,
            request.ExpirationTime,
            request.Locale,
            request.UserAgent);

    public static RemoveWebPushSubscriptionCommand ToCommand(this RemoveWebPushSubscriptionHttpRequest request, Guid userId) =>
        new(userId, request.Endpoint);

    public static ScheduleTestNotificationCommand ToCommand(this ScheduleTestNotificationHttpRequest request, Guid userId) =>
        new(userId, request.DelaySeconds, request.Type);

    public static NotificationHttpResponse ToHttpResponse(this NotificationModel model) =>
        new(model.Id, model.Type, model.Title, model.Body, model.TargetUrl, model.ReferenceId, model.IsRead, model.CreatedAtUtc);

    public static ScheduledNotificationHttpResponse ToHttpResponse(this ScheduledNotificationModel model) =>
        new(model.Type, model.DelaySeconds, model.ScheduledAtUtc);

    public static WebPushConfigurationHttpResponse ToHttpResponse(this WebPushConfigurationModel model) =>
        new(model.Enabled, model.PublicKey);

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

    public static WebPushSubscriptionHttpResponse ToHttpResponse(this WebPushSubscriptionModel subscription) =>
        new(
            subscription.Endpoint,
            subscription.EndpointHost,
            subscription.ExpirationTimeUtc,
            subscription.Locale,
            subscription.UserAgent,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc);

    private static string GetEndpointHost(string endpoint) {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)
            ? uri.Host
            : endpoint;
    }
}
