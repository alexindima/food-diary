using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Usda.Mappings;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Usda.Services;

public sealed class UsdaDailyMicronutrientReadService(
    IMealProductNutritionReadService mealProductNutritionReadService,
    IUsdaFoodReadModelRepository usdaFoodRepository) : IUsdaDailyMicronutrientReadService {
    public async Task<DailyMicronutrientSummaryModel> GetDailySummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken) {
        IReadOnlyList<MealProductNutritionReadModel> productItems = await mealProductNutritionReadService.GetForDateAsync(
            userId,
            date,
            cancellationToken).ConfigureAwait(false);

        var linkedItems = productItems
            .Where(static item => item.UsdaFdcId.HasValue)
            .ToList();

        int totalProductCount = productItems.Count;
        int linkedProductCount = linkedItems.Count;

        if (linkedItems.Count == 0) {
            return new DailyMicronutrientSummaryModel(date, 0, totalProductCount, [], HealthScores: null);
        }

        var fdcIds = linkedItems
            .Select(static item => item.UsdaFdcId!.Value)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> nutrientsByFdcId = await usdaFoodRepository
            .GetNutrientReadModelsByFdcIdsAsync(fdcIds, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> dailyValues = await usdaFoodRepository
            .GetDailyReferenceValueReadModelsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        Dictionary<int, AggregatedNutrient> aggregated = AggregateNutrients(linkedItems, nutrientsByFdcId);
        List<DailyMicronutrientModel> nutrientModels = BuildNutrientModels(aggregated, dailyValues);
        var nutrientAmounts = aggregated.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value.Total);
        var dvAmounts = dailyValues.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value.Value);
        var healthScores = HealthAreaScores.Calculate(nutrientAmounts, dvAmounts);

        return new DailyMicronutrientSummaryModel(
            date,
            linkedProductCount,
            totalProductCount,
            nutrientModels,
            healthScores.ToModel());
    }

    private static Dictionary<int, AggregatedNutrient> AggregateNutrients(
        IReadOnlyList<MealProductNutritionReadModel> linkedItems,
        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> nutrientsByFdcId) {
        var aggregated = new Dictionary<int, AggregatedNutrient>();
        foreach (MealProductNutritionReadModel item in linkedItems) {
            int fdcId = item.UsdaFdcId!.Value;

            if (!nutrientsByFdcId.TryGetValue(fdcId, out IReadOnlyList<UsdaNutrientReadModel>? nutrients)) {
                continue;
            }

            double scale = item.ProductBaseAmount > 0 ? item.Amount / item.ProductBaseAmount : 0;

            foreach (UsdaNutrientReadModel nutrient in nutrients) {
                double scaledAmount = nutrient.Amount * scale;
                if (aggregated.TryGetValue(nutrient.NutrientId, out AggregatedNutrient? existing)) {
                    aggregated[nutrient.NutrientId] = existing with { Total = existing.Total + scaledAmount };
                } else {
                    aggregated[nutrient.NutrientId] = new AggregatedNutrient(nutrient.Name, nutrient.Unit, scaledAmount);
                }
            }
        }

        return aggregated;
    }

    private static List<DailyMicronutrientModel> BuildNutrientModels(
        IReadOnlyDictionary<int, AggregatedNutrient> aggregated,
        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> dailyValues) {
        return [.. aggregated
            .Select(kvp => {
                dailyValues.TryGetValue(kvp.Key, out UsdaDailyReferenceValueReadModel? drv);
                double? dv = drv?.Value;
                double? percentDv = dv is > 0 ? Math.Round(kvp.Value.Total / dv.Value * 100, 1, MidpointRounding.ToEven) : null;

                return new DailyMicronutrientModel(
                    kvp.Key,
                    kvp.Value.Name,
                    kvp.Value.Unit,
                    Math.Round(kvp.Value.Total, 2, MidpointRounding.ToEven),
                    dv,
                    percentDv);
            })
            .OrderBy(static nutrient => nutrient.Name, StringComparer.Ordinal)];
    }

    private sealed record AggregatedNutrient(string Name, string Unit, double Total);
}
