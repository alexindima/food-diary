using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableSyncReadRepository {
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

    Task<IReadOnlyList<WearableSyncEntryReadModel>> GetDailySummaryReadModelsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
