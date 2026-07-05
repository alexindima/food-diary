using FoodDiary.Application.Abstractions.Hydration.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Hydration.Common;

public interface IHydrationEntryReadRepository {
    Task<HydrationEntry?> GetByIdAsync(
        HydrationEntryId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<HydrationEntryReadModel>> GetByDateReadModelsAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<HydrationEntry> entries = await GetByDateAsync(userId, dateUtc, cancellationToken).ConfigureAwait(false);
        return [.. entries.Select(static entry => new HydrationEntryReadModel(entry.Id.Value, entry.Timestamp, entry.AmountMl))];
    }

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
