using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableConnectionReadRepository {
    Task<WearableConnection?> GetAsync(
        UserId userId,
        WearableProvider provider,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WearableConnection>> GetAllForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<WearableConnectionModel>> GetConnectionModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<WearableConnection> connections = await GetAllForUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. connections.Select(static connection => new WearableConnectionModel(
            connection.Provider.ToString(),
            connection.ExternalUserId,
            connection.IsActive,
            connection.LastSyncedAtUtc,
            connection.CreatedOnUtc))];
    }
}
