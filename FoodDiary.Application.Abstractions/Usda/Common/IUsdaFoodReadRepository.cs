using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.Entities.Usda;

namespace FoodDiary.Application.Abstractions.Usda.Common;

public interface IUsdaFoodReadRepository {
    Task<IReadOnlyList<UsdaFood>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<UsdaFoodReadModel>> SearchReadModelsAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<UsdaFood> foods = await SearchAsync(query, limit, cancellationToken).ConfigureAwait(false);
        return [.. foods.Select(static food => new UsdaFoodReadModel(food.FdcId, food.Description, food.FoodCategory))];
    }

    Task<UsdaFood?> GetByFdcIdAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    async Task<UsdaFoodReadModel?> GetByFdcIdReadModelAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        UsdaFood? food = await GetByFdcIdAsync(fdcId, cancellationToken).ConfigureAwait(false);
        return food is null ? null : new UsdaFoodReadModel(food.FdcId, food.Description, food.FoodCategory);
    }

    Task<IReadOnlyList<UsdaFoodNutrient>> GetNutrientsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<UsdaNutrientReadModel>> GetNutrientReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<UsdaFoodNutrient> nutrients = await GetNutrientsAsync(fdcId, cancellationToken).ConfigureAwait(false);
        return [.. nutrients.Select(static nutrient => new UsdaNutrientReadModel(
            nutrient.NutrientId,
            nutrient.Nutrient.Name,
            nutrient.Nutrient.UnitName,
            nutrient.Amount))];
    }

    Task<IReadOnlyList<UsdaFoodPortion>> GetPortionsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<UsdaFoodPortionModel>> GetPortionReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<UsdaFoodPortion> portions = await GetPortionsAsync(fdcId, cancellationToken).ConfigureAwait(false);
        return [.. portions.Select(static portion => new UsdaFoodPortionModel(
            portion.Id,
            portion.Amount,
            portion.MeasureUnitName,
            portion.GramWeight,
            portion.PortionDescription,
            portion.Modifier))];
    }

    Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>> GetNutrientsByFdcIdsAsync(
        IEnumerable<int> fdcIds,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>>> GetNutrientReadModelsByFdcIdsAsync(
        IEnumerable<int> fdcIds,
        CancellationToken cancellationToken = default) {
        IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>> nutrientsByFdcId = await GetNutrientsByFdcIdsAsync(
            fdcIds,
            cancellationToken).ConfigureAwait(false);

        return nutrientsByFdcId.ToDictionary(
            static item => item.Key,
            static item => (IReadOnlyList<UsdaNutrientReadModel>)[
                .. item.Value.Select(static nutrient => new UsdaNutrientReadModel(
                    nutrient.NutrientId,
                    nutrient.Nutrient.Name,
                    nutrient.Nutrient.UnitName,
                    nutrient.Amount)),
            ]);
    }

    Task<IReadOnlyDictionary<int, DailyReferenceValue>> GetDailyReferenceValuesAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel>> GetDailyReferenceValueReadModelsAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default) {
        IReadOnlyDictionary<int, DailyReferenceValue> values = await GetDailyReferenceValuesAsync(
            ageGroup,
            gender,
            cancellationToken).ConfigureAwait(false);

        return values.ToDictionary(
            static item => item.Key,
            static item => new UsdaDailyReferenceValueReadModel(
                item.Value.NutrientId,
                item.Value.Value,
                item.Value.Unit));
    }
}
