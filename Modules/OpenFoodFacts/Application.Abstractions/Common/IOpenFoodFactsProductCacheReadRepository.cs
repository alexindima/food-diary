using FoodDiary.Modules.OpenFoodFacts.Contracts.Models;

namespace FoodDiary.Modules.OpenFoodFacts.Application.Abstractions.Common;

public interface IOpenFoodFactsProductCacheReadRepository {
    Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
