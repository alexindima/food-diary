using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Common;

public interface IWaistEntryReadService {
    Task<IReadOnlyList<WaistEntryModel>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken);

    Task<WaistEntryModel?> GetLatestAsync(UserId userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WaistEntrySummaryModel>> GetSummariesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        CancellationToken cancellationToken);
}
