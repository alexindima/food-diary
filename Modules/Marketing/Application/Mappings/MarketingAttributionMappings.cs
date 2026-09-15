using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
using FoodDiary.Modules.Marketing.Contracts.Models;

namespace FoodDiary.Modules.Marketing.Application.Mappings;

internal static class MarketingAttributionMappings {
    internal static MarketingAttributionSummaryModel ToSummaryModel(MarketingAttributionSummaryRecord summary, int normalizedWindowHours, DateTime nowUtc) {
        return new MarketingAttributionSummaryModel(
            normalizedWindowHours,
            nowUtc,
            summary.Events,
            summary.Visits,
            summary.Signups,
            summary.PremiumStarts,
            summary.AnonymousVisitors,
            summary.Sessions,
            summary.AttributedEvents,
            summary.Events - summary.AttributedEvents,
            summary.AttributedVisits,
            summary.Visits - summary.AttributedVisits,
            CalculateRate(summary.Signups, summary.Visits),
            CalculateRate(summary.PremiumStarts, summary.Signups),
            summary.LastEventAtUtc,
            [.. summary.TopCampaigns.Select(ToModel)],
            [.. summary.TopSources.Select(ToModel)],
            [.. summary.RecentEvents.Select(static x => new MarketingAttributionRecentEventModel(
                    x.OccurredAtUtc,
                    x.EventType,
                    x.AnonymousId,
                    x.SessionId,
                    x.LandingPath,
                    x.ReferrerHost,
                    x.UtmSource,
                    x.UtmMedium,
                    x.UtmCampaign,
                    x.UtmContent,
                    x.UtmTerm,
                    x.BuildVersion))]);
    }

    private static MarketingAttributionBreakdownModel ToModel(MarketingAttributionBreakdownRecord value) =>
        new(value.Source, value.Medium, value.Campaign, value.Events, value.Visits, value.Signups,
            value.PremiumStarts, value.AnonymousVisitors, value.Sessions,
            CalculateRate(value.Signups, value.Visits), CalculateRate(value.PremiumStarts, value.Signups), value.LastEventAtUtc);

    private static double CalculateRate(int numerator, int denominator) {
        return denominator > 0 ? Math.Round((double)numerator / denominator * 100, 1, MidpointRounding.ToEven) : 0;
    }
}
