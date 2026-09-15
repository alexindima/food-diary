using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoritesOverview;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.ReadMealFavoritesOverview;

public sealed class ReadMealFavoritesOverviewQueryHandler(IFavoriteMealReadModelRepository favoriteMealReadModelRepository) : IQueryHandler<ReadMealFavoritesOverviewQuery, (IReadOnlyList<MealFavoriteMealModel> Items, int TotalItems)> {
    public async Task<(IReadOnlyList<MealFavoriteMealModel> Items, int TotalItems)> Handle(ReadMealFavoritesOverviewQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        int limit = request.Limit;
        (IReadOnlyList<FavoriteMealReadModel> favorites, int totalItems) = await favoriteMealReadModelRepository
            .GetOverviewReadModelsAsync(userId, limit, cancellationToken)
            .ConfigureAwait(false);

        return ([.. favorites.Select(ToMealModel)], totalItems);
    }

    private static MealFavoriteMealModel ToMealModel(FavoriteMealReadModel favorite) =>
        new(
            favorite.Id,
            favorite.MealId,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.MealDate,
            favorite.MealType,
            favorite.TotalCalories,
            favorite.TotalProteins,
            favorite.TotalFats,
            favorite.TotalCarbs,
            favorite.ItemCount);
}
