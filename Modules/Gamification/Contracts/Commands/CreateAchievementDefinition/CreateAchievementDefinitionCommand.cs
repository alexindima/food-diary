using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Gamification.Commands.CreateAchievementDefinition;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record CreateAchievementDefinitionCommand(
    AchievementDefinitionCreateInput Input) : IRequest<Result<AchievementDefinitionAdminModel>>;
