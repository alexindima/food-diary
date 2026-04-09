using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Common;

public interface IWearableConnectionRepository {
    Task<WearableConnection?> GetAsync(
        UserId userId,
        WearableProvider provider,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WearableConnection>> GetAllForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<WearableConnection> AddAsync(
        WearableConnection connection,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        WearableConnection connection,
        CancellationToken cancellationToken = default);
}
