using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationReadModelRepository {
    Task<IReadOnlyList<RecommendationReadModel>> GetByClientReadModelsAsync(
        UserId clientUserId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendationReadModel>> GetByDietologistAndClientReadModelsAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
