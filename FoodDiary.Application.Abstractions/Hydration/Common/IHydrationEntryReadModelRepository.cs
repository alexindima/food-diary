using FoodDiary.Application.Abstractions.Hydration.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Hydration.Common;

public interface IHydrationEntryReadModelRepository {
    Task<IReadOnlyList<HydrationEntryReadModel>> GetByDateReadModelsAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default);

    Task<int> GetDailyTotalAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}
