namespace FoodDiary.JobManager.Services;

public sealed class MarketingAttributionCleanupOptions {
    public const string SectionName = "MarketingAttributionCleanup";

    public bool Enabled { get; init; } = true;
    public int RetentionDays { get; init; } = 365;
    public int BatchSize { get; init; } = 500;
    public string Cron { get; init; } = "30 3 * * *";

    public static bool HasValidConfiguration(MarketingAttributionCleanupOptions options) =>
        !options.Enabled ||
        (options.RetentionDays > 0 &&
            options.BatchSize > 0 &&
            !string.IsNullOrWhiteSpace(options.Cron));
}
