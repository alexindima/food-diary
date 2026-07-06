using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealConsumptionReadRepository {
    Task<(IReadOnlyList<MealConsumptionReadModel> Items, int TotalItems)> GetPagedConsumptionReadModelsAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealConsumptionReadModel>> GetByPeriodConsumptionReadModelsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<MealConsumptionReadModel?> GetByIdConsumptionReadModelAsync(
        MealId id,
        UserId userId,
        CancellationToken cancellationToken = default);
}
