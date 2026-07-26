using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationBulkDispatchLookupRepository {
    Task<IReadOnlyList<RecommendationBulkDispatchReadModel>> GetExistingAsync(
        UserId dietologistUserId,
        string idempotencyKey,
        IReadOnlyCollection<UserId> clientUserIds,
        CancellationToken cancellationToken = default);
}
