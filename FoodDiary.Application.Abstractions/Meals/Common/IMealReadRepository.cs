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


    Task<IReadOnlyList<Meal>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
