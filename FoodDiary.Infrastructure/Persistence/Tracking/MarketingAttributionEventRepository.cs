using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public sealed class MarketingAttributionEventRepository(FoodDiaryDbContext context) : IMarketingAttributionEventRepository {
    public Task AddAsync(MarketingAttributionEventRecord record, CancellationToken cancellationToken = default) {
        var entity = MarketingAttributionEvent.Create(
            record.EventType,
            record.OccurredAtUtc,
            record.UserId,
            record.AnonymousId,
            record.SessionId,
            record.LandingPath,
            record.ReferrerHost,
            record.UtmSource,
            record.UtmMedium,
            record.UtmCampaign,
            record.UtmContent,
            record.UtmTerm,
            record.BuildVersion,
            record.EventId);

        context.MarketingAttributionEvents.Add(entity);
        return Task.CompletedTask;
    }

    public async Task<int> DeleteOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default) {
        MarketingAttributionEventId[] ids = await context.MarketingAttributionEvents
            .AsNoTracking()
            .Where(item => item.OccurredAtUtc < olderThanUtc)
            .OrderBy(item => item.OccurredAtUtc)
            .Select(item => item.Id)
            .Take(Math.Max(batchSize, 1))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        if (ids.Length == 0) {
            return 0;
        }

        return await context.MarketingAttributionEvents
            .Where(item => Enumerable.Contains(ids, item.Id))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<MarketingAttributionSummaryRecord> GetSummaryAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) {
        IQueryable<MarketingAttributionEvent> events = context.MarketingAttributionEvents
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= sinceUtc);

        MarketingAttributionCounts counts = await events
            .GroupBy(static _ => 1)
            .Select(group => new MarketingAttributionCounts(
                group.Count(),
                group.Count(x => x.EventType == "page_landing"),
                group.Count(x => x.EventType == "signup_completed"),
                group.Count(x => x.EventType == "premium_started"),
                group.Count(x => x.UtmSource != null || x.UtmMedium != null || x.UtmCampaign != null || x.UtmContent != null || x.UtmTerm != null || x.ReferrerHost != null),
                group.Count(x => x.EventType == "page_landing" && (x.UtmSource != null || x.UtmMedium != null || x.UtmCampaign != null || x.UtmContent != null || x.UtmTerm != null || x.ReferrerHost != null)),
                group.Max(x => (DateTime?)x.OccurredAtUtc)))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? new(
                Events: 0, Visits: 0, Signups: 0, PremiumStarts: 0,
                AttributedEvents: 0, AttributedVisits: 0, LastEventAtUtc: null);

        int anonymousVisitors = await events.Select(x => x.AnonymousId).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
        int sessions = await events.Select(x => x.SessionId).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MarketingAttributionBreakdownRecord> topCampaigns = await LoadBreakdownAsync(events, includeCampaign: true, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MarketingAttributionBreakdownRecord> topSources = await LoadBreakdownAsync(events, includeCampaign: false, cancellationToken).ConfigureAwait(false);
        List<MarketingAttributionEventRecord> recentEvents = await LoadRecentEventsAsync(events, cancellationToken).ConfigureAwait(false);

        return new MarketingAttributionSummaryRecord(
            counts.Events,
            counts.Visits,
            counts.Signups,
            counts.PremiumStarts,
            anonymousVisitors,
            sessions,
            counts.AttributedEvents,
            counts.AttributedVisits,
            counts.LastEventAtUtc,
            topCampaigns,
            topSources,
            recentEvents);
    }

    public async Task<MarketingAttributionEventRecord?> GetLandingAsync(
        string anonymousId,
        string sessionId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default) {
        return await context.MarketingAttributionEvents
            .AsNoTracking()
            .Where(x => x.EventType == "page_landing" &&
                x.AnonymousId == anonymousId &&
                x.SessionId == sessionId &&
                x.OccurredAtUtc >= sinceUtc)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new MarketingAttributionEventRecord(
                x.EventType, x.OccurredAtUtc, x.UserId, x.AnonymousId, x.SessionId, x.LandingPath,
                x.ReferrerHost, x.UtmSource, x.UtmMedium, x.UtmCampaign, x.UtmContent, x.UtmTerm, x.BuildVersion, x.Id.Value))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<MarketingAttributionEventRecord?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default) {
        return await context.MarketingAttributionEvents
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new MarketingAttributionEventRecord(
                x.EventType,
                x.OccurredAtUtc,
                x.UserId,
                x.AnonymousId,
                x.SessionId,
                x.LandingPath,
                x.ReferrerHost,
                x.UtmSource,
                x.UtmMedium,
                x.UtmCampaign,
                x.UtmContent,
                x.UtmTerm,
                x.BuildVersion,
                x.Id.Value))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsForUserAsync(Guid userId, string eventType, CancellationToken cancellationToken = default) {
        return context.MarketingAttributionEvents
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == userId &&
                x.EventType == eventType,
                cancellationToken);
    }

    private static async Task<IReadOnlyList<MarketingAttributionBreakdownRecord>> LoadBreakdownAsync(
        IQueryable<MarketingAttributionEvent> events,
        bool includeCampaign,
        CancellationToken cancellationToken) {
        if (includeCampaign) {
            events = events.Where(x => x.UtmCampaign != null);
        }

        List<MarketingAttributionBreakdownAggregate> aggregates = await events
            .GroupBy(x => new {
                Source = x.UtmSource ?? x.ReferrerHost ?? "direct",
                Medium = x.UtmMedium ?? (x.ReferrerHost == null ? "none" : "referral"),
                Campaign = includeCampaign ? x.UtmCampaign ?? "none" : "all",
            })
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.Source)
            .ThenBy(group => group.Key.Medium)
            .ThenBy(group => group.Key.Campaign)
            .Take(10)
            .Select(group => new MarketingAttributionBreakdownAggregate(
                group.Key.Source,
                group.Key.Medium,
                group.Key.Campaign,
                group.Count(),
                group.Count(x => x.EventType == "page_landing"),
                group.Count(x => x.EventType == "signup_completed"),
                group.Count(x => x.EventType == "premium_started"),
                group.Max(x => (DateTime?)x.OccurredAtUtc)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        List<MarketingAttributionBreakdownRecord> result = [];
        foreach (MarketingAttributionBreakdownAggregate aggregate in aggregates) {
            IQueryable<MarketingAttributionEvent> matching = events.Where(x =>
                (x.UtmSource ?? x.ReferrerHost ?? "direct") == aggregate.Source &&
                (x.UtmMedium ?? (x.ReferrerHost == null ? "none" : "referral")) == aggregate.Medium &&
                (!includeCampaign || (x.UtmCampaign ?? "none") == aggregate.Campaign));
            int visitors = await matching.Select(x => x.AnonymousId).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
            int sessions = await matching.Select(x => x.SessionId).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
            result.Add(new MarketingAttributionBreakdownRecord(
                aggregate.Source, aggregate.Medium, aggregate.Campaign, aggregate.Events, aggregate.Visits,
                aggregate.Signups, aggregate.PremiumStarts, visitors, sessions, aggregate.LastEventAtUtc));
        }

        return result;
    }

    private static Task<List<MarketingAttributionEventRecord>> LoadRecentEventsAsync(
        IQueryable<MarketingAttributionEvent> events,
        CancellationToken cancellationToken) {
        return events
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(50)
            .Select(x => new MarketingAttributionEventRecord(
                x.EventType, x.OccurredAtUtc, x.UserId, x.AnonymousId, x.SessionId, x.LandingPath,
                x.ReferrerHost, x.UtmSource, x.UtmMedium, x.UtmCampaign, x.UtmContent, x.UtmTerm, x.BuildVersion, x.Id.Value))
            .ToListAsync(cancellationToken);
    }

    private sealed record MarketingAttributionCounts(
        int Events,
        int Visits,
        int Signups,
        int PremiumStarts,
        int AttributedEvents,
        int AttributedVisits,
        DateTime? LastEventAtUtc);

    private sealed record MarketingAttributionBreakdownAggregate(
        string Source,
        string Medium,
        string Campaign,
        int Events,
        int Visits,
        int Signups,
        int PremiumStarts,
        DateTime? LastEventAtUtc);
}
