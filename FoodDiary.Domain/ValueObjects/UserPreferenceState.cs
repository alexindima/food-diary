namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserPreferenceState(
    string? DashboardLayoutJson,
    string? Language,
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled,
    int FastingCheckInReminderHours,
    int FastingCheckInFollowUpReminderHours) {
    public static UserPreferenceState CreateInitial() {
        return new UserPreferenceState(
            DashboardLayoutJson: null,
            Language: null,
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 12,
            FastingCheckInFollowUpReminderHours: 20);
    }
}
