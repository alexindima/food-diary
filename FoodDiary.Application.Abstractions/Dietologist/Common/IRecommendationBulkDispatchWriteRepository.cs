using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationBulkDispatchWriteRepository {
    Task<RecommendationBulkDispatch> AddAsync(
        RecommendationBulkDispatch dispatch,
        CancellationToken cancellationToken = default);
}
