using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class WaistEntryRepository(FoodDiaryDbContext context) : IWaistEntryRepository {
    public async Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default) {
        await context.WaistEntries.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task UpdateAsync(WaistEntry entry, CancellationToken cancellationToken = default) {
        context.WaistEntries.Update(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WaistEntry entry, CancellationToken cancellationToken = default) {
        context.WaistEntries.Remove(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WaistEntry?> GetByIdAsync(
        WaistEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        var query = asTracking
            ? context.WaistEntries.AsQueryable()
            : context.WaistEntries.AsNoTracking();

        return await query.FirstOrDefaultAsync(
            entry => entry.Id == id && entry.UserId == userId,
            cancellationToken);
    }

    public async Task<WaistEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        var normalizedDate = date.Date;
        return await context.WaistEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.UserId == userId && entry.Date == normalizedDate,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default) {
        var query = context.WaistEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId);

        if (dateFrom.HasValue) {
            var from = dateFrom.Value.Date;
            query = query.Where(entry => entry.Date >= from);
        }

        if (dateTo.HasValue) {
            var to = dateTo.Value.Date;
            query = query.Where(entry => entry.Date <= to);
        }

        query = descending
            ? query.OrderByDescending(entry => entry.Date).ThenByDescending(entry => entry.CreatedOnUtc)
            : query.OrderBy(entry => entry.Date).ThenBy(entry => entry.CreatedOnUtc);

        if (limit.HasValue && limit.Value > 0) {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
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
            .ToListAsync(cancellationToken);
    }
}
