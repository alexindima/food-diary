using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationBulkDispatchWriteRepository {
    Task<RecommendationBulkDispatch> AddAsync(
        RecommendationBulkDispatch dispatch,
        CancellationToken cancellationToken = default);
}
