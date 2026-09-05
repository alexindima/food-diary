namespace FoodDiary.Presentation.Api.Features.Notifications.Requests;

public sealed record UpdateNotificationPreferencesHttpRequest(
    bool? PushNotificationsEnabled,
    bool? FastingPushNotificationsEnabled,
    bool? SocialPushNotificationsEnabled,
    int? FastingCheckInReminderHours,
    int? FastingCheckInFollowUpReminderHours);
