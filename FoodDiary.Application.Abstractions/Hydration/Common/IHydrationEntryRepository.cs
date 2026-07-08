using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Hydration.Common;

public interface IHydrationEntryRepository : IHydrationEntryReadRepository, IHydrationEntryReadModelRepository, IHydrationEntryWriteRepository {
    new Task<int> GetDailyTotalAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default);

    new Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}
