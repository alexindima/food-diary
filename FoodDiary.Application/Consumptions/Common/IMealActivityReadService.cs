using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Common;

public interface IMealActivityReadService {
    Task<int> GetCountAsync(UserId userId, MealQueryFilters filters, CancellationToken cancellationToken);
    Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken);
    Task<int> GetTotalMealCountAsync(UserId userId, CancellationToken cancellationToken);
}
