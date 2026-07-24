namespace FoodDiary.JobManager.Services;

public sealed class ClientTaskReminderOptions {
    public const string SectionName = "ClientTaskReminders";

    public bool Enabled { get; init; } = true;
    public string Cron { get; init; } = "0 * * * *";

    public static bool HasValidConfiguration(ClientTaskReminderOptions options) =>
        !options.Enabled || !string.IsNullOrWhiteSpace(options.Cron);
}
