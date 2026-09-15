using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Infrastructure.Persistence;

internal sealed record DashboardFavoriteMealProjection(MealId MealId, Guid FavoriteMealId);
