namespace FoodDiary.MailRelay.Infrastructure.Options;

public sealed class MailRelayQueueOptions {
    public const string SectionName = "MailRelayQueue";

    public int PollIntervalSeconds { get; init; } = 5;
    public int BatchSize { get; init; } = 10;
    public int MaxAttempts { get; init; } = 6;
    public int BaseRetryDelaySeconds { get; init; } = 30;
    public int MaxRetryDelaySeconds { get; init; } = 1800;
    public int LockTimeoutSeconds { get; init; } = 120;

    public static bool HasValidConfiguration(MailRelayQueueOptions options) {
        return options.PollIntervalSeconds > 0 &&
               options.BatchSize > 0 &&
               options.MaxAttempts > 0 &&
               options.BaseRetryDelaySeconds > 0 &&
               options.MaxRetryDelaySeconds >= options.BaseRetryDelaySeconds &&
               options.LockTimeoutSeconds > 0;
    }
}
