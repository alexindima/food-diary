using FoodDiary.Application.Hydration.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class HydrationEntryRepository(FoodDiaryDbContext context) : IHydrationEntryRepository {
    public async Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
        context.HydrationEntries.Add(entry);
        return entry;
    }

    public async Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
        context.HydrationEntries.Update(entry);
    }

    public async Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
        context.HydrationEntries.Remove(entry);
    }

    public async Task<HydrationEntry?> GetByIdAsync(
        HydrationEntryId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        var query = context.HydrationEntries.AsQueryable();
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default) {
        var dayStart = dateUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        return await context.HydrationEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp < dayEnd)
            .OrderBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) {
        var dayStart = dateUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        return await context.HydrationEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Timestamp >= dayStart && x.Timestamp < dayEnd)
            .SumAsync(x => x.AmountMl, cancellationToken);
    }
}
