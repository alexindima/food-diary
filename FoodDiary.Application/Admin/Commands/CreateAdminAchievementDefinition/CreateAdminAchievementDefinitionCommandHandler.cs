using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.CreateAdminAchievementDefinition;

public sealed class CreateAdminAchievementDefinitionCommandHandler(IAchievementDefinitionAdministrationService service)
    : ICommandHandler<CreateAdminAchievementDefinitionCommand, Result<AchievementDefinitionAdminModel>> {
    public Task<Result<AchievementDefinitionAdminModel>> Handle(
        CreateAdminAchievementDefinitionCommand command,
        CancellationToken cancellationToken) =>
        service.CreateAsync(command.Input, cancellationToken);
}
