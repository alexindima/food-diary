using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Wearables.Infrastructure.Persistence;

internal sealed class WearableConnectionRepository(DbSet<WearableConnection> records) : IWearableConnectionRepository {
    public async Task<WearableConnection?> GetAsync(
        UserId userId, WearableProvider provider, CancellationToken cancellationToken = default) {
        return await records
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Provider == provider, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WearableConnection>> GetAllForUserAsync(
        UserId userId, CancellationToken cancellationToken = default) {
        return await records
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Provider)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WearableConnectionModel>> GetConnectionModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await records
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Provider)
            .Select(c => new WearableConnectionModel(
                c.Provider.ToString(),
                c.ExternalUserId,
                c.IsActive,
                c.LastSyncedAtUtc,
                c.CreatedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<WearableConnection> AddAsync(
        WearableConnection connection, CancellationToken cancellationToken = default) {
        await records.AddAsync(connection, cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public async Task UpdateAsync(
        WearableConnection connection, CancellationToken cancellationToken = default) {
        records.Update(connection);
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
