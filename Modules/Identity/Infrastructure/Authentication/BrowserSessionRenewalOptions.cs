namespace FoodDiary.Modules.Identity.Infrastructure.Authentication;

public sealed class BrowserSessionRenewalOptions {
    public const string SectionName = "BrowserSessions";
    public int RenewalWindowDays { get; init; } = 7;
    public int? AbsoluteLifetimeDays { get; init; }

    public static bool IsValid(BrowserSessionRenewalOptions options) =>
        options.RenewalWindowDays is > 0 and <= 30 && options.AbsoluteLifetimeDays is null or >= 90 and <= 3650;
}
