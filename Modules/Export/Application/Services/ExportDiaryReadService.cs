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
        int limit,
        CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        IReadOnlyList<MealProjectionReadModel> meals = await mealExportReadService.GetByPeriodAsync(
            userId,
            dateFrom,
            dateTo,
            checked(limit + 1),
            cancellationToken).ConfigureAwait(false);

        List<MealProjectionReadModel> matchingMeals = [.. meals.Where(meal => meal.Date >= dateFrom && meal.Date <= dateTo)];

        return new ExportDiaryMealsReadModel(
            [.. matchingMeals.Take(limit)],
            matchingMeals.Count > limit);
    }
}
