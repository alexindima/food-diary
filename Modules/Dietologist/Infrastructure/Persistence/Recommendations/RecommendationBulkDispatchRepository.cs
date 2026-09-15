using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Dietologist.Infrastructure.Persistence.Recommendations;

internal sealed class RecommendationBulkDispatchRepository(DbSet<RecommendationBulkDispatch> records)
    : IRecommendationBulkDispatchRepository {
    public async Task<IReadOnlyList<RecommendationBulkDispatchReadModel>> GetExistingAsync(
        UserId dietologistUserId,
        string idempotencyKey,
        IReadOnlyCollection<UserId> clientUserIds,
        CancellationToken cancellationToken = default) {
        UserId[] clientIds = [.. clientUserIds];
        return await records
            .AsNoTracking()
            .Where(dispatch =>
                dispatch.DietologistUserId == dietologistUserId &&
                dispatch.IdempotencyKey == idempotencyKey &&
                Enumerable.Contains(clientIds, dispatch.ClientUserId))
            .Select(dispatch => new RecommendationBulkDispatchReadModel(
                dispatch.ClientUserId.Value,
                dispatch.RecommendationId.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<RecommendationBulkDispatch> AddAsync(
        RecommendationBulkDispatch dispatch,
        CancellationToken cancellationToken = default) {
        await records.AddAsync(dispatch, cancellationToken).ConfigureAwait(false);
        return dispatch;
    }
}
