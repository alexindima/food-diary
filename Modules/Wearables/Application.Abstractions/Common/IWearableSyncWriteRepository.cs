using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Common;

public interface IWearableSyncWriteRepository {
    Task<WearableSyncEntry?> GetAsync(
        UserId userId,
        WearableProvider provider,
        WearableDataType dataType,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WearableSyncEntry>> GetDailySummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
    Task<WearableSyncEntry> AddAsync(
        WearableSyncEntry entry,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        WearableSyncEntry entry,
        CancellationToken cancellationToken = default);
}
