using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationTemplateWriteRepository {
    Task<RecommendationTemplate> AddAsync(
        RecommendationTemplate template,
        CancellationToken cancellationToken = default);

    Task<RecommendationTemplate?> GetByIdAsync(
        RecommendationTemplateId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);
}
