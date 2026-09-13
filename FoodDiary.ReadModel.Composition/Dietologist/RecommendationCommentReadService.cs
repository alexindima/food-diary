using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Dietologist;

internal sealed class RecommendationCommentReadService(FoodDiaryDbContext context) : IRecommendationCommentReadModelRepository {
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
