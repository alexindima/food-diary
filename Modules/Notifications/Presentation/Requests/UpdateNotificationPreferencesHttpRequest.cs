namespace FoodDiary.Modules.Notifications.Presentation.Requests;

public sealed record UpdateNotificationPreferencesHttpRequest(
    bool? PushNotificationsEnabled,
    bool? FastingPushNotificationsEnabled,
    bool? SocialPushNotificationsEnabled,
    int? FastingCheckInReminderHours,
    int? FastingCheckInFollowUpReminderHours);
