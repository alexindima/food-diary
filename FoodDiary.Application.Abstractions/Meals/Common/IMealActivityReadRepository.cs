using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealActivityReadRepository {
    Task<int> GetCountAsync(
        UserId userId,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalMealCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
