using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public interface IRecommendationTemplateRepository {
    Task<RecommendationTemplate> AddAsync(
        RecommendationTemplate template,
        CancellationToken cancellationToken = default);

    Task<RecommendationTemplate?> GetByIdAsync(
        RecommendationTemplateId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendationTemplateReadModel>> SearchAsync(
        UserId dietologistUserId,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default);
}
