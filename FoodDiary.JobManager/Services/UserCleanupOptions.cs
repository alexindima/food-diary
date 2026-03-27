namespace FoodDiary.JobManager.Services;

public sealed class UserCleanupOptions {
    public const string SectionName = "UserCleanup";

    public int RetentionDays { get; init; } = 30;
    public int BatchSize { get; init; } = 50;
    public string Cron { get; init; } = "0 3 * * *";
    public string? ReassignUserId { get; init; }

    public static bool HasValidConfiguration(UserCleanupOptions options) {
        return options.RetentionDays > 0 &&
               options.BatchSize > 0 &&
               !string.IsNullOrWhiteSpace(options.Cron) &&
               (string.IsNullOrWhiteSpace(options.ReassignUserId) || Guid.TryParse(options.ReassignUserId, out _));
    }
}
