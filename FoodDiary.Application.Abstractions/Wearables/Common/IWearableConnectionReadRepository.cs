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
}
