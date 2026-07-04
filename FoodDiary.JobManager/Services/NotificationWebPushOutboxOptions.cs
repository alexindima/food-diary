namespace FoodDiary.JobManager.Services;

public sealed class NotificationWebPushOutboxOptions {
    public const string SectionName = "NotificationWebPushOutbox";

    public bool Enabled { get; init; } = true;
    public int BatchSize { get; init; } = 50;
    public string Cron { get; init; } = "* * * * *";

    public static bool HasValidConfiguration(NotificationWebPushOutboxOptions options) =>
        !options.Enabled ||
        (options.BatchSize > 0 && !string.IsNullOrWhiteSpace(options.Cron));
}
