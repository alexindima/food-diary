using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationCommentReadModelRepository {
    Task<IReadOnlyList<RecommendationCommentReadModel>> GetByRecommendationAsync(
        RecommendationId recommendationId,
        CancellationToken cancellationToken = default);
}
