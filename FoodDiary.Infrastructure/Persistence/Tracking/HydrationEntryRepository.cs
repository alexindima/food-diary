using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Hydration.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public sealed class HydrationEntryRepository(FoodDiaryDbContext context) : IHydrationEntryRepository {
    public Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
        context.HydrationEntries.Add(entry);
        return Task.FromResult(entry);
    }

    public Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
        context.HydrationEntries.Update(entry);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
        context.HydrationEntries.Remove(entry);
        return Task.CompletedTask;
    }

    public async Task<HydrationEntry?> GetByIdAsync(
        HydrationEntryId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<HydrationEntry> query = context.HydrationEntries.AsQueryable();
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default) {
        DateTime dayStart = dateUtc.Date;
        DateTime dayEnd = dayStart.AddDays(1);

        return await context.HydrationEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp < dayEnd)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HydrationEntryReadModel>> GetByDateReadModelsAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default) {
        DateTime dayStart = dateUtc.Date;
        DateTime dayEnd = dayStart.AddDays(1);

        return await context.HydrationEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp < dayEnd)
            .OrderBy(x => x.Timestamp)
            .Select(x => new HydrationEntryReadModel(x.Id.Value, x.Timestamp, x.AmountMl))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) {
        DateTime dayStart = dateUtc.Date;
        DateTime dayEnd = dayStart.AddDays(1);

        return await context.HydrationEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp < dayEnd)
            .SumAsync(x => x.AmountMl, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        DateTime from = dateFrom.Date;
        DateTime to = dateTo.Date.AddDays(1);

        var results = await context.HydrationEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= from && x.Timestamp < to)
            .GroupBy(x => x.Timestamp.Date)
            .Select(g => new { Date = g.Key, TotalMl = g.Sum(x => x.AmountMl) })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return results.ConvertAll(r => (r.Date, r.TotalMl));
    }
}
