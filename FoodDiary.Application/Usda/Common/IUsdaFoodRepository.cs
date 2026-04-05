using FoodDiary.Domain.Entities.Usda;

namespace FoodDiary.Application.Usda.Common;

public interface IUsdaFoodRepository {
    Task<IReadOnlyList<UsdaFood>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<UsdaFood?> GetByFdcIdAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsdaFoodNutrient>> GetNutrientsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsdaFoodPortion>> GetPortionsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>> GetNutrientsByFdcIdsAsync(
        IEnumerable<int> fdcIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, DailyReferenceValue>> GetDailyReferenceValuesAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default);
}
