using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Services;

public sealed class MealExportReadService(IMealProjectionReadRepository repository) : IMealExportReadService {
    public Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int limit,
        CancellationToken cancellationToken) =>
        repository.GetByPeriodMealProjectionsAsync(userId, dateFrom, dateTo, limit, cancellationToken);
}
