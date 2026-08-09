namespace FoodDiary.JobManager.Services;

public sealed class AchievementEvaluationOutboxOptions {
    public const string SectionName = "AchievementEvaluationOutbox";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public string Cron { get; set; } = "* * * * *";

    public static bool HasValidConfiguration(AchievementEvaluationOutboxOptions options) =>
        !options.Enabled || (options.BatchSize > 0 && !string.IsNullOrWhiteSpace(options.Cron));
}
