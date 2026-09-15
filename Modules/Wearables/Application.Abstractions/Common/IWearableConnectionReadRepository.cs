using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Common;

public interface IWearableConnectionReadRepository {
    Task<WearableConnection?> GetAsync(
        UserId userId,
        WearableProvider provider,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WearableConnection>> GetAllForUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WearableConnectionModel>> GetConnectionModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
