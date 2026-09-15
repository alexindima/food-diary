using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Application.Abstractions.Common;

public interface IUsdaFoodReadModelRepository {
    Task<IReadOnlyList<UsdaFoodReadModel>> SearchReadModelsAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<UsdaFoodReadModel?> GetByFdcIdReadModelAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsdaNutrientReadModel>> GetNutrientReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsdaFoodPortionModel>> GetPortionReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>>> GetNutrientReadModelsByFdcIdsAsync(
        IEnumerable<int> fdcIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel>> GetDailyReferenceValueReadModelsAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default);
}
