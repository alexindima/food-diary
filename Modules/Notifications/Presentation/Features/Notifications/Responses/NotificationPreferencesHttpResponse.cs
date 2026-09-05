namespace FoodDiary.Presentation.Api.Features.Notifications.Responses;

public sealed record NotificationPreferencesHttpResponse(
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled,
    int FastingCheckInReminderHours,
    int FastingCheckInFollowUpReminderHours);
