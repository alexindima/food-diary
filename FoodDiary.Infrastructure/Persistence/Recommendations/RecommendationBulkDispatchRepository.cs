using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Recommendations;

internal sealed class RecommendationBulkDispatchRepository(FoodDiaryDbContext context)
    : IRecommendationBulkDispatchRepository {
    public async Task<IReadOnlyList<RecommendationBulkDispatchReadModel>> GetExistingAsync(
        UserId dietologistUserId,
        string idempotencyKey,
        IReadOnlyCollection<UserId> clientUserIds,
        CancellationToken cancellationToken = default) {
        UserId[] clientIds = [.. clientUserIds];
        return await context.RecommendationBulkDispatches
            .AsNoTracking()
            .Where(dispatch =>
                dispatch.DietologistUserId == dietologistUserId &&
                dispatch.IdempotencyKey == idempotencyKey &&
                clientIds.Contains(dispatch.ClientUserId))
            .Select(dispatch => new RecommendationBulkDispatchReadModel(
                dispatch.ClientUserId.Value,
                dispatch.RecommendationId.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<RecommendationBulkDispatch> AddAsync(
        RecommendationBulkDispatch dispatch,
        CancellationToken cancellationToken = default) {
        await context.RecommendationBulkDispatches.AddAsync(dispatch, cancellationToken).ConfigureAwait(false);
        return dispatch;
    }
}
