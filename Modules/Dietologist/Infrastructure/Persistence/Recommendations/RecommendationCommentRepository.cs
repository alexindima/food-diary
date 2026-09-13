using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Recommendations;

internal sealed class RecommendationCommentRepository(DbSet<RecommendationComment> records, IRecommendationCommentReadModelRepository readModels) : IRecommendationCommentRepository {
    public async Task<RecommendationComment> AddAsync(
        RecommendationComment comment,
        CancellationToken cancellationToken = default) {
        await records.AddAsync(comment, cancellationToken).ConfigureAwait(false);
        return comment;
    }

    public Task<IReadOnlyList<RecommendationCommentReadModel>> GetByRecommendationAsync(
        RecommendationId recommendationId,
        CancellationToken cancellationToken = default) =>
        readModels.GetByRecommendationAsync(recommendationId, cancellationToken);
}
