using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Usda.Common;
using FoodDiary.Application.Usda.Mappings;
using FoodDiary.Application.Usda.Models;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Usda.Queries.GetMicronutrients;

public class GetMicronutrientsQueryHandler(IUsdaFoodRepository repository)
    : IQueryHandler<GetMicronutrientsQuery, Result<UsdaFoodDetailModel>> {
    public async Task<Result<UsdaFoodDetailModel>> Handle(
        GetMicronutrientsQuery query,
        CancellationToken cancellationToken) {
        var food = await repository.GetByFdcIdAsync(query.FdcId, cancellationToken);
        if (food is null) {
            return Result.Failure<UsdaFoodDetailModel>(Errors.Usda.FoodNotFound(query.FdcId));
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
}
