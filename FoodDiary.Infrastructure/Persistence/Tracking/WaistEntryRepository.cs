using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class WaistEntryRepository(FoodDiaryDbContext context) : IWaistEntryRepository {
    public async Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default) {
        await context.WaistEntries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        return entry;
    }

    public Task UpdateAsync(WaistEntry entry, CancellationToken cancellationToken = default) {
        context.WaistEntries.Update(entry);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WaistEntry entry, CancellationToken cancellationToken = default) {
        context.WaistEntries.Remove(entry);
        return Task.CompletedTask;
    }

    public async Task<WaistEntry?> GetByIdAsync(
        WaistEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<WaistEntry> query = asTracking
            ? context.WaistEntries.AsQueryable()
            : context.WaistEntries.AsNoTracking();

        return await query.FirstOrDefaultAsync(
            entry => entry.Id == id && entry.UserId == userId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<WaistEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        DateTime normalizedDate = date.Date;
        return await context.WaistEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.UserId == userId && entry.Date == normalizedDate,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default) {
        IQueryable<WaistEntry> query = context.WaistEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId);

        if (dateFrom.HasValue) {
            DateTime from = dateFrom.Value.Date;
            query = query.Where(entry => entry.Date >= from);
        }

        if (dateTo.HasValue) {
            DateTime to = dateTo.Value.Date;
            query = query.Where(entry => entry.Date <= to);
        }

        query = descending
            ? query.OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedOnUtc)
            : query.OrderBy(entry => entry.Date).ThenBy(entry => entry.CreatedOnUtc);

        if (limit > 0) {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WaistEntry>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        var from = DateTime.SpecifyKind(dateFrom, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(dateTo, DateTimeKind.Utc);

        return await context.WaistEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Date >= from && entry.Date <= to)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.CreatedOnUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
