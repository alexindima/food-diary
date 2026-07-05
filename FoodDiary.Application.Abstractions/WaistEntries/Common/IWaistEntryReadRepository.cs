using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.WaistEntries.Common;

public interface IWaistEntryReadRepository {
    Task<WaistEntry?> GetByIdAsync(
        WaistEntryId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<WaistEntry?> GetByDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<WaistEntryReadModel>> GetEntryReadModelsAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<WaistEntry> entries = await GetEntriesAsync(
            userId,
            dateFrom,
            dateTo,
            limit,
            descending,
            cancellationToken).ConfigureAwait(false);

        return [.. entries.Select(static entry => new WaistEntryReadModel(entry.Date, entry.Circumference))];
    }

    Task<IReadOnlyList<WaistEntry>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<WaistEntryReadModel>> GetByPeriodReadModelsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<WaistEntry> entries = await GetByPeriodAsync(
            userId,
            dateFrom,
            dateTo,
            cancellationToken).ConfigureAwait(false);

        return [.. entries.Select(static entry => new WaistEntryReadModel(entry.Date, entry.Circumference))];
    }
}
