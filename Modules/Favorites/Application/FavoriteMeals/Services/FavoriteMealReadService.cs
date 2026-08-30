using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.Favorites.FavoriteMeals.Mappings;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Favorites.FavoriteMeals.Services;

public sealed class FavoriteMealReadService(IFavoriteMealReadModelRepository favoriteMealReadModelRepository)
    : IFavoriteMealReadService, IMealFavoriteReadService {
    public async Task<IReadOnlyList<FavoriteMealModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteMealReadModel> favorites = await favoriteMealReadModelRepository.GetAllReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(favorite => favorite.ToModel())];
    }

    public Task<bool> ExistsByMealIdAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        favoriteMealReadModelRepository.ExistsByMealIdAsync(mealId, userId, cancellationToken);

    public Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoriteIdsByMealIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken = default) =>
        favoriteMealReadModelRepository.GetFavoriteIdsByMealIdsAsync(userId, mealIds, cancellationToken);

    public async Task<(IReadOnlyList<FavoriteMealModel> Items, int TotalItems)> GetOverviewAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteMealReadModel> favorites = await favoriteMealReadModelRepository
            .GetAllReadModelsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return ([.. favorites.Take(limit).Select(favorite => favorite.ToModel())], favorites.Count);
    }

    async Task<(IReadOnlyList<MealFavoriteMealModel> Items, int TotalItems)>
        IMealFavoriteReadService.GetOverviewAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken) {
        IReadOnlyList<FavoriteMealReadModel> favorites = await favoriteMealReadModelRepository
            .GetAllReadModelsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return ([.. favorites.Take(limit).Select(ToMealModel)], favorites.Count);
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
