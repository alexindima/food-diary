using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationWriteRepository {
    Task<Recommendation?> GetByIdAsync(
        RecommendationId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<Recommendation> AddAsync(Recommendation recommendation, CancellationToken cancellationToken = default);

    Task UpdateAsync(Recommendation recommendation, CancellationToken cancellationToken = default);
}
