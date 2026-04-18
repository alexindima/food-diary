namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserPreferenceState(
    string? DashboardLayoutJson,
    string? Language,
    string? Theme,
    string? UiStyle,
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled,
    int FastingCheckInReminderHours,
    int FastingCheckInFollowUpReminderHours) {
    public static UserPreferenceState CreateInitial() {
        return new UserPreferenceState(
            DashboardLayoutJson: null,
            Language: null,
            Theme: ThemeCode.Default.Value,
            UiStyle: UiStyleCode.Default.Value,
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 12,
            FastingCheckInFollowUpReminderHours: 20);
    }
}
