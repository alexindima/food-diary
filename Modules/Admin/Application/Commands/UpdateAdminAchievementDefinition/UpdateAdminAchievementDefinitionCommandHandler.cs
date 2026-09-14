using FoodDiary.Mediator;
using FoodDiary.Application.Gamification.Commands.UpdateAchievementDefinition;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.UpdateAdminAchievementDefinition;

public sealed class UpdateAdminAchievementDefinitionCommandHandler(ISender service)
    : ICommandHandler<UpdateAdminAchievementDefinitionCommand, Result<AchievementDefinitionAdminModel>> {
    public Task<Result<AchievementDefinitionAdminModel>> Handle(
        UpdateAdminAchievementDefinitionCommand command,
        CancellationToken cancellationToken) =>
        service.Send(new UpdateAchievementDefinitionCommand(Id: command.Id, Input: command.Input), cancellationToken);
}
