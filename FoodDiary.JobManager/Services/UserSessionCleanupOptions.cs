namespace FoodDiary.JobManager.Services;

public sealed class UserSessionCleanupOptions {
    public const string SectionName = "UserSessionCleanup";
    public bool Enabled { get; init; } = true;
    public int RetentionDays { get; init; } = 30;
    public int BatchSize { get; init; } = 500;
    public int MaxBatches { get; init; } = 20;
    public string Cron { get; init; } = "15 3 * * *";

    public static bool HasValidConfiguration(UserSessionCleanupOptions options) => !options.Enabled ||
        (options is { RetentionDays: > 0, BatchSize: > 0 and <= 1000, MaxBatches: > 0 and <= 100 } &&
         !string.IsNullOrWhiteSpace(options.Cron));
}
