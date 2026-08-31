using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Meals.Mappings;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Services;

public sealed class MealReadService(
    IMealProjectionReadRepository mealRepository,
    IMealFavoriteReadService favoriteReadService) : IMealReadService, IFavoriteMealSourceReadService {
    public async Task<PagedResponse<MealModel>> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken) {
        (IReadOnlyList<MealProjectionReadModel> items, int totalItems) = await mealRepository.GetPagedMealProjectionsAsync(
            userId,
            page,
            limit,
            filters,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyDictionary<MealId, FavoriteMealId> favoritesByMealId = await GetFavoritesByMealIdAsync(
            userId,
            items,
            cancellationToken).ConfigureAwait(false);

        return ToPagedResponse(items, favoritesByMealId, page, limit, totalItems);
    }

    public async Task<MealOverviewModel> GetOverviewAsync(
        UserId userId,
        int page,
        int limit,
        int favoriteLimit,
        MealQueryFilters filters,
        CancellationToken cancellationToken) {
        (IReadOnlyList<MealProjectionReadModel> items, int totalItems) = await mealRepository.GetPagedMealProjectionsAsync(
            userId,
            page,
            limit,
            filters,
            cancellationToken).ConfigureAwait(false);

        (IReadOnlyList<MealFavoriteMealModel> favoriteItems, int favoriteCount) =
            await favoriteReadService.GetOverviewAsync(userId, favoriteLimit, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<MealId, FavoriteMealId> favoritesByMealId = await GetFavoritesByMealIdAsync(
            userId,
            items,
            cancellationToken).ConfigureAwait(false);

        PagedResponse<MealModel> allMeals = ToPagedResponse(items, favoritesByMealId, page, limit, totalItems);

        return new MealOverviewModel(allMeals, favoriteItems, favoriteCount);
    }

    public async Task<MealModel?> GetByIdAsync(
        UserId userId,
        MealId mealId,
        CancellationToken cancellationToken) {
        MealProjectionReadModel? meal = await mealRepository.GetByIdMealProjectionAsync(
            mealId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return meal?.ToModel();
    }

    public async Task<FavoriteMealSourceModel?> GetAsync(
        UserId userId,
        MealId mealId,
        CancellationToken cancellationToken) {
        MealProjectionReadModel? meal = await mealRepository.GetByIdMealProjectionAsync(
            mealId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return meal is null
            ? null
            : new FavoriteMealSourceModel(
                meal.Date,
                meal.MealType?.ToString(),
                meal.TotalCalories,
                meal.TotalProteins,
                meal.TotalFats,
                meal.TotalCarbs,
                meal.Items.Count);
    }

    private async Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoritesByMealIdAsync(
        UserId userId,
        IReadOnlyList<MealProjectionReadModel> meals,
        CancellationToken cancellationToken) {
        MealId[] mealIds = [.. meals
            .Select(static meal => (MealId)meal.Id)
            .Distinct()];

        return await favoriteReadService.GetFavoriteIdsByMealIdsAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
    }

    private static PagedResponse<MealModel> ToPagedResponse(
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
