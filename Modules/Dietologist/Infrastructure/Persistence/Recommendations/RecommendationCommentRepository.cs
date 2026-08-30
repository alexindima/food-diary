using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recommendations;

internal sealed class RecommendationCommentRepository(FoodDiaryDbContext context) : IRecommendationCommentRepository {
    public async Task<RecommendationComment> AddAsync(
        RecommendationComment comment,
        CancellationToken cancellationToken = default) {
        await context.RecommendationComments.AddAsync(comment, cancellationToken).ConfigureAwait(false);
        return comment;
    }

    public async Task<IReadOnlyList<RecommendationCommentReadModel>> GetByRecommendationAsync(
        RecommendationId recommendationId,
        CancellationToken cancellationToken = default) {
        return await context.RecommendationComments
            .AsNoTracking()
            .Where(comment => comment.RecommendationId == recommendationId)
            .OrderBy(comment => comment.CreatedOnUtc)
            .Select(comment => new RecommendationCommentReadModel(
                comment.Id.Value,
                comment.RecommendationId.Value,
                comment.AuthorUserId.Value,
                comment.AuthorUser.FirstName,
                comment.AuthorUser.LastName,
                comment.AuthorUser.Email,
                comment.Text,
                comment.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
