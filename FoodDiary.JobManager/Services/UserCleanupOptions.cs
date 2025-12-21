namespace FoodDiary.JobManager.Services;

public sealed class UserCleanupOptions
{
    public const string SectionName = "UserCleanup";

    public int RetentionDays { get; set; } = 30;
    public int BatchSize { get; set; } = 50;
    public string Cron { get; set; } = "0 3 * * *";
    public string? ReassignUserId { get; set; }
}
