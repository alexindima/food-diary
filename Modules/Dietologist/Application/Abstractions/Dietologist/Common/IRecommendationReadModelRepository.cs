using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

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
