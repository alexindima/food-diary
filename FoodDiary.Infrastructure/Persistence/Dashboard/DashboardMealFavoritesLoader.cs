using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Dashboard;

internal sealed class DashboardMealFavoritesLoader(FoodDiaryDbContext context) {
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
