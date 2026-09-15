namespace FoodDiary.Modules.Notifications.Presentation.Contracts.Responses;

public sealed record NotificationPreferencesHttpResponse(
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled,
    int FastingCheckInReminderHours,
    int FastingCheckInFollowUpReminderHours);
