using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.WeightEntries.Common;

public interface IWeightEntryReadModelRepository {
    Task<IReadOnlyList<WeightEntryReadModel>> GetEntryReadModelsAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeightEntryReadModel>> GetByPeriodReadModelsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}