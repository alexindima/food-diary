using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Export.Models;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Export.Services;

public sealed class ExportDiaryReadService(IConsumptionExportReadService consumptionExportReadService) : IExportDiaryReadService {
    public async Task<ExportDiaryMealsReadModel> GetMealsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) {
        IReadOnlyList<MealConsumptionReadModel> meals = await consumptionExportReadService.GetByPeriodAsync(
            userId,
            dateFrom,
            dateTo,
            cancellationToken).ConfigureAwait(false);

        return new ExportDiaryMealsReadModel(
            [.. meals.Where(meal => meal.Date >= dateFrom && meal.Date <= dateTo)]);
    }
}
