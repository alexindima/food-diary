using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationCommentReadModelRepository {
    Task<bool> IsParticipantAsync(
        RecommendationId recommendationId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendationCommentReadModel>> GetByRecommendationAsync(
        RecommendationId recommendationId,
        CancellationToken cancellationToken = default);
}
