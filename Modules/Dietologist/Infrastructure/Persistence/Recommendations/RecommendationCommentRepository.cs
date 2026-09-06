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
            .Join(context.Users.AsNoTracking(), comment => comment.AuthorUserId, user => user.Id, (comment, user) => new RecommendationCommentReadModel(
                comment.Id.Value,
                comment.RecommendationId.Value,
                comment.AuthorUserId.Value,
                user.FirstName,
                user.LastName,
                user.Email,
                comment.Text,
                comment.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
