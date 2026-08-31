using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Infrastructure.Persistence.Dashboard;

internal sealed record DashboardFavoriteMealProjection(MealId MealId, Guid FavoriteMealId);
