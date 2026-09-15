using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Gamification.Contracts.Queries.GetAchievementDefinitionsForAdministration;

public sealed record GetAchievementDefinitionsForAdministrationQuery : IRequest<IReadOnlyList<AchievementDefinitionAdminModel>>;
