namespace FoodDiary.Web.Api.Options;

public sealed class UserLoginEventCleanupOptions {
    public const string SectionName = "UserLoginEventCleanup";

    public bool Enabled { get; init; } = true;
    public int RetentionDays { get; init; } = 180;
    public int BatchSize { get; init; } = 500;
    public int PollIntervalHours { get; init; } = 24;

    public static bool HasValidConfiguration(UserLoginEventCleanupOptions options) =>
        !options.Enabled ||
        options.RetentionDays > 0 &&
        options.BatchSize > 0 &&
        options.PollIntervalHours > 0;
}
