using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminAchievementDefinition;

public sealed class UpdateAdminAchievementDefinitionCommandHandler(IAchievementDefinitionAdministrationService service)
    : ICommandHandler<UpdateAdminAchievementDefinitionCommand, Result<AchievementDefinitionAdminModel>> {
    public Task<Result<AchievementDefinitionAdminModel>> Handle(
        UpdateAdminAchievementDefinitionCommand command,
        CancellationToken cancellationToken) =>
        service.UpdateAsync(command.Id, command.Input, cancellationToken);
}
