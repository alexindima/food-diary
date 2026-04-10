namespace FoodDiary.JobManager.Services;

public sealed class NotificationCleanupOptions {
    public const string SectionName = "NotificationCleanup";

    public IReadOnlyList<string> TransientTypes { get; init; } = [];
    public int TransientReadRetentionDays { get; init; } = 14;
    public int TransientUnreadRetentionDays { get; init; } = 30;
    public int StandardReadRetentionDays { get; init; } = 60;
    public int StandardUnreadRetentionDays { get; init; } = 90;
    public int BatchSize { get; init; } = 100;
    public string Cron { get; init; } = "0 4 * * *";

    public static bool HasValidConfiguration(NotificationCleanupOptions options) {
        return options.TransientTypes.Count > 0 &&
               options.TransientTypes.All(type => !string.IsNullOrWhiteSpace(type)) &&
               options.TransientReadRetentionDays > 0 &&
               options.TransientUnreadRetentionDays > 0 &&
               options.StandardReadRetentionDays > 0 &&
               options.StandardUnreadRetentionDays > 0 &&
               options.BatchSize > 0 &&
               !string.IsNullOrWhiteSpace(options.Cron);
    }
}
