using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Export.Models;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Export.Services;

public sealed class ExportDiaryReadService(IMealExportReadService mealExportReadService) : IExportDiaryReadService {
    public async Task<ExportDiaryMealsReadModel> GetMealsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) {
        IReadOnlyList<MealProjectionReadModel> meals = await mealExportReadService.GetByPeriodAsync(
            userId,
            dateFrom,
            dateTo,
            cancellationToken).ConfigureAwait(false);

        return new ExportDiaryMealsReadModel(
            [.. meals.Where(meal => meal.Date >= dateFrom && meal.Date <= dateTo)]);
    }
}
