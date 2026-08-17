namespace FoodDiary.Application.Marketing.Common;

public static class MarketingAttributionEventTypes {
    public const string PageLanding = "page_landing";
    public const string SignupCompleted = "signup_completed";
    public const string PremiumStarted = "premium_started";

    public static bool IsClientIngestionSupported(string? eventType) =>
        eventType is PageLanding or SignupCompleted;
}
