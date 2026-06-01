using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Wearables;

internal sealed class WearableSyncRepository(FoodDiaryDbContext context) : IWearableSyncRepository {
    public async Task<WearableSyncEntry?> GetAsync(
        UserId userId, WearableProvider provider, WearableDataType dataType,
        DateTime date, CancellationToken cancellationToken = default) {
        return await context.WearableSyncEntries
            .FirstOrDefaultAsync(e =>
                e.UserId == userId && e.Provider == provider &&
                e.DataType == dataType && e.Date == date.Date,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WearableSyncEntry>> GetDailySummaryAsync(
        UserId userId, DateTime date, CancellationToken cancellationToken = default) {
        return await context.WearableSyncEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date == date.Date)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<WearableSyncEntry> AddAsync(
        WearableSyncEntry entry, CancellationToken cancellationToken = default) {
        await context.WearableSyncEntries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry;
    }

    public async Task UpdateAsync(
        WearableSyncEntry entry, CancellationToken cancellationToken = default) {
        context.WearableSyncEntries.Update(entry);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
