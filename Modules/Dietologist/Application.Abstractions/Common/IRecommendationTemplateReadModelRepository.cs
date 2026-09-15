using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationTemplateReadModelRepository {
    Task<IReadOnlyList<RecommendationTemplateReadModel>> SearchAsync(
        UserId dietologistUserId,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default);
}
