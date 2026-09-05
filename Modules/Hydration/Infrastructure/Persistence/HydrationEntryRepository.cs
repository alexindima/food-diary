using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Hydration.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Hydration.Infrastructure.Persistence;

public sealed class HydrationEntryRepository(DbSet<HydrationEntry> entries)
    : IHydrationEntryReadModelRepository, IHydrationEntryWriteRepository {
    public Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
        entries.Add(entry);
        return Task.FromResult(entry);
    }

    public Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
        entries.Remove(entry);
        return Task.CompletedTask;
    }

    public Task<HydrationEntry?> GetByIdForUpdateAsync(
        HydrationEntryId id,
        CancellationToken cancellationToken = default) =>
        entries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default) {
        DateTime dayStart = dateUtc.Date;
        DateTime dayEnd = TemporalRangePolicy.GetInclusiveDayEnd(dateUtc);

        return await entries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp <= dayEnd)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HydrationEntryReadModel>> GetByDateReadModelsAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default) {
        DateTime dayStart = dateUtc.Date;
        DateTime dayEnd = TemporalRangePolicy.GetInclusiveDayEnd(dateUtc);

        return await entries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp <= dayEnd)
            .OrderBy(x => x.Timestamp)
            .Select(x => new HydrationEntryReadModel(x.Id.Value, x.Timestamp, x.AmountMl))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) {
        DateTime dayStart = dateUtc.Date;
        DateTime dayEnd = TemporalRangePolicy.GetInclusiveDayEnd(dateUtc);

        return await entries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp <= dayEnd)
            .SumAsync(x => x.AmountMl, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        DateTime from = dateFrom.Date;
        DateTime to = TemporalRangePolicy.GetInclusiveDayEnd(dateTo);

        var results = await entries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= from && x.Timestamp <= to)
            .GroupBy(x => x.Timestamp.Date)
            .Select(g => new { Date = g.Key, TotalMl = g.Sum(x => x.AmountMl) })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return results.ConvertAll(r => (r.Date, r.TotalMl));
    }
}
