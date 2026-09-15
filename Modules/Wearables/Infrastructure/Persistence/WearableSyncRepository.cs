using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Wearables.Infrastructure.Persistence;

internal sealed class WearableSyncRepository(DbSet<WearableSyncEntry> records) : IWearableSyncRepository {
    public async Task<WearableSyncEntry?> GetAsync(
        UserId userId, WearableProvider provider, WearableDataType dataType,
        DateTime date, CancellationToken cancellationToken = default) {
        return await records
            .FirstOrDefaultAsync(e =>
                e.UserId == userId && e.Provider == provider &&
                e.DataType == dataType && e.Date == date.Date,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WearableSyncEntry>> GetDailySummaryAsync(
        UserId userId, DateTime date, CancellationToken cancellationToken = default) {
        return await records
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date == date.Date)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WearableSyncEntryReadModel>> GetDailySummaryReadModelsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        return await records
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date == date.Date)
            .Select(e => new WearableSyncEntryReadModel(e.DataType, e.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<WearableSyncEntry> AddAsync(
        WearableSyncEntry entry, CancellationToken cancellationToken = default) {
        await records.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        return entry;
    }

    public async Task UpdateAsync(
        WearableSyncEntry entry, CancellationToken cancellationToken = default) {
        records.Update(entry);
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
