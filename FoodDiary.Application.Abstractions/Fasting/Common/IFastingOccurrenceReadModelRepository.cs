using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingOccurrenceReadModelRepository {
    Task<FastingOccurrenceReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FastingOccurrenceReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<FastingOccurrenceReadModel> Items, int TotalItems)> GetPagedByUserReadModelsAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? from = null,
        DateTime? to = null,
        FastingOccurrenceStatus? status = null,
        CancellationToken cancellationToken = default);
}