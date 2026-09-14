using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Gamification.Commands.UpdateAchievementDefinition;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record UpdateAchievementDefinitionCommand(
    Guid Id,
    AchievementDefinitionUpdateInput Input) : IRequest<Result<AchievementDefinitionAdminModel>>;
