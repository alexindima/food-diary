using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.ReadModel.Composition.Dashboard;

internal sealed record DashboardFavoriteMealProjection(MealId MealId, Guid FavoriteMealId);
