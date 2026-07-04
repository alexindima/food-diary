using FoodDiary.Domain.Entities.Wearables;

namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableSyncWriteRepository : IWearableSyncReadRepository {
    Task<WearableSyncEntry> AddAsync(
        WearableSyncEntry entry,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        WearableSyncEntry entry,
        CancellationToken cancellationToken = default);
}
