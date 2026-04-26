using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableSyncRepository {
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
