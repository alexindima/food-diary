using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Common;

public interface IRecommendationDiscussionReadService {
    Task<Result<IReadOnlyList<RecommendationCommentModel>>> GetAsync(
        UserId userId,
        Guid recommendationId,
        CancellationToken cancellationToken);
}
