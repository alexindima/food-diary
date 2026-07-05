using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Common;

public interface IHydrationEntryReadService {
    Task<IReadOnlyList<HydrationEntryModel>> GetEntriesByDateAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken);

    Task<int> GetDailyTotalAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken);
}
