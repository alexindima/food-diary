using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationReadRepository {
    Task<IReadOnlyList<Recommendation>> GetByClientAsync(
        UserId clientUserId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recommendation>> GetByDietologistAndClientAsync(
        UserId dietologistUserId,
        UserId clientUserId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<Recommendation?> GetByIdAsync(
        RecommendationId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(UserId clientUserId, CancellationToken cancellationToken = default);
}
