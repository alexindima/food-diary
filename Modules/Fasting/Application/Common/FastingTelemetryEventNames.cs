namespace FoodDiary.Modules.Fasting.Application.Common;

public static class FastingTelemetryEventNames {
    public const string ReminderPresetSelected = "fasting.reminder-preset.selected";
    public const string ReminderTimingSaved = "fasting.reminder-timing.saved";
    public const string SessionStarted = "fasting.session.started";
    public const string SessionCompleted = "fasting.session.completed";
    public const string CheckInSaved = "fasting.check-in.saved";

    public static bool IsSupported(string? name) => name is
        ReminderPresetSelected or
        ReminderTimingSaved or
        SessionStarted or
        SessionCompleted or
        CheckInSaved;
}
