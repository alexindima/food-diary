namespace FoodDiary.Application.Marketing.Models;

public sealed record MarketingAttributionSummaryModel(
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
    IReadOnlyList<MarketingAttributionBreakdownModel> TopCampaigns,
    IReadOnlyList<MarketingAttributionBreakdownModel> TopSources,
    IReadOnlyList<MarketingAttributionRecentEventModel> RecentEvents);
