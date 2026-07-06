using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Wearables.Common;

public interface IWearableSyncReadModelRepository {
    Task<IReadOnlyList<WearableSyncEntryReadModel>> GetDailySummaryReadModelsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
}