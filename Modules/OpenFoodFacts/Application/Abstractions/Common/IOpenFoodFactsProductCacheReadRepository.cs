using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

namespace FoodDiary.Application.Abstractions.OpenFoodFacts.Common;

public interface IOpenFoodFactsProductCacheReadRepository {
    Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
