using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealProjectionReadRepository {
    Task<(IReadOnlyList<MealProjectionReadModel> Items, int TotalItems)> GetPagedMealProjectionsAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int limit,
        CancellationToken cancellationToken = default);

    Task<MealProjectionReadModel?> GetByIdMealProjectionAsync(
        MealId id,
        UserId userId,
        CancellationToken cancellationToken = default);
}
