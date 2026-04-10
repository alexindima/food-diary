namespace FoodDiary.Web.Api.Options;

public sealed class FastingNotificationOptions {
    public const string SectionName = "FastingNotifications";

    public bool Enabled { get; init; } = true;
    public int PollIntervalSeconds { get; init; } = 60;

    public static bool HasValidConfiguration(FastingNotificationOptions options) =>
        !options.Enabled || options.PollIntervalSeconds > 0;
}
