namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserPreferenceUpdate(
    string? DashboardLayoutJson = null,
    string? Language = null,
    string? Theme = null,
    string? UiStyle = null,
    bool? PushNotificationsEnabled = null,
    bool? FastingPushNotificationsEnabled = null,
    bool? SocialPushNotificationsEnabled = null,
    int? FastingCheckInReminderHours = null,
    int? FastingCheckInFollowUpReminderHours = null);
