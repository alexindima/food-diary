namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record MarketingAttributionBreakdownHttpResponse(
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
