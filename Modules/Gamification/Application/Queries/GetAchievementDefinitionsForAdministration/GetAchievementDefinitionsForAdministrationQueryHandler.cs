using FoodDiary.Modules.Gamification.Contracts.Queries.GetAchievementDefinitionsForAdministration;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Mediator;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Gamification.Application.Queries.GetAchievementDefinitionsForAdministration;

public sealed class GetAchievementDefinitionsForAdministrationQueryHandler(IAchievementDefinitionReadModelRepository repository) : IRequestHandler<GetAchievementDefinitionsForAdministrationQuery, IReadOnlyList<AchievementDefinitionAdminModel>> {
    public Task<IReadOnlyList<AchievementDefinitionAdminModel>> Handle(GetAchievementDefinitionsForAdministrationQuery request, CancellationToken cancellationToken) =>
        repository.GetForAdministrationAsync(
            PaginationPolicy.NormalizePage(request.Page),
            PaginationPolicy.NormalizePageSize(request.Limit),
            cancellationToken);
}
