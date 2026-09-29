using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Dietologist;

internal sealed class RecommendationCommentReadService(ICompositionReadContext context) : IRecommendationCommentReadModelRepository {
    public async Task<(IReadOnlyList<RecommendationCommentReadModel> Items, int Total)> GetPageByRecommendationAsync(
        RecommendationId recommendationId, int page, int limit, CancellationToken cancellationToken = default) {
        int normalizedPage = Math.Clamp(page, 1, 10_000);
        int normalizedLimit = Math.Clamp(limit, 1, 100);
        IQueryable<RecommendationComment> query = context.RecommendationComments.AsNoTracking()
            .Where(comment => comment.RecommendationId == recommendationId);
        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<RecommendationCommentReadModel> items = await query
            .OrderBy(comment => comment.CreatedOnUtc)
            .ThenBy(comment => comment.Id)
            .Skip((normalizedPage - 1) * normalizedLimit)
            .Take(normalizedLimit)
            .Join(context.Users.AsNoTracking(), comment => comment.AuthorUserId, user => user.Id, (comment, user) => new RecommendationCommentReadModel(
                comment.Id.Value, comment.RecommendationId.Value, comment.AuthorUserId.Value,
                user.FirstName, user.LastName, user.Email, comment.Text, comment.CreatedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return (items, total);
    }

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
