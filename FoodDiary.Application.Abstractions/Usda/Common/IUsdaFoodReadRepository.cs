using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.Entities.Usda;

namespace FoodDiary.Application.Abstractions.Usda.Common;

public interface IUsdaFoodReadRepository {
    Task<IReadOnlyList<UsdaFood>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsdaFoodReadModel>> SearchReadModelsAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<UsdaFood?> GetByFdcIdAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<UsdaFoodReadModel?> GetByFdcIdReadModelAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsdaFoodNutrient>> GetNutrientsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsdaNutrientReadModel>> GetNutrientReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsdaFoodPortion>> GetPortionsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsdaFoodPortionModel>> GetPortionReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>> GetNutrientsByFdcIdsAsync(
        IEnumerable<int> fdcIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>>> GetNutrientReadModelsByFdcIdsAsync(
        IEnumerable<int> fdcIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, DailyReferenceValue>> GetDailyReferenceValuesAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel>> GetDailyReferenceValueReadModelsAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default);
}
