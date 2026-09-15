using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;

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
