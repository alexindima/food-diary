namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record MarketingAttributionSummaryHttpResponse(
    int WindowHours,
    DateTime GeneratedAtUtc,
    int Events,
    int Visits,
    int Signups,
    int PremiumStarts,
    int AnonymousVisitors,
    int Sessions,
    int AttributedEvents,
    int OrganicEvents,
    double SignupRatePercent,
    double PremiumRatePercent,
    DateTime? LastEventAtUtc,
    IReadOnlyList<MarketingAttributionBreakdownHttpResponse> TopCampaigns,
    IReadOnlyList<MarketingAttributionBreakdownHttpResponse> TopSources,
    IReadOnlyList<MarketingAttributionRecentEventHttpResponse> RecentEvents);
