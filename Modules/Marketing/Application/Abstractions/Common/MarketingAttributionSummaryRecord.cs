namespace FoodDiary.Application.Abstractions.Marketing.Common;

public sealed record MarketingAttributionSummaryRecord(
    int Events,
    int Visits,
    int Signups,
    int PremiumStarts,
    int AnonymousVisitors,
    int Sessions,
    int AttributedEvents,
    int AttributedVisits,
    DateTime? LastEventAtUtc,
    IReadOnlyList<MarketingAttributionBreakdownRecord> TopCampaigns,
    IReadOnlyList<MarketingAttributionBreakdownRecord> TopSources,
    IReadOnlyList<MarketingAttributionEventRecord> RecentEvents);
