using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationTemplateWriteRepository {
    Task<RecommendationTemplate> AddAsync(
        RecommendationTemplate template,
        CancellationToken cancellationToken = default);

    Task<RecommendationTemplate?> GetByIdAsync(
        RecommendationTemplateId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);
}
