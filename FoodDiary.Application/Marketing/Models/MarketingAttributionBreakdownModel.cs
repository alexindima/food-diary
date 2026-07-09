namespace FoodDiary.Application.Marketing.Models;

public sealed record MarketingAttributionBreakdownModel(
    string Source,
    string Medium,
    string Campaign,
    int Events,
    int Visits,
    int Signups,
    int PremiumStarts,
    int AnonymousVisitors,
    int Sessions,
    double SignupRatePercent,
    double PremiumRatePercent,
    DateTime? LastEventAtUtc);
