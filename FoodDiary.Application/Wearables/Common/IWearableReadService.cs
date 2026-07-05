using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Common;

public interface IWearableReadService {
    Task<IReadOnlyList<WearableConnectionModel>> GetConnectionsAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<WearableDailySummaryModel> GetDailySummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken);
}
