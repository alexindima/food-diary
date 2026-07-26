using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationCommentWriteRepository {
    Task<RecommendationComment> AddAsync(
        RecommendationComment comment,
        CancellationToken cancellationToken = default);
}
