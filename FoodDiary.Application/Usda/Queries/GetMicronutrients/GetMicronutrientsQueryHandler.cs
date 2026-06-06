using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Usda.Mappings;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Usda.Queries.GetMicronutrients;

public class GetMicronutrientsQueryHandler(
    IUsdaFoodRepository repository,
    IUsdaFoodSearchService brandedSearchService)
    : IQueryHandler<GetMicronutrientsQuery, Result<UsdaFoodDetailModel>> {
    public async Task<Result<UsdaFoodDetailModel>> Handle(
        GetMicronutrientsQuery query,
        CancellationToken cancellationToken) {
        UsdaFood? food = await repository.GetByFdcIdAsync(query.FdcId, cancellationToken).ConfigureAwait(false);
        if (food is null) {
            return await BuildBrandedDetailAsync(query.FdcId, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<UsdaFoodNutrient> nutrients = await repository.GetNutrientsAsync(query.FdcId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<UsdaFoodPortion> portions = await repository.GetPortionsAsync(query.FdcId, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<int, DailyReferenceValue> dailyValues = await repository.GetDailyReferenceValuesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success(BuildLocalDetail(food, nutrients, portions, dailyValues));
    }

    private async Task<Result<UsdaFoodDetailModel>> BuildBrandedDetailAsync(int fdcId, CancellationToken cancellationToken) {
        UsdaFoodDetailModel? brandedDetail = await brandedSearchService.GetFoodDetailAsync(fdcId, cancellationToken).ConfigureAwait(false);
        if (brandedDetail is null) {
            return Result.Failure<UsdaFoodDetailModel>(Errors.Usda.FoodNotFound(fdcId));
        }

        IReadOnlyDictionary<int, DailyReferenceValue> dailyValues = await repository.GetDailyReferenceValuesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MicronutrientModel> nutrientModels = ApplyDailyValues(brandedDetail.Nutrients, dailyValues);
        var nutrientAmounts = nutrientModels.ToDictionary(n => n.NutrientId, n => n.AmountPer100g);
        var dvAmounts = dailyValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
        var healthScores = HealthAreaScores.Calculate(nutrientAmounts, dvAmounts);

        return Result.Success(brandedDetail with {
            Nutrients = nutrientModels,
            HealthScores = healthScores.ToModel(),
        });
    }

    private static UsdaFoodDetailModel BuildLocalDetail(
        UsdaFood food,
        IReadOnlyList<UsdaFoodNutrient> nutrients,
        IReadOnlyList<UsdaFoodPortion> portions,
        IReadOnlyDictionary<int, Domain.Entities.Usda.DailyReferenceValue> dailyValues) {
        var nutrientModels = nutrients
            .Select(n => {
                dailyValues.TryGetValue(n.NutrientId, out DailyReferenceValue? drv);
                double? dailyValue = drv?.Value;
                double? percentDv = dailyValue is > 0 ? Math.Round(n.Amount / dailyValue.Value * 100, 1) : (double?)null;

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

        return new UsdaFoodDetailModel(
            food.FdcId,
            food.Description,
            food.FoodCategory,
            nutrientModels,
            portionModels,
            healthScores.ToModel());
    }

    private static IReadOnlyList<MicronutrientModel> ApplyDailyValues(
        IReadOnlyList<MicronutrientModel> nutrients,
        IReadOnlyDictionary<int, Domain.Entities.Usda.DailyReferenceValue> dailyValues) =>
        nutrients
            .Select(n => {
                dailyValues.TryGetValue(n.NutrientId, out DailyReferenceValue? drv);
                double? dailyValue = drv?.Value;
                double? percentDv = dailyValue is > 0 ? Math.Round(n.AmountPer100g / dailyValue.Value * 100, 1) : (double?)null;

                return n with {
                    DailyValue = dailyValue,
                    PercentDailyValue = percentDv,
                };
            })
            .ToList();
}
