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
            record.BuildVersion);

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
            .Where(item => ids.Contains(item.Id))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MarketingAttributionEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) {
        return await context.MarketingAttributionEvents
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= sinceUtc)
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
                x.BuildVersion))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
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
                x.BuildVersion))
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
}
