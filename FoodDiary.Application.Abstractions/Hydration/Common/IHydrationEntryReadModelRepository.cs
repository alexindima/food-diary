using FoodDiary.Application.Abstractions.Hydration.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Hydration.Common;

public interface IHydrationEntryReadModelRepository {
    Task<IReadOnlyList<HydrationEntryReadModel>> GetByDateReadModelsAsync(
        UserId userId,
        DateTime dateUtc,
        CancellationToken cancellationToken = default);
}