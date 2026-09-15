using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Gamification.Queries.GetAchievementDefinitionsForAdministration;

public sealed class GetAchievementDefinitionsForAdministrationQueryHandler(IAchievementDefinitionReadModelRepository repository) : IRequestHandler<GetAchievementDefinitionsForAdministrationQuery, IReadOnlyList<AchievementDefinitionAdminModel>> {
    public Task<IReadOnlyList<AchievementDefinitionAdminModel>> Handle(GetAchievementDefinitionsForAdministrationQuery request, CancellationToken cancellationToken) =>
        repository.GetForAdministrationAsync(cancellationToken);
}
