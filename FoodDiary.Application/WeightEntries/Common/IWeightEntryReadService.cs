using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Common;

public interface IWeightEntryReadService {
    Task<IReadOnlyList<WeightEntryModel>> GetEntriesAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken);

    Task<WeightEntryModel?> GetLatestAsync(UserId userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WeightEntrySummaryModel>> GetSummariesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        CancellationToken cancellationToken);
}
