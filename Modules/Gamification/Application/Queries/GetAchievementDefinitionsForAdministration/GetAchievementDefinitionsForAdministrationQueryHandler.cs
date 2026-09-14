using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Gamification.Queries.GetAchievementDefinitionsForAdministration;

public sealed class GetAchievementDefinitionsForAdministrationQueryHandler(IAchievementDefinitionStore store) : IRequestHandler<GetAchievementDefinitionsForAdministrationQuery, IReadOnlyList<AchievementDefinitionAdminModel>> {
    public async Task<IReadOnlyList<AchievementDefinitionAdminModel>> Handle(GetAchievementDefinitionsForAdministrationQuery request, CancellationToken cancellationToken) {
        IReadOnlyDictionary<string, int> counts = await store.GetAwardCountsAsync(cancellationToken).ConfigureAwait(false);
        return (await store.GetAllAsync(cancellationToken).ConfigureAwait(false))
            .Select(item => ToModel(item) with { AwardedUsers = counts.GetValueOrDefault(item.Key) }).ToList();

    }

    private static AchievementDefinitionAdminModel ToModel(AchievementDefinition definition) => new(
        definition.Id.Value, definition.Key, definition.Category, definition.Metric.ToString(), definition.Threshold,
        definition.TitleRu, definition.TitleEn, definition.DescriptionRu, definition.DescriptionEn, definition.Icon,
        definition.SortOrder, definition.IsActive, definition.Version);

}
