using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Usda.Common;

namespace FoodDiary.Application.Usda.Services;

public sealed class UsdaProductSuggestionReadService(IUsdaFoodReadModelRepository repository)
    : IUsdaProductSuggestionReadService {
    public Task<IReadOnlyList<UsdaFoodReadModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken) =>
        repository.SearchReadModelsAsync(search, limit, cancellationToken);
}
