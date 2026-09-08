using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Domain.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Marketing.Infrastructure.Persistence;

public sealed partial class MarketingAttributionEventRepository {
    public async Task<MarketingAttributionRangeRecord> GetRangeAsync(MarketingAttributionRangeFilter filter, CancellationToken cancellationToken) {
        IQueryable<MarketingAttributionEvent> events = context.MarketingAttributionEvents.AsNoTracking()
            .Where(item => item.OccurredAtUtc >= filter.FromUtc && item.OccurredAtUtc < filter.ToUtc);
        DateTime previousFrom = filter.FromUtc - (filter.ToUtc - filter.FromUtc);
        MarketingAttributionSummaryRecord current = await LoadSummaryAsync(events, cancellationToken).ConfigureAwait(false);
        MarketingAttributionSummaryRecord previous = await LoadSummaryAsync(context.MarketingAttributionEvents.AsNoTracking()
            .Where(item => item.OccurredAtUtc >= previousFrom && item.OccurredAtUtc < filter.FromUtc), cancellationToken).ConfigureAwait(false);
        List<MarketingAttributionDayRecord> byDay = await events.GroupBy(item => item.OccurredAtUtc.Date).OrderBy(group => group.Key)
            .Select(group => new MarketingAttributionDayRecord(group.Key, group.Count(item => item.EventType == "page_landing"),
                group.Count(item => item.EventType == "signup_completed"), group.Count(item => item.EventType == "premium_started")))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<MarketingAttributionEvent> journal = FilterJournal(events, filter);
        int count = await journal.CountAsync(cancellationToken).ConfigureAwait(false);
        List<MarketingAttributionEventRecord> items = await journal.OrderByDescending(item => item.OccurredAtUtc).ThenByDescending(item => item.Id)
            .Skip((filter.Page - 1) * filter.Limit).Take(filter.Limit)
            .Select(item => new MarketingAttributionEventRecord(item.EventType, item.OccurredAtUtc, item.UserId, item.AnonymousId,
                item.SessionId, item.LandingPath, item.ReferrerHost, item.UtmSource, item.UtmMedium, item.UtmCampaign,
                item.UtmContent, item.UtmTerm, item.BuildVersion, item.Id.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new MarketingAttributionRangeRecord(current with { RecentEvents = items }, previous, byDay, count);
    }

    private static IQueryable<MarketingAttributionEvent> FilterJournal(IQueryable<MarketingAttributionEvent> events, MarketingAttributionRangeFilter filter) {
        if (!string.IsNullOrWhiteSpace(filter.EventType)) { events = events.Where(item => item.EventType == filter.EventType); }
        if (string.Equals(filter.Channel, "tracked", StringComparison.Ordinal)) {
            events = events.Where(item => item.UtmSource != null || item.UtmMedium != null || item.UtmCampaign != null || item.UtmContent != null || item.UtmTerm != null || item.ReferrerHost != null);
        } else if (string.Equals(filter.Channel, "direct", StringComparison.Ordinal)) {
            events = events.Where(item => item.UtmSource == null && item.UtmMedium == null && item.UtmCampaign == null && item.UtmContent == null && item.UtmTerm == null && item.ReferrerHost == null);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            string pattern = "%" + filter.Search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
            events = events.Where(item => EF.Functions.ILike(item.LandingPath, pattern, "\\") || EF.Functions.ILike(item.UtmSource ?? "", pattern, "\\") ||
                EF.Functions.ILike(item.UtmCampaign ?? "", pattern, "\\") || EF.Functions.ILike(item.ReferrerHost ?? "", pattern, "\\"));
        }
        return events;
    }
}
