using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteIds;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Meals.Service.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Application.Mappings;

namespace FoodDiary.Modules.Meals.Application.Common;

internal static class MealReadSupport {
    internal static async Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoritesByMealIdAsync(
        ISender sender,
        UserId userId,
        IReadOnlyList<MealProjectionReadModel> meals,
        CancellationToken cancellationToken) {
        MealId[] mealIds = [.. meals
            .Select(static meal => (MealId)meal.Id)
            .Distinct()];

        return await sender.Send(new ReadMealFavoriteIdsQuery(userId, mealIds), cancellationToken).ConfigureAwait(false);
    }
    internal static PagedResponse<MealModel> ToPagedResponse(
        IReadOnlyList<MealProjectionReadModel> meals,
        IReadOnlyDictionary<MealId, FavoriteMealId> favoritesByMealId,
        int page,
        int limit,
        int totalItems) {
        int totalPages = (int)Math.Ceiling(totalItems / (double)limit);
        var items = meals
            .Select(meal => {
                bool isFavorite = favoritesByMealId.TryGetValue((MealId)meal.Id, out FavoriteMealId favoriteMealId);
                return meal.ToModel(
                    isFavorite: isFavorite,
                    favoriteMealId: isFavorite ? favoriteMealId.Value : null);
            })
            .ToList();

        return new PagedResponse<MealModel>(items, page, limit, totalPages, totalItems);
    }
}
