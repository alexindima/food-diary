namespace FoodDiary.JobManager.Services;

public sealed class BillingRenewalOptions {
    public const string SectionName = "BillingRenewal";

    public bool Enabled { get; init; }
    public string Provider { get; init; } = "YooKassa";
    public int BatchSize { get; init; } = 50;
    public string Cron { get; init; } = "15 * * * *";

    public static bool HasValidConfiguration(BillingRenewalOptions options) =>
        !options.Enabled ||
        (!string.IsNullOrWhiteSpace(options.Provider) &&
            options.BatchSize > 0 &&
            !string.IsNullOrWhiteSpace(options.Cron));
}
