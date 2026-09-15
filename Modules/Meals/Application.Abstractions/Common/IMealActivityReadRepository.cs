using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Abstractions.Common;

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
