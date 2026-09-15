using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Infrastructure.Persistence.Recommendations;

internal sealed class RecommendationCommentRepository(DbSet<RecommendationComment> records, IRecommendationCommentReadModelRepository readModels) : IRecommendationCommentRepository {
    public Task<bool> IsParticipantAsync(RecommendationId recommendationId, UserId userId, CancellationToken cancellationToken = default) =>
        readModels.IsParticipantAsync(recommendationId, userId, cancellationToken);

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
