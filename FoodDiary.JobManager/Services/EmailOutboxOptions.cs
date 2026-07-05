namespace FoodDiary.JobManager.Services;

public sealed class EmailOutboxOptions {
    public const string SectionName = "EmailOutbox";

    public bool Enabled { get; init; } = true;
    public int BatchSize { get; init; } = 50;
    public string Cron { get; init; } = "* * * * *";

    public static bool HasValidConfiguration(EmailOutboxOptions options) =>
        !options.Enabled ||
        (options.BatchSize > 0 && !string.IsNullOrWhiteSpace(options.Cron));
}
