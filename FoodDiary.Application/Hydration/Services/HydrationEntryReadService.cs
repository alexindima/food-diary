using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Hydration.Models;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Services;

internal sealed class HydrationEntryReadService(IHydrationEntryReadRepository hydrationEntryRepository) : IHydrationEntryReadService {
    public async Task<IReadOnlyList<HydrationEntryModel>> GetEntriesByDateAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken) {
        IReadOnlyList<HydrationEntryReadModel> entries = await hydrationEntryRepository
            .GetByDateReadModelsAsync(userId, dateUtc, cancellationToken)
            .ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];
    }

    public Task<int> GetDailyTotalAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken) =>
        hydrationEntryRepository.GetDailyTotalAsync(userId, dateUtc, cancellationToken);

    public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) =>
        hydrationEntryRepository.GetDailyTotalsAsync(userId, dateFrom, dateTo, cancellationToken);
}
