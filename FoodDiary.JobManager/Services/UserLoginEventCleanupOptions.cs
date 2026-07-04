namespace FoodDiary.JobManager.Services;

public sealed class UserLoginEventCleanupOptions {
    public const string SectionName = "UserLoginEventCleanup";

    public bool Enabled { get; init; } = true;
    public int RetentionDays { get; init; } = 180;
    public int BatchSize { get; init; } = 500;
    public string Cron { get; init; } = "0 3 * * *";

    public static bool HasValidConfiguration(UserLoginEventCleanupOptions options) =>
        !options.Enabled ||
        (options.RetentionDays > 0 &&
            options.BatchSize > 0 &&
            !string.IsNullOrWhiteSpace(options.Cron));
}
