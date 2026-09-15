using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

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
