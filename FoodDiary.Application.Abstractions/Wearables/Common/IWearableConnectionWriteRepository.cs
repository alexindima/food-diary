using FoodDiary.Domain.Entities.Wearables;

namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableConnectionWriteRepository : IWearableConnectionReadRepository {
    Task<WearableConnection> AddAsync(
        WearableConnection connection,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        WearableConnection connection,
        CancellationToken cancellationToken = default);
}
