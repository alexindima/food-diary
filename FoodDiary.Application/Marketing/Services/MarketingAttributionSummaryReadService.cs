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
        IReadOnlyList<MarketingAttributionEventRecord> events = await repository.GetSinceAsync(windowStartUtc, cancellationToken).ConfigureAwait(false);

        MarketingAttributionEventRecord[] attributedEvents = [.. events.Where(static x => HasUtmData(x) || !string.IsNullOrWhiteSpace(x.ReferrerHost))];
        MarketingAttributionEventRecord[] organicEvents = [.. events.Except(attributedEvents)];
        MarketingAttributionEventRecord[] visitEvents = [.. events.Where(static x => string.Equals(x.EventType, "page_landing", StringComparison.OrdinalIgnoreCase))];
        MarketingAttributionEventRecord[] signupEvents = [.. events.Where(static x => string.Equals(x.EventType, "signup_completed", StringComparison.OrdinalIgnoreCase))];
        MarketingAttributionEventRecord[] premiumEvents = [.. events.Where(static x => string.Equals(x.EventType, "premium_started", StringComparison.OrdinalIgnoreCase))];

        return Result.Success(new MarketingAttributionSummaryModel(
            normalizedWindowHours,
            nowUtc,
            events.Count,
            visitEvents.Length,
            signupEvents.Length,
            premiumEvents.Length,
            events.Select(static x => x.AnonymousId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            events.Select(static x => x.SessionId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            attributedEvents.Length,
            organicEvents.Length,
            CalculateRate(signupEvents.Length, visitEvents.Length),
            CalculateRate(premiumEvents.Length, signupEvents.Length),
            events.MaxBy(static x => x.OccurredAtUtc)?.OccurredAtUtc,
            BuildBreakdown(attributedEvents, includeCampaign: true, limit: 10),
            BuildBreakdown(attributedEvents, includeCampaign: false, limit: 10),
            [.. events
                .OrderByDescending(static x => x.OccurredAtUtc)
                .Take(25)
                .Select(static x => new MarketingAttributionRecentEventModel(
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

    private static IReadOnlyList<MarketingAttributionBreakdownModel> BuildBreakdown(
        IReadOnlyCollection<MarketingAttributionEventRecord> events,
        bool includeCampaign,
        int limit) {
        return [.. events
            .GroupBy(
                x => new AttributionKey(
                    NormalizeDimension(x.UtmSource ?? x.ReferrerHost, "direct"),
                    NormalizeDimension(x.UtmMedium, x.ReferrerHost is null ? "none" : "referral"),
                    includeCampaign ? NormalizeDimension(x.UtmCampaign, "none") : "all"),
                EqualityComparer<AttributionKey>.Default)
            .Select(static group => new MarketingAttributionBreakdownModel(
                group.Key.Source,
                group.Key.Medium,
                group.Key.Campaign,
                group.Count(),
                group.Count(static x => string.Equals(x.EventType, "page_landing", StringComparison.OrdinalIgnoreCase)),
                group.Count(static x => string.Equals(x.EventType, "signup_completed", StringComparison.OrdinalIgnoreCase)),
                group.Count(static x => string.Equals(x.EventType, "premium_started", StringComparison.OrdinalIgnoreCase)),
                group.Select(static x => x.AnonymousId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                group.Select(static x => x.SessionId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                CalculateRate(
                    group.Count(static x => string.Equals(x.EventType, "signup_completed", StringComparison.OrdinalIgnoreCase)),
                    group.Count(static x => string.Equals(x.EventType, "page_landing", StringComparison.OrdinalIgnoreCase))),
                CalculateRate(
                    group.Count(static x => string.Equals(x.EventType, "premium_started", StringComparison.OrdinalIgnoreCase)),
                    group.Count(static x => string.Equals(x.EventType, "signup_completed", StringComparison.OrdinalIgnoreCase))),
                group.Max(static x => x.OccurredAtUtc)))
            .OrderByDescending(static x => x.Events)
            .ThenBy(static x => x.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static x => x.Medium, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static x => x.Campaign, StringComparer.OrdinalIgnoreCase)
            .Take(limit)];
    }

    private static bool HasUtmData(MarketingAttributionEventRecord record) {
        return !string.IsNullOrWhiteSpace(record.UtmSource) ||
            !string.IsNullOrWhiteSpace(record.UtmMedium) ||
            !string.IsNullOrWhiteSpace(record.UtmCampaign) ||
            !string.IsNullOrWhiteSpace(record.UtmContent) ||
            !string.IsNullOrWhiteSpace(record.UtmTerm);
    }

    private static string NormalizeDimension(string? value, string fallback) {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static double CalculateRate(int numerator, int denominator) {
        return denominator > 0 ? Math.Round((double)numerator / denominator * 100, 1, MidpointRounding.ToEven) : 0;
    }

    private sealed record AttributionKey(string Source, string Medium, string Campaign);
}
