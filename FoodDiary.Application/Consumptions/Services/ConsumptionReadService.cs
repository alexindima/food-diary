using FoodDiary.Application.Abstractions.Consumptions.Models;
using FoodDiary.Application.Abstractions.Consumptions.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Services;

public sealed class ConsumptionReadService(
    IMealConsumptionReadRepository mealRepository,
    IConsumptionFavoriteReadService favoriteReadService) : IConsumptionReadService, IFavoriteMealSourceReadService {
    public async Task<PagedResponse<ConsumptionModel>> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken) {
        (IReadOnlyList<MealConsumptionReadModel> items, int totalItems) = await mealRepository.GetPagedConsumptionReadModelsAsync(
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

    public async Task<ConsumptionOverviewModel> GetOverviewAsync(
        UserId userId,
        int page,
        int limit,
        int favoriteLimit,
        MealQueryFilters filters,
        CancellationToken cancellationToken) {
        (IReadOnlyList<MealConsumptionReadModel> items, int totalItems) = await mealRepository.GetPagedConsumptionReadModelsAsync(
            userId,
            page,
            limit,
            filters,
            cancellationToken).ConfigureAwait(false);

        (IReadOnlyList<ConsumptionFavoriteMealModel> favoriteItems, int favoriteCount) =
            await favoriteReadService.GetOverviewAsync(userId, favoriteLimit, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<MealId, FavoriteMealId> favoritesByMealId = await GetFavoritesByMealIdAsync(
            userId,
            items,
            cancellationToken).ConfigureAwait(false);

        PagedResponse<ConsumptionModel> allConsumptions = ToPagedResponse(items, favoritesByMealId, page, limit, totalItems);

        return new ConsumptionOverviewModel(allConsumptions, favoriteItems, favoriteCount);
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

    public async Task<FavoriteMealSourceModel?> GetAsync(
        UserId userId,
        MealId mealId,
        CancellationToken cancellationToken) {
        MealConsumptionReadModel? meal = await mealRepository.GetByIdConsumptionReadModelAsync(
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
        IReadOnlyList<MealConsumptionReadModel> meals,
        CancellationToken cancellationToken) {
        MealId[] mealIds = [.. meals
            .Select(static meal => (MealId)meal.Id)
            .Distinct()];

        return await favoriteReadService.GetFavoriteIdsByMealIdsAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
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
