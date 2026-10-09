using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public readonly record struct UserPreferenceState(
    string? DashboardLayoutJson,
    string? Language,
    string? Theme,
    string? UiStyle,
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled,
    FastingReminderSchedule ReminderDelays,
    string SurfaceStyle = "normal") {
    public int FastingCheckInReminderHours => ReminderDelays.FirstHours;
    public int FastingCheckInFollowUpReminderHours => ReminderDelays.FollowUpHours;
    public static UserPreferenceState CreateInitial() {
        return new UserPreferenceState(
            DashboardLayoutJson: null,
            Language: null,
            Theme: ThemeCode.Default.Value,
            UiStyle: UiStyleCode.Default.Value,
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true,
            ReminderDelays: FastingReminderSchedule.Default);
    }
}
