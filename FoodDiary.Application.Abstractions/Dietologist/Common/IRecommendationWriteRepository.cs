using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationWriteRepository : IRecommendationReadRepository {
    Task<Recommendation> AddAsync(Recommendation recommendation, CancellationToken cancellationToken = default);

    Task UpdateAsync(Recommendation recommendation, CancellationToken cancellationToken = default);
}
