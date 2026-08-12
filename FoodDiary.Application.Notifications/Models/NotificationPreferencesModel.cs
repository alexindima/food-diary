namespace FoodDiary.Application.Notifications.Models;

public sealed record NotificationPreferencesModel(
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled,
    int FastingCheckInReminderHours,
    int FastingCheckInFollowUpReminderHours);
