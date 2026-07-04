namespace FoodDiary.JobManager.Services;

public sealed class FastingNotificationOptions {
    public const string SectionName = "FastingNotifications";

    public bool Enabled { get; init; } = true;
    public string Cron { get; init; } = "* * * * *";

    public static bool HasValidConfiguration(FastingNotificationOptions options) =>
        !options.Enabled || !string.IsNullOrWhiteSpace(options.Cron);
}
