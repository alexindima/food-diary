using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Dietologist;

internal sealed class RecommendationCommentReadService(ICompositionReadContext context) : IRecommendationCommentReadModelRepository {
    public Task<bool> IsParticipantAsync(RecommendationId recommendationId, UserId userId, CancellationToken cancellationToken = default) =>
        context.Recommendations.AsNoTracking().AnyAsync(
            recommendation => recommendation.Id == recommendationId &&
                (recommendation.ClientUserId == userId || recommendation.DietologistUserId == userId),
            cancellationToken);

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
