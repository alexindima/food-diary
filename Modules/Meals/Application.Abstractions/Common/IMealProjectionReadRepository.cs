using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Abstractions.Common;

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
