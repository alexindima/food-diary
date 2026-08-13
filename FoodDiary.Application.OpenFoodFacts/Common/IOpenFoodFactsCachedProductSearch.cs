using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Common;

public interface IOpenFoodFactsCachedProductSearch {
    Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken = default);
}
