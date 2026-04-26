using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Application.Abstractions.Usda.Common;

public interface IUsdaFoodSearchService {
    Task<IReadOnlyList<UsdaFoodModel>> SearchBrandedAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);
}
