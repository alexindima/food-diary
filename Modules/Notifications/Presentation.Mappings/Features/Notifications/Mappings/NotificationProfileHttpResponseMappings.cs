using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;

namespace FoodDiary.Presentation.Api.Features.Notifications.Mappings;

public static class NotificationProfileHttpResponseMappings {
    extension(UserNotificationPreferencesModel model) {
        public NotificationPreferencesHttpResponse ToHttpResponse() =>
                new(
                    model.PushNotificationsEnabled,
                    model.FastingPushNotificationsEnabled,
                    model.SocialPushNotificationsEnabled,
                    model.FastingCheckInReminderHours,
                    model.FastingCheckInFollowUpReminderHours);
    }

    extension(ProfileWebPushSubscriptionModel subscription) {
        public WebPushSubscriptionHttpResponse ToHttpResponse() =>
                new(
                    subscription.Endpoint,
                    subscription.EndpointHost,
                    subscription.ExpirationTimeUtc,
                    subscription.Locale,
                    subscription.UserAgent,
                    subscription.CreatedAtUtc,
                    subscription.UpdatedAtUtc);
    }
}
