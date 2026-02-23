using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence;

public class WeightEntryRepository(FoodDiaryDbContext context) : IWeightEntryRepository {
    public async Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default) {
        await context.WeightEntries.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default) {
        context.WeightEntries.Update(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WeightEntry entry, CancellationToken cancellationToken = default) {
        context.WeightEntries.Remove(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WeightEntry?> GetByIdAsync(
        WeightEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        var query = asTracking
            ? context.WeightEntries.AsQueryable()
            : context.WeightEntries.AsNoTracking();

        return await query.FirstOrDefaultAsync(
            entry => entry.Id == id && entry.UserId == userId,
            cancellationToken);
    }

    public async Task<WeightEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        var normalizedDate = date.Date;
        return await context.WeightEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.UserId == userId && entry.Date == normalizedDate,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WeightEntry>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default) {
        var query = context.WeightEntries
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

    public async Task<IReadOnlyList<WeightEntry>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        var from = DateTime.SpecifyKind(dateFrom, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(dateTo, DateTimeKind.Utc);

        return await context.WeightEntries
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Date >= from && entry.Date <= to)
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.CreatedOnUtc)
            .ToListAsync(cancellationToken);
    }
}
