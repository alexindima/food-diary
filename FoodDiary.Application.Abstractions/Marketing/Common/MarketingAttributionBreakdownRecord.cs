namespace FoodDiary.Application.Abstractions.Marketing.Common;

public sealed record MarketingAttributionBreakdownRecord(
    string Source,
    string Medium,
    string Campaign,
    int Events,
    int Visits,
    int Signups,
    int PremiumStarts,
    int AnonymousVisitors,
    int Sessions,
    DateTime? LastEventAtUtc);
