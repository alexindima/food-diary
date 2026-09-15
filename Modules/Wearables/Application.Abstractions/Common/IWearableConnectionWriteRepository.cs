using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Common;

public interface IWearableConnectionWriteRepository {
    Task<WearableConnection?> GetAsync(
        UserId userId,
        WearableProvider provider,
        CancellationToken cancellationToken = default);

    Task<WearableConnection> AddAsync(
        WearableConnection connection,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        WearableConnection connection,
        CancellationToken cancellationToken = default);
}
