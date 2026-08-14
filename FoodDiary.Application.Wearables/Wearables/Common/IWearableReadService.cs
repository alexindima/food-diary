using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Wearables.Common;

internal interface IWearableReadService {
    Task<IReadOnlyList<WearableConnectionModel>> GetConnectionsAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<WearableDailySummaryModel> GetDailySummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken);
}
