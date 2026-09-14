using FoodDiary.Application.Gamification.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Gamification.Queries.GetAchievementDefinitionsForAdministration;

public sealed record GetAchievementDefinitionsForAdministrationQuery : IRequest<IReadOnlyList<AchievementDefinitionAdminModel>>;
