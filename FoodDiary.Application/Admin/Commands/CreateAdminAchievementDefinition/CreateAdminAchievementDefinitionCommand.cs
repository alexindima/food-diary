using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.CreateAdminAchievementDefinition;

public sealed record CreateAdminAchievementDefinitionCommand(AchievementDefinitionCreateInput Input)
    : ICommand<Result<AchievementDefinitionAdminModel>>;
