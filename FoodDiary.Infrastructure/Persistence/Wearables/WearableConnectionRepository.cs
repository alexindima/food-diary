using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Wearables;

internal sealed class WearableConnectionRepository(FoodDiaryDbContext context) : IWearableConnectionRepository {
    public async Task<WearableConnection?> GetAsync(
        UserId userId, WearableProvider provider, CancellationToken cancellationToken = default) {
        return await context.WearableConnections
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Provider == provider, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WearableConnection>> GetAllForUserAsync(
        UserId userId, CancellationToken cancellationToken = default) {
        return await context.WearableConnections
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Provider)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WearableConnectionModel>> GetConnectionModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.WearableConnections
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
        await context.WearableConnections.AddAsync(connection, cancellationToken).ConfigureAwait(false);
        return connection;
    }

    public async Task UpdateAsync(
        WearableConnection connection, CancellationToken cancellationToken = default) {
        context.WearableConnections.Update(connection);
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
