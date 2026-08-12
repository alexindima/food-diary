using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminAchievementDefinition;

public sealed record UpdateAdminAchievementDefinitionCommand(Guid Id, AchievementDefinitionUpdateInput Input)
    : ICommand<Result<AchievementDefinitionAdminModel>>;
