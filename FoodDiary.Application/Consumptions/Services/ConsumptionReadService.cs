using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Meals;
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
        (IReadOnlyList<Meal> Items, int TotalItems) = await mealRepository.GetPagedAsync(
            userId,
            page,
            limit,
            filters,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyDictionary<MealId, FavoriteMeal> favoritesByMealId = await GetFavoritesByMealIdAsync(
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
        (IReadOnlyList<Meal> Items, int TotalItems) = await mealRepository.GetPagedAsync(
            userId,
            page,
            limit,
            filters,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<FavoriteMeal> favorites = await favoriteMealRepository.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        var favoriteItems = favorites
            .Take(favoriteLimit)
            .Select(static favorite => favorite.ToModel())
            .ToList();
        IReadOnlyDictionary<MealId, FavoriteMeal> favoritesByMealId = await GetFavoritesByMealIdAsync(
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
        Meal? meal = await mealRepository.GetByIdAsync(
            consumptionId,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return meal?.ToModel();
    }

    private async Task<IReadOnlyDictionary<MealId, FavoriteMeal>> GetFavoritesByMealIdAsync(
        UserId userId,
        IReadOnlyList<Meal> meals,
        CancellationToken cancellationToken) {
        MealId[] mealIds = [.. meals
            .Select(static meal => meal.Id)
            .Distinct()];

        return await favoriteMealRepository.GetByMealIdsAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
    }

    private static PagedResponse<ConsumptionModel> ToPagedResponse(
        IReadOnlyList<Meal> meals,
        IReadOnlyDictionary<MealId, FavoriteMeal> favoritesByMealId,
        int page,
        int limit,
        int totalItems) {
        int totalPages = (int)Math.Ceiling(totalItems / (double)limit);
        var items = meals
            .Select(meal => {
                FavoriteMeal? favorite = favoritesByMealId.GetValueOrDefault(meal.Id);
                return meal.ToModel(
                    isFavorite: favorite is not null,
                    favoriteMealId: favorite?.Id.Value);
            })
            .ToList();

        return new PagedResponse<ConsumptionModel>(items, page, limit, totalPages, totalItems);
    }
}
