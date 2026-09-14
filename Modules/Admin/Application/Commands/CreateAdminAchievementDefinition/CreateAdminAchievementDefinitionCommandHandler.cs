using FoodDiary.Mediator;
using FoodDiary.Application.Gamification.Commands.CreateAchievementDefinition;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.CreateAdminAchievementDefinition;

public sealed class CreateAdminAchievementDefinitionCommandHandler(ISender service)
    : ICommandHandler<CreateAdminAchievementDefinitionCommand, Result<AchievementDefinitionAdminModel>> {
    public Task<Result<AchievementDefinitionAdminModel>> Handle(
        CreateAdminAchievementDefinitionCommand command,
        CancellationToken cancellationToken) =>
        service.Send(new CreateAchievementDefinitionCommand(Input: command.Input), cancellationToken);
}
