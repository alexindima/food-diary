using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Usda.Mappings;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;

public class GetDailyMicronutrientsQueryHandler(
    IMealRepository mealRepository,
    IUsdaFoodRepository usdaFoodRepository)
    : IQueryHandler<GetDailyMicronutrientsQuery, Result<DailyMicronutrientSummaryModel>> {
    public async Task<Result<DailyMicronutrientSummaryModel>> Handle(
        GetDailyMicronutrientsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<DailyMicronutrientSummaryModel>(userIdResult.Error);
        }

        IReadOnlyList<Meal> meals = await mealRepository.GetWithItemsAndProductsAsync(
            userIdResult.Value, query.Date, cancellationToken).ConfigureAwait(false);

        var allItems = meals.SelectMany(m => m.Items).ToList();
        var productItems = allItems.Where(i => i.IsProduct && i.Product is not null).ToList();
        var linkedItems = productItems.Where(i => i.Product!.UsdaFdcId.HasValue).ToList();

        int totalProductCount = productItems.Count;
        int linkedProductCount = linkedItems.Count;

        if (linkedItems.Count == 0) {
            return Result.Success(new DailyMicronutrientSummaryModel(
                query.Date, 0, totalProductCount, [], HealthScores: null));
        }

        var fdcIds = linkedItems
            .Select(i => i.Product!.UsdaFdcId!.Value)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>> nutrientsByFdcId = await usdaFoodRepository.GetNutrientsByFdcIdsAsync(fdcIds, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<int, DailyReferenceValue> dailyValues = await usdaFoodRepository.GetDailyReferenceValuesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        Dictionary<int, AggregatedNutrient> aggregated = AggregateNutrients(linkedItems, nutrientsByFdcId);
        List<DailyMicronutrientModel> nutrientModels = BuildNutrientModels(aggregated, dailyValues);
        var nutrientAmounts = aggregated.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Total);
        var dvAmounts = dailyValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
        var healthScores = HealthAreaScores.Calculate(nutrientAmounts, dvAmounts);

        return Result.Success(new DailyMicronutrientSummaryModel(
            query.Date, linkedProductCount, totalProductCount, nutrientModels, healthScores.ToModel()));
    }

    private static Dictionary<int, AggregatedNutrient> AggregateNutrients(
        IReadOnlyList<MealItem> linkedItems,
        IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>> nutrientsByFdcId) {
        var aggregated = new Dictionary<int, AggregatedNutrient>();
        foreach (MealItem item in linkedItems) {
            Product product = item.Product!;
            int fdcId = product.UsdaFdcId!.Value;

            if (!nutrientsByFdcId.TryGetValue(fdcId, out IReadOnlyList<UsdaFoodNutrient>? nutrients)) {
                continue;
            }

            double scale = product.BaseAmount > 0 ? item.Amount / product.BaseAmount : 0;

            foreach (UsdaFoodNutrient nutrient in nutrients) {
                double scaledAmount = nutrient.Amount * scale;
                if (aggregated.TryGetValue(nutrient.NutrientId, out AggregatedNutrient? existing)) {
                    aggregated[nutrient.NutrientId] = existing with { Total = existing.Total + scaledAmount };
                } else {
                    aggregated[nutrient.NutrientId] = new AggregatedNutrient(nutrient.Nutrient.Name, nutrient.Nutrient.UnitName, scaledAmount);
                }
            }
        }

        return aggregated;
    }

    private static List<DailyMicronutrientModel> BuildNutrientModels(
        IReadOnlyDictionary<int, AggregatedNutrient> aggregated,
        IReadOnlyDictionary<int, DailyReferenceValue> dailyValues) {
        return [.. aggregated
            .Select(kvp => {
                dailyValues.TryGetValue(kvp.Key, out DailyReferenceValue? drv);
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
            .OrderBy(n => n.Name, StringComparer.Ordinal)];
    }

    private sealed record AggregatedNutrient(string Name, string Unit, double Total);
}
