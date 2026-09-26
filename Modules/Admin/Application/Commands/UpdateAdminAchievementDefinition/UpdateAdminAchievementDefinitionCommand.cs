using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.UpdateAdminAchievementDefinition;

public sealed record UpdateAdminAchievementDefinitionCommand(Guid Id, AchievementDefinitionUpdateInput Input)
    : ICommand<Result<AchievementDefinitionAdminModel>>;
