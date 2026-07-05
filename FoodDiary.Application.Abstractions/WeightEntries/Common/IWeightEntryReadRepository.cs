using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.WeightEntries.Common;

public interface IWeightEntryReadRepository {
    Task<WeightEntry?> GetByIdAsync(
        WeightEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<WeightEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeightEntry>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<WeightEntryReadModel>> GetEntryReadModelsAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<WeightEntry> entries = await GetEntriesAsync(
            userId,
            dateFrom,
            dateTo,
            limit,
            descending,
            cancellationToken).ConfigureAwait(false);

        return [.. entries.Select(static entry => new WeightEntryReadModel(entry.Date, entry.Weight))];
    }

    Task<IReadOnlyList<WeightEntry>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<WeightEntryReadModel>> GetByPeriodReadModelsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<WeightEntry> entries = await GetByPeriodAsync(
            userId,
            dateFrom,
            dateTo,
            cancellationToken).ConfigureAwait(false);

        return [.. entries.Select(static entry => new WeightEntryReadModel(entry.Date, entry.Weight))];
    }
}
