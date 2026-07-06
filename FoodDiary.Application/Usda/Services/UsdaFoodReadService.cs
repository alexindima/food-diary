using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Usda.Common;
using FoodDiary.Application.Usda.Mappings;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Usda.Services;

public sealed class UsdaFoodReadService(
    IUsdaFoodReadModelRepository repository,
    IUsdaFoodSearchService brandedSearchService)
    : IUsdaFoodReadService {
    public async Task<Result<IReadOnlyList<UsdaFoodModel>>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken) {
        IReadOnlyList<UsdaFoodReadModel> localFoods = await repository.SearchReadModelsAsync(search, limit, cancellationToken).ConfigureAwait(false);

        var models = localFoods
            .Select(static food => new UsdaFoodModel(food.FdcId, food.Description, food.FoodCategory))
            .ToList();

        if (models.Count >= limit) {
            return Result.Success<IReadOnlyList<UsdaFoodModel>>(models);
        }

        int remaining = limit - models.Count;
        IReadOnlyList<UsdaFoodModel> brandedFoods = await brandedSearchService.SearchBrandedAsync(
            search, remaining, cancellationToken).ConfigureAwait(false);

        var existingIds = models.Select(static model => model.FdcId).ToHashSet();
        IEnumerable<UsdaFoodModel> newBranded = brandedFoods.Where(food => !existingIds.Contains(food.FdcId));
        models.AddRange(newBranded);
        return Result.Success<IReadOnlyList<UsdaFoodModel>>(models);
    }

    public async Task<Result<UsdaFoodDetailModel>> GetDetailAsync(int fdcId, CancellationToken cancellationToken) {
        UsdaFoodReadModel? food = await repository.GetByFdcIdReadModelAsync(fdcId, cancellationToken).ConfigureAwait(false);
        if (food is null) {
            return await BuildBrandedDetailAsync(fdcId, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<UsdaNutrientReadModel> nutrients = await repository.GetNutrientReadModelsAsync(fdcId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<UsdaFoodPortionModel> portions = await repository.GetPortionReadModelsAsync(fdcId, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> dailyValues = await repository.GetDailyReferenceValueReadModelsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success(BuildLocalDetail(food, nutrients, portions, dailyValues));
    }

    private async Task<Result<UsdaFoodDetailModel>> BuildBrandedDetailAsync(int fdcId, CancellationToken cancellationToken) {
        UsdaFoodDetailModel? brandedDetail = await brandedSearchService.GetFoodDetailAsync(fdcId, cancellationToken).ConfigureAwait(false);
        if (brandedDetail is null) {
            return Result.Failure<UsdaFoodDetailModel>(Errors.Usda.FoodNotFound(fdcId));
        }

        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> dailyValues = await repository.GetDailyReferenceValueReadModelsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MicronutrientModel> nutrientModels = ApplyDailyValues(brandedDetail.Nutrients, dailyValues);
        var nutrientAmounts = nutrientModels.ToDictionary(static nutrient => nutrient.NutrientId, static nutrient => nutrient.AmountPer100g);
        var dvAmounts = dailyValues.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value.Value);
        var healthScores = HealthAreaScores.Calculate(nutrientAmounts, dvAmounts);

        return Result.Success(brandedDetail with {
            Nutrients = nutrientModels,
            HealthScores = healthScores.ToModel(),
        });
    }

    private static UsdaFoodDetailModel BuildLocalDetail(
        UsdaFoodReadModel food,
        IReadOnlyList<UsdaNutrientReadModel> nutrients,
        IReadOnlyList<UsdaFoodPortionModel> portions,
        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> dailyValues) {
        var nutrientModels = nutrients
            .Select(nutrient => {
                dailyValues.TryGetValue(nutrient.NutrientId, out UsdaDailyReferenceValueReadModel? drv);
                double? dailyValue = drv?.Value;
                double? percentDv = dailyValue is > 0 ? Math.Round(nutrient.Amount / dailyValue.Value * 100, 1, MidpointRounding.ToEven) : null;

                return new MicronutrientModel(
                    nutrient.NutrientId,
                    nutrient.Name,
                    nutrient.Unit,
                    nutrient.Amount,
                    dailyValue,
                    percentDv);
            })
            .ToList();

        var nutrientAmounts = nutrients.ToDictionary(static nutrient => nutrient.NutrientId, static nutrient => nutrient.Amount);
        var dvAmounts = dailyValues.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value.Value);
        var healthScores = HealthAreaScores.Calculate(nutrientAmounts, dvAmounts);

        return new UsdaFoodDetailModel(
            food.FdcId,
            food.Description,
            food.FoodCategory,
            nutrientModels,
            portions,
            healthScores.ToModel());
    }

    private static IReadOnlyList<MicronutrientModel> ApplyDailyValues(
        IReadOnlyList<MicronutrientModel> nutrients,
        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> dailyValues) =>
        nutrients
            .Select(nutrient => {
                dailyValues.TryGetValue(nutrient.NutrientId, out UsdaDailyReferenceValueReadModel? drv);
                double? dailyValue = drv?.Value;
                double? percentDv = dailyValue is > 0 ? Math.Round(nutrient.AmountPer100g / dailyValue.Value * 100, 1, MidpointRounding.ToEven) : null;

                return nutrient with {
                    DailyValue = dailyValue,
                    PercentDailyValue = percentDv,
                };
            })
            .ToList();
}
