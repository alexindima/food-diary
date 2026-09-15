using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Fasting.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

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
