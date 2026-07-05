using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Services;

internal sealed class HydrationEntryReadService(IHydrationEntryReadRepository hydrationEntryRepository) : IHydrationEntryReadService {
    public async Task<IReadOnlyList<HydrationEntryModel>> GetEntriesByDateAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken) {
        IReadOnlyList<HydrationEntry> entries = await hydrationEntryRepository
            .GetByDateAsync(userId, dateUtc, cancellationToken)
            .ConfigureAwait(false);

        return [.. entries.Select(entry => entry.ToModel())];
    }

    public Task<int> GetDailyTotalAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken) =>
        hydrationEntryRepository.GetDailyTotalAsync(userId, dateUtc, cancellationToken);
}
