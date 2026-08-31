using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Common;

public interface IMealExportReadService {
    Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int limit,
        CancellationToken cancellationToken);
}
