namespace FoodDiary.JobManager.Services;

public sealed class ImageObjectDeletionOutboxOptions {
    public const string SectionName = "ImageObjectDeletionOutbox";

    public bool Enabled { get; init; } = true;
    public int BatchSize { get; init; } = 25;
    public string Cron { get; init; } = "* * * * *";

    public static bool HasValidConfiguration(ImageObjectDeletionOutboxOptions options) =>
        !options.Enabled ||
        (options.BatchSize > 0 && !string.IsNullOrWhiteSpace(options.Cron));
}
