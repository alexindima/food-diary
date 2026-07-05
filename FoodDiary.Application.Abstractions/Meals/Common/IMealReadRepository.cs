using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealReadRepository {
    Task<Meal?> GetByIdAsync(
        MealId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default);

    async Task<int> GetCountAsync(
        UserId userId,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default) {
        (_, int totalItems) = await GetPagedAsync(userId, page: 1, limit: 1, filters, cancellationToken).ConfigureAwait(false);
        return totalItems;
    }

    Task<IReadOnlyList<Meal>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalMealCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<MealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<Meal> meals = await GetWithItemsAndProductsAsync(userId, date, cancellationToken).ConfigureAwait(false);

        return [
            .. meals
                .SelectMany(static meal => meal.Items)
                .Where(static item => item.IsProduct && item.Product is not null)
                .Select(static item => new MealProductNutritionReadModel(
                    item.Amount,
                    item.Product!.BaseAmount,
                    item.Product.UsdaFdcId)),
        ];
    }
}
