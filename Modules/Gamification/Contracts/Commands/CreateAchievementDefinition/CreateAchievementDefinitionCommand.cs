using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Gamification.Contracts.Commands.CreateAchievementDefinition;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record CreateAchievementDefinitionCommand(
    AchievementDefinitionCreateInput Input) : IRequest<Result<AchievementDefinitionAdminModel>>;
