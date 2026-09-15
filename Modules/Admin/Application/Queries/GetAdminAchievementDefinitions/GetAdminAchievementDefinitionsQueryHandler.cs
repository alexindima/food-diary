using FoodDiary.Mediator;
using FoodDiary.Modules.Gamification.Contracts.Queries.GetAchievementDefinitionsForAdministration;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAchievementDefinitions;

public sealed class GetAdminAchievementDefinitionsQueryHandler(ISender service)
    : IQueryHandler<GetAdminAchievementDefinitionsQuery, Result<IReadOnlyList<AchievementDefinitionAdminModel>>> {
    public async Task<Result<IReadOnlyList<AchievementDefinitionAdminModel>>> Handle(
        GetAdminAchievementDefinitionsQuery query,
        CancellationToken cancellationToken) =>
        Result.Success(await service.Send(new GetAchievementDefinitionsForAdministrationQuery(), cancellationToken).ConfigureAwait(false));
}
