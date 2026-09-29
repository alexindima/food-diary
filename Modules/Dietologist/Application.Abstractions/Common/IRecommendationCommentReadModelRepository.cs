using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationCommentReadModelRepository {
    Task<bool> IsParticipantAsync(
        RecommendationId recommendationId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendationCommentReadModel>> GetByRecommendationAsync(
        RecommendationId recommendationId,
        CancellationToken cancellationToken = default);

    async Task<(IReadOnlyList<RecommendationCommentReadModel> Items, int Total)> GetPageByRecommendationAsync(
        RecommendationId recommendationId, int page, int limit, CancellationToken cancellationToken = default) {
        IReadOnlyList<RecommendationCommentReadModel> all = await GetByRecommendationAsync(recommendationId, cancellationToken).ConfigureAwait(false);
        return (all.Skip((page - 1) * limit).Take(limit).ToArray(), all.Count);
    }
}
