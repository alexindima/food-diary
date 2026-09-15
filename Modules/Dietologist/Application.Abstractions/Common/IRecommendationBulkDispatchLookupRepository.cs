using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationBulkDispatchLookupRepository {
    Task<IReadOnlyList<RecommendationBulkDispatchReadModel>> GetExistingAsync(
        UserId dietologistUserId,
        string idempotencyKey,
        IReadOnlyCollection<UserId> clientUserIds,
        CancellationToken cancellationToken = default);
}
