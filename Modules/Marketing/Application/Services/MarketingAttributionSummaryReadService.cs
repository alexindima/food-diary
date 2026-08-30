using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Marketing.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Services;

public sealed class MarketingAttributionSummaryReadService(
    IMarketingAttributionEventReadRepository repository,
    TimeProvider timeProvider)
    : IMarketingAttributionSummaryReadService {
    public async Task<Result<MarketingAttributionSummaryModel>> GetAsync(int hours, CancellationToken cancellationToken) {
        int normalizedWindowHours = Math.Clamp(hours, 1, 2160);
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        DateTime windowStartUtc = nowUtc.AddHours(-normalizedWindowHours);
        MarketingAttributionSummaryRecord summary = await repository.GetSummaryAsync(windowStartUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success(new MarketingAttributionSummaryModel(
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
                    x.BuildVersion))]));
    }

    private static MarketingAttributionBreakdownModel ToModel(MarketingAttributionBreakdownRecord value) =>
        new(value.Source, value.Medium, value.Campaign, value.Events, value.Visits, value.Signups,
            value.PremiumStarts, value.AnonymousVisitors, value.Sessions,
            CalculateRate(value.Signups, value.Visits), CalculateRate(value.PremiumStarts, value.Signups), value.LastEventAtUtc);

    private static double CalculateRate(int numerator, int denominator) {
        return denominator > 0 ? Math.Round((double)numerator / denominator * 100, 1, MidpointRounding.ToEven) : 0;
    }
}
