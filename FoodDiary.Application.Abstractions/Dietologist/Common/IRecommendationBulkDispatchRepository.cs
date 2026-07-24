using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationBulkDispatchRepository {
    Task<IReadOnlyList<RecommendationBulkDispatchReadModel>> GetExistingAsync(
        UserId dietologistUserId,
        string idempotencyKey,
        IReadOnlyCollection<UserId> clientUserIds,
        CancellationToken cancellationToken = default);

    Task<RecommendationBulkDispatch> AddAsync(
        RecommendationBulkDispatch dispatch,
        CancellationToken cancellationToken = default);
}
