namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserNotificationPreferencesModel(
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled,
    int FastingCheckInReminderHours,
    int FastingCheckInFollowUpReminderHours);
