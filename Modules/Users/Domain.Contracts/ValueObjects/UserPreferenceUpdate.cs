namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

public readonly record struct UserPreferenceUpdate(
    FastingReminderDelayUpdate ReminderDelays,
    string? DashboardLayoutJson = null,
    string? Language = null,
    string? Theme = null,
    string? UiStyle = null,
    bool? PushNotificationsEnabled = null,
    bool? FastingPushNotificationsEnabled = null,
    bool? SocialPushNotificationsEnabled = null,
    string? SurfaceStyle = null) {
    public int? FastingCheckInReminderHours => ReminderDelays.FirstHours;
    public int? FastingCheckInFollowUpReminderHours => ReminderDelays.FollowUpHours;

    // Existing scalar callers decode partial fields without moving owner validation earlier.
    public UserPreferenceUpdate(
        string? dashboardLayoutJson = null,
        string? language = null,
        string? theme = null,
        string? uiStyle = null,
        bool? pushNotificationsEnabled = null,
        bool? fastingPushNotificationsEnabled = null,
        bool? socialPushNotificationsEnabled = null,
        int? fastingCheckInReminderHours = null,
        int? fastingCheckInFollowUpReminderHours = null,
        string? surfaceStyle = null)
        : this(new FastingReminderDelayUpdate(fastingCheckInReminderHours, fastingCheckInFollowUpReminderHours),
            dashboardLayoutJson, language, theme, uiStyle, pushNotificationsEnabled, fastingPushNotificationsEnabled,
            socialPushNotificationsEnabled, surfaceStyle) { }
}
