using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationCommentWriteRepository {
    Task<RecommendationComment> AddAsync(
        RecommendationComment comment,
        CancellationToken cancellationToken = default);
}
