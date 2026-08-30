using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.WaistEntries.Common;

public interface IWaistEntryReadModelRepository {
    Task<IReadOnlyList<WaistEntryReadModel>> GetEntryReadModelsAsync(
        UserId userId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? limit,
        bool descending,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaistEntryReadModel>> GetByPeriodReadModelsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}
