using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Export.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Export.Services;

public sealed class ExportDiaryReadService(IMealConsumptionReadRepository mealRepository) : IExportDiaryReadService {
    public async Task<ExportDiaryMealsReadModel> GetMealsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) {
        IReadOnlyList<MealConsumptionReadModel> meals = await mealRepository.GetByPeriodConsumptionReadModelsAsync(
            userId,
            dateFrom,
            dateTo,
            cancellationToken).ConfigureAwait(false);

        return new ExportDiaryMealsReadModel(
            [.. meals.Where(meal => meal.Date >= dateFrom && meal.Date <= dateTo)]);
    }
}
