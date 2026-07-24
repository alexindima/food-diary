using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class RecommendationTemplateReadService(IRecommendationTemplateRepository repository)
    : IRecommendationTemplateReadService {
    public async Task<IReadOnlyList<RecommendationTemplateModel>> SearchAsync(
        UserId dietologistUserId,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken) {
        IReadOnlyList<RecommendationTemplateReadModel> templates = await repository.SearchAsync(
            dietologistUserId,
            search,
            includeArchived,
            cancellationToken).ConfigureAwait(false);
        return templates.Select(template => template.ToModel()).ToList();
    }
}
