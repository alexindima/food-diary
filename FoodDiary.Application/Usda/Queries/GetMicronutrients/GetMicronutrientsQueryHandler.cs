using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Usda.Mappings;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Usda.Queries.GetMicronutrients;

public class GetMicronutrientsQueryHandler(
    IUsdaFoodRepository repository,
    IUsdaFoodSearchService brandedSearchService)
    : IQueryHandler<GetMicronutrientsQuery, Result<UsdaFoodDetailModel>> {
    public async Task<Result<UsdaFoodDetailModel>> Handle(
        GetMicronutrientsQuery query,
        CancellationToken cancellationToken) {
        var food = await repository.GetByFdcIdAsync(query.FdcId, cancellationToken);
        if (food is null) {
            var brandedDetail = await brandedSearchService.GetFoodDetailAsync(query.FdcId, cancellationToken);
            if (brandedDetail is null) {
                return Result.Failure<UsdaFoodDetailModel>(Errors.Usda.FoodNotFound(query.FdcId));
            }

            var brandedDailyValues = await repository.GetDailyReferenceValuesAsync(cancellationToken: cancellationToken);
            var brandedNutrientModels = ApplyDailyValues(brandedDetail.Nutrients, brandedDailyValues);
            var brandedNutrientAmounts = brandedNutrientModels.ToDictionary(n => n.NutrientId, n => n.AmountPer100g);
            var brandedDvAmounts = brandedDailyValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
            var brandedHealthScores = HealthAreaScores.Calculate(brandedNutrientAmounts, brandedDvAmounts);

            return Result.Success(brandedDetail with {
                Nutrients = brandedNutrientModels,
                HealthScores = brandedHealthScores.ToModel(),
            });
        }

        var nutrients = await repository.GetNutrientsAsync(query.FdcId, cancellationToken);
        var portions = await repository.GetPortionsAsync(query.FdcId, cancellationToken);
        var dailyValues = await repository.GetDailyReferenceValuesAsync(cancellationToken: cancellationToken);

        var nutrientModels = nutrients
            .Select(n => {
                dailyValues.TryGetValue(n.NutrientId, out var drv);
                var dailyValue = drv?.Value;
                var percentDv = dailyValue is > 0 ? Math.Round(n.Amount / dailyValue.Value * 100, 1) : (double?)null;

                return new MicronutrientModel(
                    n.NutrientId,
                    n.Nutrient.Name,
                    n.Nutrient.UnitName,
                    n.Amount,
                    dailyValue,
                    percentDv);
            })
            .ToList();

        var portionModels = portions
            .Select(p => new UsdaFoodPortionModel(
                p.Id,
                p.Amount,
                p.MeasureUnitName,
                p.GramWeight,
                p.PortionDescription,
                p.Modifier))
            .ToList();

        var nutrientAmounts = nutrients.ToDictionary(n => n.NutrientId, n => n.Amount);
        var dvAmounts = dailyValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
        var healthScores = HealthAreaScores.Calculate(nutrientAmounts, dvAmounts);

        var model = new UsdaFoodDetailModel(
            food.FdcId,
            food.Description,
            food.FoodCategory,
            nutrientModels,
            portionModels,
            healthScores.ToModel());

        return Result.Success(model);
    }

    private static IReadOnlyList<MicronutrientModel> ApplyDailyValues(
        IReadOnlyList<MicronutrientModel> nutrients,
        IReadOnlyDictionary<int, Domain.Entities.Usda.DailyReferenceValue> dailyValues) =>
        nutrients
            .Select(n => {
                dailyValues.TryGetValue(n.NutrientId, out var drv);
                var dailyValue = drv?.Value;
                var percentDv = dailyValue is > 0 ? Math.Round(n.AmountPer100g / dailyValue.Value * 100, 1) : (double?)null;

                return n with {
                    DailyValue = dailyValue,
                    PercentDailyValue = percentDv,
                };
            })
            .ToList();
}
