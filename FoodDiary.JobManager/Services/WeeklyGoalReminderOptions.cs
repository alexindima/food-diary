namespace FoodDiary.JobManager.Services;

public sealed class WeeklyGoalReminderOptions {
    public const string SectionName = "WeeklyGoalReminders";

    public bool Enabled { get; init; } = true;
    public string Cron { get; init; } = "*/15 * * * *";

    public static bool HasValidConfiguration(WeeklyGoalReminderOptions options) =>
        !options.Enabled || !string.IsNullOrWhiteSpace(options.Cron);
}
