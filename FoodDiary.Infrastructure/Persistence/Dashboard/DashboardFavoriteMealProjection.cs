using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Dashboard;

internal sealed record DashboardFavoriteMealProjection(MealId MealId, Guid FavoriteMealId);
