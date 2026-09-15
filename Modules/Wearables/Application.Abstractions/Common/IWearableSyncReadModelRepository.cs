using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Common;

public interface IWearableSyncReadModelRepository {
    Task<IReadOnlyList<WearableSyncEntryReadModel>> GetDailySummaryReadModelsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
