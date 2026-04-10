namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserPreferenceState(
    string? DashboardLayoutJson,
    string? Language,
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled) {
    public static UserPreferenceState CreateInitial() {
        return new UserPreferenceState(
            DashboardLayoutJson: null,
            Language: null,
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true);
    }
}
