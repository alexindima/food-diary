using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

namespace FoodDiary.Application.Abstractions.OpenFoodFacts.Common;

public interface IOpenFoodFactsProductCacheWriteRepository {
    Task UpsertAsync(
        IReadOnlyCollection<OpenFoodFactsProductModel> products,
        CancellationToken cancellationToken = default);
}
