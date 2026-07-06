using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationWriteRepository {
    Task<Recommendation?> GetByIdAsync(
        RecommendationId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<Recommendation> AddAsync(Recommendation recommendation, CancellationToken cancellationToken = default);

    Task UpdateAsync(Recommendation recommendation, CancellationToken cancellationToken = default);
}
