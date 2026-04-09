using FoodDiary.Application.Usda.Models;

namespace FoodDiary.Application.Usda.Common;

public interface IUsdaFoodSearchService {
    Task<IReadOnlyList<UsdaFoodModel>> SearchBrandedAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);
}
