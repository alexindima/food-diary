using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteMeals.Services;

public sealed class FavoriteMealReadService(IFavoriteMealReadModelRepository favoriteMealReadModelRepository)
    : IFavoriteMealReadService, IConsumptionFavoriteReadService {
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

    async Task<(IReadOnlyList<ConsumptionFavoriteMealModel> Items, int TotalItems)>
        IConsumptionFavoriteReadService.GetOverviewAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken) {
        IReadOnlyList<FavoriteMealReadModel> favorites = await favoriteMealReadModelRepository
            .GetAllReadModelsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return ([.. favorites.Take(limit).Select(ToConsumptionModel)], favorites.Count);
    }

    private static ConsumptionFavoriteMealModel ToConsumptionModel(FavoriteMealReadModel favorite) =>
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
