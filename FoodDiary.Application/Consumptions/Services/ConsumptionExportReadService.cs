using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Services;

public sealed class ConsumptionExportReadService(IMealConsumptionReadRepository repository) : IConsumptionExportReadService {
    public Task<IReadOnlyList<MealConsumptionReadModel>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) =>
        repository.GetByPeriodConsumptionReadModelsAsync(userId, dateFrom, dateTo, cancellationToken);
}
