using FoodDiary.Modules.OpenFoodFacts.Contracts.Models;

namespace FoodDiary.Modules.OpenFoodFacts.Application.Abstractions.Common;

public interface IOpenFoodFactsProductCacheWriteRepository {
    Task UpsertAsync(
        IReadOnlyCollection<OpenFoodFactsProductModel> products,
        CancellationToken cancellationToken = default);
}
