using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.UpdateAdminAchievementDefinition;

public sealed class UpdateAdminAchievementDefinitionCommandHandler(IAchievementDefinitionAdministrationService service)
    : ICommandHandler<UpdateAdminAchievementDefinitionCommand, Result<AchievementDefinitionAdminModel>> {
    public Task<Result<AchievementDefinitionAdminModel>> Handle(
        UpdateAdminAchievementDefinitionCommand command,
        CancellationToken cancellationToken) =>
        service.UpdateAsync(command.Id, command.Input, cancellationToken);
}
