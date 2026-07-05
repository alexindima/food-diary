using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Common;

public interface IConsumptionReadService {
    Task<PagedResponse<ConsumptionModel>> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken);

    Task<ConsumptionOverviewModel> GetOverviewAsync(
        UserId userId,
        int page,
        int limit,
        int favoriteLimit,
        MealQueryFilters filters,
        CancellationToken cancellationToken);

    Task<ConsumptionModel?> GetByIdAsync(
        UserId userId,
        MealId consumptionId,
        CancellationToken cancellationToken);
}
