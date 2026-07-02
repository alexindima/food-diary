namespace FoodDiary.Web.Api.Options;

public sealed class NotificationWebPushOutboxOptions {
    public const string SectionName = "NotificationWebPushOutbox";

    public bool Enabled { get; init; } = true;
    public int BatchSize { get; init; } = 50;
    public int PollIntervalSeconds { get; init; } = 30;

    public static bool HasValidConfiguration(NotificationWebPushOutboxOptions options) =>
        !options.Enabled || options is { BatchSize: > 0, PollIntervalSeconds: > 0 };
}
