namespace FoodDiary.JobManager.Services;

public sealed class ImageCleanupOptions {
    public const string SectionName = "ImageCleanup";

    public int OlderThanHours { get; init; } = 12;
    public int BatchSize { get; init; } = 50;

    /// <summary>
    /// Gets the cleanup schedule. The default value runs the job hourly.
    /// </summary>
    public string Cron { get; init; } = "0 * * * *";

    public static bool HasValidConfiguration(ImageCleanupOptions options) {
        return options is { OlderThanHours: > 0, BatchSize: > 0 } &&
               !string.IsNullOrWhiteSpace(options.Cron);
    }
}
