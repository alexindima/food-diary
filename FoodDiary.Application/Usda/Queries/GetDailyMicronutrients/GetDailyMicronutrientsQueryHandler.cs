using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Usda.Common;
using FoodDiary.Application.Usda.Models;

namespace FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;

public class GetDailyMicronutrientsQueryHandler(
    IMealRepository mealRepository,
    IUsdaFoodRepository usdaFoodRepository)
    : IQueryHandler<GetDailyMicronutrientsQuery, Result<DailyMicronutrientSummaryModel>> {
    public async Task<Result<DailyMicronutrientSummaryModel>> Handle(
        GetDailyMicronutrientsQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<DailyMicronutrientSummaryModel>(userIdResult.Error);
        }

        var meals = await mealRepository.GetWithItemsAndProductsAsync(
            userIdResult.Value, query.Date, cancellationToken);

        var allItems = meals.SelectMany(m => m.Items).ToList();
        var productItems = allItems.Where(i => i.IsProduct && i.Product is not null).ToList();
        var linkedItems = productItems.Where(i => i.Product!.UsdaFdcId.HasValue).ToList();

        var totalProductCount = productItems.Count;
        var linkedProductCount = linkedItems.Count;

        if (linkedItems.Count == 0) {
            return Result.Success(new DailyMicronutrientSummaryModel(
                query.Date, 0, totalProductCount, []));
        }

        var fdcIds = linkedItems
            .Select(i => i.Product!.UsdaFdcId!.Value)
            .Distinct()
            .ToList();

        var nutrientsByFdcId = await usdaFoodRepository.GetNutrientsByFdcIdsAsync(fdcIds, cancellationToken);
        var dailyValues = await usdaFoodRepository.GetDailyReferenceValuesAsync(cancellationToken: cancellationToken);

        // Aggregate nutrients across all meal items
        // USDA data is per 100g; scale by (mealItem.Amount / product.BaseAmount)
        var aggregated = new Dictionary<int, (string Name, string Unit, double Total)>();

        foreach (var item in linkedItems) {
            var product = item.Product!;
            var fdcId = product.UsdaFdcId!.Value;

            if (!nutrientsByFdcId.TryGetValue(fdcId, out var nutrients)) {
                continue;
            }

            var scale = product.BaseAmount > 0 ? item.Amount / product.BaseAmount : 0;

            foreach (var nutrient in nutrients) {
                var scaledAmount = nutrient.Amount * scale;
                if (aggregated.TryGetValue(nutrient.NutrientId, out var existing)) {
                    aggregated[nutrient.NutrientId] = (existing.Name, existing.Unit, existing.Total + scaledAmount);
                } else {
                    aggregated[nutrient.NutrientId] = (nutrient.Nutrient.Name, nutrient.Nutrient.UnitName, scaledAmount);
                }
            }
        }

        var nutrientModels = aggregated
            .Select(kvp => {
                dailyValues.TryGetValue(kvp.Key, out var drv);
                var dv = drv?.Value;
                var percentDv = dv is > 0 ? Math.Round(kvp.Value.Total / dv.Value * 100, 1) : (double?)null;

                return new DailyMicronutrientModel(
                    kvp.Key,
                    kvp.Value.Name,
                    kvp.Value.Unit,
                    Math.Round(kvp.Value.Total, 2),
                    dv,
                    percentDv);
            })
            .OrderBy(n => n.Name)
            .ToList();

        return Result.Success(new DailyMicronutrientSummaryModel(
            query.Date, linkedProductCount, totalProductCount, nutrientModels));
    }
}
