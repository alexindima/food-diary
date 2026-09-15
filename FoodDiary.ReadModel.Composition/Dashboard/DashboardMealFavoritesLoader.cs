using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Dashboard.Infrastructure.Persistence;

internal sealed class DashboardMealFavoritesLoader(ICompositionReadContext context) {
    public async Task<IReadOnlyDictionary<MealId, Guid>> LoadAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken) {
        List<DashboardFavoriteMealProjection> favorites = await context.FavoriteMeals
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId && mealIds.Contains(favorite.MealId))
            .Select(favorite => new DashboardFavoriteMealProjection(favorite.MealId, favorite.Id.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return favorites.ToDictionary(favorite => favorite.MealId, favorite => favorite.FavoriteMealId);
    }
}
