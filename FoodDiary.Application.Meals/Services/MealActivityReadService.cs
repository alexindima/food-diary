using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Services;

public sealed class MealActivityReadService(IMealActivityReadRepository repository) : IMealActivityReadService {
    public Task<int> GetCountAsync(UserId userId, MealQueryFilters filters, CancellationToken cancellationToken) =>
        repository.GetCountAsync(userId, filters, cancellationToken);

    public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) =>
        repository.GetDistinctMealDatesAsync(userId, dateFrom, dateTo, cancellationToken);

    public Task<int> GetTotalMealCountAsync(UserId userId, CancellationToken cancellationToken) =>
        repository.GetTotalMealCountAsync(userId, cancellationToken);
}
