using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Services;

public sealed class ConsumptionReadService(
    IMealReadRepository mealRepository,
    IFavoriteMealReadRepository favoriteMealRepository) : IConsumptionReadService {
    public async Task<PagedResponse<ConsumptionModel>> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken) {
        (IReadOnlyList<MealConsumptionReadModel> Items, int TotalItems) = await mealRepository.GetPagedConsumptionReadModelsAsync(
            userId,
            page,
            limit,
            filters,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyDictionary<MealId, FavoriteMealId> favoritesByMealId = await GetFavoritesByMealIdAsync(
            userId,
            Items,
            cancellationToken).ConfigureAwait(false);

        return ToPagedResponse(Items, favoritesByMealId, page, limit, TotalItems);
    }

    public async Task<ConsumptionOverviewModel> GetOverviewAsync(
        UserId userId,
        int page,
        int limit,
        int favoriteLimit,
        MealQueryFilters filters,
        CancellationToken cancellationToken) {
        (IReadOnlyList<MealConsumptionReadModel> Items, int TotalItems) = await mealRepository.GetPagedConsumptionReadModelsAsync(
            userId,
            page,
            limit,
            filters,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<FavoriteMealReadModel> favorites = await favoriteMealRepository.GetAllReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        var favoriteItems = favorites
            .Take(favoriteLimit)
            .Select(static favorite => favorite.ToModel())
            .ToList();
        IReadOnlyDictionary<MealId, FavoriteMealId> favoritesByMealId = await GetFavoritesByMealIdAsync(
            userId,
            Items,
            cancellationToken).ConfigureAwait(false);

        PagedResponse<ConsumptionModel> allConsumptions = ToPagedResponse(Items, favoritesByMealId, page, limit, TotalItems);

        return new ConsumptionOverviewModel(allConsumptions, favoriteItems, favorites.Count);
    }

    public async Task<ConsumptionModel?> GetByIdAsync(
        UserId userId,
        MealId consumptionId,
        CancellationToken cancellationToken) {
        MealConsumptionReadModel? meal = await mealRepository.GetByIdConsumptionReadModelAsync(
            consumptionId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return meal?.ToModel();
    }

    private async Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoritesByMealIdAsync(
        UserId userId,
        IReadOnlyList<MealConsumptionReadModel> meals,
        CancellationToken cancellationToken) {
        MealId[] mealIds = [.. meals
            .Select(static meal => (MealId)meal.Id)
            .Distinct()];

        return await favoriteMealRepository.GetFavoriteIdsByMealIdsAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
    }

    private static PagedResponse<ConsumptionModel> ToPagedResponse(
        IReadOnlyList<MealConsumptionReadModel> meals,
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

        return new PagedResponse<ConsumptionModel>(items, page, limit, totalPages, totalItems);
    }
}
