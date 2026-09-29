using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Gamification.Contracts.Queries.GetAchievementDefinitionsForAdministration;

public sealed record GetAchievementDefinitionsForAdministrationQuery(int Page = 1, int Limit = 50) : IRequest<IReadOnlyList<AchievementDefinitionAdminModel>>;
