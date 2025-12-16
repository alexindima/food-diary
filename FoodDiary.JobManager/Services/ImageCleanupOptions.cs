namespace FoodDiary.JobManager.Services;

public sealed class ImageCleanupOptions
{
    public const string SectionName = "ImageCleanup";

    public int OlderThanHours { get; init; } = 12;
    public int BatchSize { get; init; } = 50;
    public string Cron { get; init; } = "0 * * * *"; // hourly
}

