namespace FoodDiary.JobManager.Services;

public sealed class FastingTelemetryCleanupOptions {
    public const string SectionName = "FastingTelemetryCleanup";

    public bool Enabled { get; init; } = true;
    public int RetentionDays { get; init; } = 90;
    public int BatchSize { get; init; } = 500;
    public string Cron { get; init; } = "45 3 * * *";

    public static bool HasValidConfiguration(FastingTelemetryCleanupOptions options) =>
        !options.Enabled ||
        (options is { RetentionDays: > 0, BatchSize: > 0 } &&
         !string.IsNullOrWhiteSpace(options.Cron));
}
