using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Services;

internal sealed class WearableReadService(
    IWearableConnectionReadRepository connectionRepository,
    IWearableSyncReadRepository syncRepository) : IWearableReadService {
    public async Task<IReadOnlyList<WearableConnectionModel>> GetConnectionsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        IReadOnlyList<WearableConnection> connections = await connectionRepository
            .GetAllForUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return [.. connections.Select(connection => new WearableConnectionModel(
            connection.Provider.ToString(),
            connection.ExternalUserId,
            connection.IsActive,
            connection.LastSyncedAtUtc,
            connection.CreatedOnUtc))];
    }

    public async Task<WearableDailySummaryModel> GetDailySummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken) {
        IReadOnlyList<WearableSyncEntry> entries = await syncRepository
            .GetDailySummaryAsync(userId, date, cancellationToken)
            .ConfigureAwait(false);

        return SyncWearableDataCommandHandler.MapToSummary(date, entries);
    }
}
