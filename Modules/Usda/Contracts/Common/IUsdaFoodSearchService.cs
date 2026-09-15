using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Contracts.Common;

public interface IUsdaFoodSearchService {
    Task<IReadOnlyList<UsdaFoodModel>> SearchBrandedAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<UsdaFoodDetailModel?> GetFoodDetailAsync(
        int fdcId,
        CancellationToken cancellationToken = default);
}
