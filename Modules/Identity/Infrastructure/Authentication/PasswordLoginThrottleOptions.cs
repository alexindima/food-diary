namespace FoodDiary.Modules.Identity.Infrastructure.Authentication;

public sealed class PasswordLoginThrottleOptions {
    public const string SectionName = "PasswordLoginThrottle";
    public int FailureThreshold { get; init; } = 10;
    public int FailureWindowSeconds { get; init; } = 900;
    public int InitialCooldownSeconds { get; init; } = 30;
    public int MaximumCooldownSeconds { get; init; } = 300;
    public int AttemptLeaseSeconds { get; init; } = 30;
    public int MaximumTrackedEmails { get; init; } = 10000;

    public static bool IsValid(PasswordLoginThrottleOptions options) =>
        options.FailureThreshold is > 0 and <= 100 &&
        options.FailureWindowSeconds is > 0 and <= 86400 &&
        options.InitialCooldownSeconds > 0 &&
        options.MaximumCooldownSeconds >= options.InitialCooldownSeconds &&
        options.MaximumCooldownSeconds <= options.FailureWindowSeconds &&
        options.AttemptLeaseSeconds is > 0 and <= 300 &&
        options.MaximumTrackedEmails is > 0 and <= 100000;
}
