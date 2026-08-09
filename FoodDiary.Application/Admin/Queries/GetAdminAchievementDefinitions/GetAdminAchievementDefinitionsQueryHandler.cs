using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminAchievementDefinitions;

public sealed class GetAdminAchievementDefinitionsQueryHandler(IAchievementDefinitionAdministrationService service)
    : IQueryHandler<GetAdminAchievementDefinitionsQuery, Result<IReadOnlyList<AchievementDefinitionAdminModel>>> {
    public async Task<Result<IReadOnlyList<AchievementDefinitionAdminModel>>> Handle(
        GetAdminAchievementDefinitionsQuery query,
        CancellationToken cancellationToken) =>
        Result.Success(await service.GetAllAsync(cancellationToken).ConfigureAwait(false));
}
